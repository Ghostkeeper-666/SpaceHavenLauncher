using SH.Content;
using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Modding.Build;

public sealed class ModBuilder : IAsyncDisposable
{
    private readonly BuildSettings Settings;
    private BuildPathData Paths => Settings.Paths;

    private readonly LoggerCollection Log;
    private FileLogger FileLogger;

    private BuildData Build;
    private ParallelOptions ParallelOptions => Settings.ParallelOptions;
    private CancellationToken CT => Settings.CT;

    private IProgressInfo Initialization => Settings.InitializationProgress;

    private IProgressInfo XmlBuild => Settings.XmlBuildProgress;
    private IProgressInfo ResetXmlBuild;
    private IProgressInfo CopyTemplateFiles;
    private IProgressInfo LoadXml;
    private IProgressInfo FixTexts;
    private IProgressInfo MergeAudio;
    private IProgressInfo CollectSpriteRefs;
    private IProgressInfo LoadSprites;
    private IProgressInfo PackSprites;
    private IProgressInfo WriteSpriteSheets;
    private IProgressInfo WriteTextureXmlFiles;
    private IProgressInfo MergeXmlFiles;
    private IProgressInfo PatchXmlFiles;
    private IProgressInfo BuildJarFile;

    private IProgressInfo JavaBuild => Settings.JavaBuildProgress;
    private IProgressInfo ResetJavaBuild;
    private IProgressInfo PrepareJavaFiles;

    public bool IsNewJar { get; private set; }
    public bool NeedsXmlBuild { get; private set; }
    public bool NeedsJavaBuild { get; private set; }

    private readonly EXmlFileType[] SupportedXmlMergeFileTypes =
    [
        EXmlFileType.SpaceHavenSettings,
        EXmlFileType.Audio,
        EXmlFileType.Textures,
        EXmlFileType.Animations,
        EXmlFileType.Texts,
        EXmlFileType.Haven,
    ];

    private readonly EXmlFileType[] SupportedXmlPatchFileTypes =
    [
        EXmlFileType.SpaceHavenSettings,
        EXmlFileType.Audio,
        EXmlFileType.Textures,
        EXmlFileType.Animations,
        EXmlFileType.Texts,
        EXmlFileType.Haven,
    ];

    private readonly Stopwatch Clock = new();




    public ModBuilder(BuildSettings settings, ILogger logger)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Settings.Paths = new(
            settings.AppDir,
            settings.WorkDir,
            settings.SpaceHavenDir,
            settings.SpaceHavenJarDir
        );
        Log = new LoggerCollection(logger);
    }

    private void Fail() => Settings.Fail();
















    public async Task<bool> TryBuildAsync()
    {
        try
        {
            Log.Info($"Starting Build...", Paths.BuildDirectory);

            Build = new BuildData(Settings, Log);

            // No mods?
            if ((Settings?.Mods?.Count ?? 0) <= 0)
            {
                Log.Error("There are no mods enabled");
                return false;
            }

            // Progress setup:
            if (!await TrySetupProgress())
                return false;

            // Start build performance measurement:
            Clock.Restart();

            // Build Initialization:
            if (!await TryInitialize())
                return false;

            // JAVA build:
            if (NeedsJavaBuild)
            {
                // Copy JAVA hash file to cache directory:
                if (!await IOUtils.TryCopyFileAsync(Paths.BuildJavaHashPath, Paths.CacheJavaHashPath, true, Log, CT))
                    return false;
            }
            else Log.Success("JAVA build skipped");
            JavaBuild.Complete();

            // XML Build:
            if (NeedsXmlBuild)
            {
                // Clear build directories:
                if (!await TryResetXmlBuild())
                    return false;

                // Copy template stage:
                Clock.Restart();
                if (!await IOUtils.TryCopyDirectoryAsync(Paths.TemplateStageDirectory, Paths.BuildStageDirectory, true, Log, ParallelOptions))
                    return false;
                CopyTemplateFiles.Complete();
                Log.Debug($"{CopyTemplateFiles} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildDirectory);

                // Load XML files:
                Clock.Restart();
                // Read base XML files:
                if (!await Build.TryLoadXmlFiles(CT))
                    return false;
                // Read mod XML files, evaluating with previously loaded variable values:
                await Parallel.ForEachAsync(Build.Mods, ParallelOptions, async (mod, ct) =>
                {
                    if (!await mod.TryLoadXmlFiles())
                    {
                        Fail();
                        return;
                    }
                });
                LoadXml.Complete();
                Log.Debug($"{LoadXml} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildDirectory);

                // Write modified version:
                if (!await Build.TryWriteVersion(Log, CT))
                    return false;

                // Merge XML:
                if (!await TryMergeXML())
                    return false;

                // Patch XML:
                if (!await TryPatchXML())
                    return false;

                // Fix Text entries:
                if (!await TryFixTexts())
                    return false;

                // Merge Audio:
                if (!await TryMergeAudio())
                    return false;

                // Generate Textures:
                if (!await TryGenerateTextures())
                    return false;

                // Save all XML files:
                foreach (XmlFile xmlFile in Build.XmlFile.Values)
                    if (!await xmlFile.TrySaveAsync(Log, CT))
                        return false;

                // Add mod authors and modding tools developers to credits section
                // Only for XML builds, since we don't want to unnecessarily touch spacehaven.jar
                // Also, ignore errors
                await TryComposeGameCredits();

                // Copy XML hash file:
                if (!await IOUtils.TryCopyFileAsync(Paths.BuildXmlHashPath, Paths.CacheXmlHashPath, true, Log, CT))
                    return false;
            }
            else Log.Success("XML build skipped");
            XmlBuild.Complete();

            // Always build the modifiedspacehaven.jar file:
            if (!await TryCreateModifiedSpaceHavenJarFile())
                return false;

            // Copy template config.json
            if (!await IOUtils.TryCopyFileAsync(Paths.TemplateConfigJsonPath, Paths.CacheConfigJsonPath, true, Log, CT))
                return false;

            // Create mods.json file for JAVA modders:
            if (!await TryWriteModsJson())
                return false;

            // Done.
            Log.Success($"{this} has completed", Paths.BuildDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BuildDirectory);
            Log.Error($"Build has failed", Paths.BuildDirectory);
            return false;
        }
        finally
        {
            // Dispose stuff here:
            if (Build != null)
            {
                try { await Build.DisposeAsync(); } catch { }
                Build = null;
            }
            if (FileLogger != null)
            {
                try
                {
                    await FileLogger.DisposeAsync();
                    Log.RemoveLogger(FileLogger);
                }
                catch { }
                FileLogger = null;
            }

            // Progress:
            Initialization?.RemoveAll();
            XmlBuild?.RemoveAll();
            JavaBuild?.RemoveAll();

            // XML Build:
            ResetXmlBuild?.Dispose();
            CopyTemplateFiles?.Dispose();
            LoadXml?.Dispose();
            FixTexts?.Dispose();
            MergeAudio?.Dispose();

            CollectSpriteRefs?.Dispose();
            LoadSprites?.Dispose();
            PackSprites?.Dispose();
            WriteSpriteSheets?.Dispose();
            WriteTextureXmlFiles?.Dispose();

            MergeXmlFiles?.Dispose();
            PatchXmlFiles?.Dispose();
            BuildJarFile?.Dispose();

            // JAVA Build:
            ResetJavaBuild?.Dispose();
            PrepareJavaFiles?.Dispose();
        }
    }



















    private async Task<bool> TrySetupProgress()
    {
        try
        {
            Clock.Restart();

            ResetXmlBuild = new ProgressInfo("Reset XML Build");
            XmlBuild.AddChild(ResetXmlBuild, 2000);

            CopyTemplateFiles = new ProgressInfo("Copy Template Files");
            XmlBuild.AddChild(CopyTemplateFiles, 2000);

            LoadXml = new ProgressInfo("Load Template Xml");
            XmlBuild.AddChild(LoadXml, 5000);

            FixTexts = new ProgressInfo("Fix Text Entries");
            XmlBuild.AddChild(FixTexts, 100);

            MergeAudio = new ProgressInfo("Merge Audio");
            XmlBuild.AddChild(MergeAudio, 100);

            CollectSpriteRefs = new ProgressInfo("Pack Textures");
            XmlBuild.AddChild(CollectSpriteRefs, 2270);

            LoadSprites = new ProgressInfo("Load Sprites");
            XmlBuild.AddChild(LoadSprites, 100);

            PackSprites = new ProgressInfo("Pack Sprites");
            XmlBuild.AddChild(PackSprites, 270);

            WriteSpriteSheets = new ProgressInfo("Write Sprite Sheets");
            XmlBuild.AddChild(WriteSpriteSheets, 430);

            WriteTextureXmlFiles = new ProgressInfo("Write Texture XML Files");
            XmlBuild.AddChild(WriteTextureXmlFiles, 100);

            MergeXmlFiles = new ProgressInfo("Merge XML files");
            XmlBuild.AddChild(MergeXmlFiles, 8000);

            PatchXmlFiles = new ProgressInfo("Patch XML files");
            XmlBuild.AddChild(PatchXmlFiles, 16000);

            BuildJarFile = new ProgressInfo("Create JAR file");
            XmlBuild.AddChild(BuildJarFile, 260);

            ResetJavaBuild = new ProgressInfo("Reset JAVA Build");
            JavaBuild.AddChild(ResetJavaBuild, 2000);

            PrepareJavaFiles = new ProgressInfo("Prepare JAVA Files");
            JavaBuild.AddChild(PrepareJavaFiles, 2000);

            // Done.
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BuildDirectory);
            return false;
        }
    }


















    private async Task<bool> TryInitialize()
    {
        try
        {
            Log.Debug("Initializing build...", Paths.BuildDirectory);
            Clock.Restart();

            // Initialize build data, and start logging build to file, right after the build directory reset:
            Build = new(Settings, Log);

            // Add mods:
            Build.AddMods(Settings.Mods);
            Initialization?.SetNormalized(0.45);

            // Load mod variables:
            foreach (ModBuildData mod in Build.Mods)
                if (!await mod.TryMapVariables())
                    return false;
            Initialization?.SetNormalized(0.75);

            // Compute build bypass:
            if (!await TryComputeHashes())
            {
                // In case of error, rebuild all:
                NeedsJavaBuild = true;
                NeedsXmlBuild = true;
            }
            Initialization?.Complete();

            // 'mods.json' file:
            Build.ModsJsonFile.AOPLibs.Add(ModdingConstants.ASPECTJ);
            Build.ModsJsonFile.AOPLibs.Add(ModdingConstants.ASPECTJWEAVER);
            Build.ModsJsonFile.AOPLibs.Sort();

            foreach (ModBuildData mod in Build.Mods)
            {
                ModInfo modInfo = new();
                modInfo.SchemaVersion = "1";
                modInfo.Name = mod.Name;
                modInfo.Version = mod.Version.ToString();
                modInfo.Directory = mod.Directory.AsStdPath();
                modInfo.ID = mod.ID;
                modInfo.Textures.AddRange(mod.SpritePaths.Select(path => path.AsStdPath()));
                modInfo.Audio.AddRange(mod.AudioFilePaths.Select(path => path.AsStdPath()));
                modInfo.Java.AddRange(mod.JavaFilePaths.Select(path => path.AsStdPath()));
                modInfo.Other.AddRange(mod.OtherFilePaths.Select(path => path.AsStdPath()));
                foreach (VarBuildData var in mod.Variables.Values)
                {
                    VarInfo varInfo = new();
                    varInfo.Name = var.Name;
                    varInfo.Type = var.Type;
                    varInfo.Value = var.StrValue;
                    modInfo.Vars.Add(varInfo);
                }
                Build.ModsJsonFile.Mods.Add(modInfo);
            }

            // Done.
            Log.Debug($"{Initialization} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildDirectory);
            Clock.Restart();

            // Done.
            Log.Success("Build initialization is complete", Paths.BuildDirectory);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Unable to initialize build: {ex}", Paths.BuildDirectory);
            return false;
        }
    }












    private async Task<bool> TryComputeHashes()
    {
        try
        {
            NeedsXmlBuild = true;
            NeedsJavaBuild = true;

            // Compute hashes from input:
            if (!await Build.ComputeHash())
                return false;

            // Write build hashes:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.BuildXmlHashPath, Build.XmlHash, Log, CT))
                return false;
            if (!await IOUtils.TryWriteAllTextAsync(Paths.BuildJavaHashPath, Build.JavaHash, Log, CT))
                return false;

            // JAR:
            string templateJarHash = IOUtils.FileExists(Paths.TemplateJarHashPath) ? await IOUtils.TryReadAllTextAsync(Paths.TemplateJarHashPath, Log, CT) : null;
            templateJarHash ??= string.Empty;
            string buildJarHash = IOUtils.FileExists(Paths.BuildJarHashPath) ? await IOUtils.TryReadAllTextAsync(Paths.BuildJarHashPath, Log, CT) : null;
            buildJarHash ??= string.Empty;
            string cacheJarHash = IOUtils.FileExists(Paths.CacheJarHashPath) ? await IOUtils.TryReadAllTextAsync(Paths.CacheJarHashPath, Log, CT) : null;
            cacheJarHash ??= string.Empty;
            IsNewJar = templateJarHash != buildJarHash || buildJarHash != cacheJarHash;

            // Is a new build required?
            NeedsXmlBuild = Build.HasXmlMods;
            NeedsJavaBuild = Build.HasJavaMods;
            if (Settings.SkipRebuilding)
            {
                NeedsXmlBuild &=
                    IsNewJar ||
                    !IOUtils.FileExists(Paths.CacheJarPath) ||
                    !IOUtils.FileExists(Paths.CacheJarHashPath) ||
                    (Build.XmlHash ?? string.Empty) != (await IOUtils.TryReadAllTextAsync(Paths.CacheXmlHashPath, Log, CT) ?? string.Empty);

                NeedsJavaBuild &=
                    !IOUtils.FileExists(Paths.CacheConfigJsonPath) ||
                    !IOUtils.FileExists(Paths.CacheJavaHashPath) ||
                    (Build.JavaHash ?? string.Empty) != (await IOUtils.TryReadAllTextAsync(Paths.CacheJavaHashPath, Log, CT) ?? string.Empty);
            }

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable to compute build bypass: {ex}");
            return false;
        }
    }










    private async Task<bool> TryResetXmlBuild()
    {
        try
        {
            Log.Info($@"Resetting XML build...");

            if (!await IOUtils.TryDeleteDirectoryContentAsync(Paths.BuildAudioDirectory, Log, CT))
                return false;
            ResetXmlBuild.SetNormalized(0.10);

            if (!await IOUtils.TryDeleteDirectoryContentAsync(Paths.BuildTexturesDirectory, Log, CT))
                return false;
            ResetXmlBuild.SetNormalized(0.20);

            if (!await IOUtils.TryDeleteDirectoryContentAsync(Paths.BuildMergeDirectory, Log, CT))
                return false;
            ResetXmlBuild.SetNormalized(0.50);

            if (!await IOUtils.TryDeleteDirectoryContentAsync(Paths.BuildPatchDirectory, Log, CT))
                return false;
            ResetXmlBuild.SetNormalized(0.80);

            if (!await IOUtils.TryDeleteDirectoryContentAsync(Paths.BuildStageDirectory, Log, CT))
                return false;

            // Done.
            ResetXmlBuild.Complete();
            Log.Debug($"{ResetXmlBuild} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable reset XML build: {ex}");
            return false;
        }
    }
















    private async Task<bool> TryMergeXML()
    {
        try
        {
            // Only merge supported files!
            Log.Info($@"Merging XML files: {SupportedXmlMergeFileTypes.Select(t => $"{t.ToString().ToLowerInvariant()}").JoinToString(", ")}...", Paths.BuildMergeDirectory);
            Clock.Restart();

            HashSet<XmlFile> mergedSpaceHavenXmlFiles = [];

            foreach (ModBuildData mod in Build.Mods)
            {
                CT.ThrowIfCancellationRequested();

                try
                {
                    ILogger modLog = mod.Log;

                    if (!mod.HasLibraryXml)
                    {
                        modLog.Debug($@"This mod has no XML library files", mod.Directory);
                        continue;
                    }

                    modLog.Debug($"Performing XML merge operations...", mod.Directory);

                    HashSet<XmlFile> mergedModXmlFiles = [];

                    // Only merge supported files!
                    foreach (EXmlFileType xmlFileType in SupportedXmlMergeFileTypes)
                    {
                        CT.ThrowIfCancellationRequested();

                        // Any such files in mod?
                        XmlFile[] modXmlFiles = mod.XmlFiles[xmlFileType].Values.ToArray();
                        if (modXmlFiles.Length <= 0)
                            continue;

                        // Get target file:
                        if (!Build.XmlFile.TryGetValue(xmlFileType, out XmlFile spaceHavenXmlFile))
                        {
                            modLog.Error($@"Unable to get target XML file of type '{xmlFileType}'", Paths.BuildStageDirectory);
                            return false;
                        }

                        // Merge with all mod library XML files:
                        foreach (XmlFile modXmlFile in modXmlFiles)
                        {
                            CT.ThrowIfCancellationRequested();

                            if (modXmlFile.IsIgnored)
                            {
                                modLog.Warn($@"Ignoring ""{modXmlFile}"" as defined by '{XmlFile.ATTRIBUTE_IGNORE}' attribute in root node", modXmlFile.Path);
                                continue;
                            }

                            // Merge by registered node type:
                            foreach (NodeType nodeType in NodeType.RegisteredTypes.Values.Where(n => n.XmlFileType == xmlFileType))
                            {
                                CT.ThrowIfCancellationRequested();

                                // Get parent node:
                                XElement parentNode = spaceHavenXmlFile.GetParentNode(nodeType);
                                if (parentNode == null)
                                {
                                    modLog.Error($"Unable to find target parent node with xpath '{nodeType.ParentXPath}' for registered node type '{nodeType}'", spaceHavenXmlFile.Path);
                                    return false;
                                }

                                // List all nodes:
                                List<XElement> nodes = modXmlFile.GetNodes(nodeType)?.ToList() ?? [];
                                if (nodes.Count <= 0)
                                    continue;

                                modLog.Debug($@"Merging {nodes.Count} node(s) of type '{nodeType}' from file ""{modXmlFile}""", modXmlFile.Path);

                                mergedSpaceHavenXmlFiles.Add(spaceHavenXmlFile);
                                mergedModXmlFiles.Add(modXmlFile);

                                foreach (XElement node in nodes)
                                {
                                    CT.ThrowIfCancellationRequested();

                                    // Strip XML comments out:
                                    node.DescendantNodesAndSelf().OfType<XComment>().Remove();

                                    // Read id and name:
                                    string key = nodeType.KeyAttribute != null ? node.Attribute(nodeType.KeyAttribute)?.Value : null;
                                    string src = $"{mod.Name}, {modXmlFile.RelativePath}, line {node.Line()}";

                                    // Replace existing nodes with same id OR same name:
                                    HashSet<XElement> existingNodes = [];
                                    if (!nodeType.KeyAttribute.IsNullOrWhiteSpace())
                                        existingNodes.AddRange(parentNode.Elements(node.Name)?.Where(n => n.Attribute(nodeType.KeyAttribute)?.Value == key) ?? []);

                                    string prettyPath = $"path='{nodeType.XPath}'";

                                    string prettyNewKey = key.IsNullOrWhiteSpace() ? null : $" {nodeType.KeyAttribute}={key}";
                                    string prettyNewSrc = $" src='{src}'";

                                    string prettyNewNode = $"new node [{prettyPath}{prettyNewKey}{prettyNewSrc}]";

                                    // Remove existing node(s):
                                    if (existingNodes.Count <= 0)
                                    {
                                        modLog.Debug($@"Adding {prettyNewNode}", modXmlFile.Path);
                                    }
                                    else
                                    {
                                        foreach (XElement existingNode in existingNodes)
                                        {
                                            CT.ThrowIfCancellationRequested();

                                            string existingKey = nodeType.KeyAttribute.IsNullOrWhiteSpace() ? null : existingNode.Attribute(nodeType.KeyAttribute)?.Value;
                                            string existingMod = existingNode.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value;
                                            if (existingMod.IsNullOrWhiteSpace()) existingMod = null;
                                            string existingSrc = existingNode.Attribute(NodeType.ATTRIBUTE_LIBRARY)?.Value;
                                            if (existingMod == null || existingSrc.IsNullOrWhiteSpace()) existingSrc = null;

                                            string prettyExistingKey = existingKey == null ? null : $" {nodeType.KeyAttribute}={existingKey}";
                                            string prettyExistingSrc = existingSrc == null ? null : $" src='{existingSrc}'";

                                            string prettyExistingNode = $"existing node [{prettyPath}{prettyExistingKey}{prettyExistingSrc}]";

                                            if (existingMod == null)
                                                modLog.Debug($@"Replacing {prettyExistingNode} with {prettyNewNode}", modXmlFile.Path);
                                            else if (existingMod == mod.Name)
                                                modLog.Warn($@"Replacing {prettyExistingNode} with {prettyNewNode}. The node was modified by the same mod => This could be an ERROR", modXmlFile.Path);
                                            else
                                                modLog.Warn($@"Replacing {prettyExistingNode} with {prettyNewNode} => This is a potential MOD INCOMPATIBILITY", modXmlFile.Path);
                                            existingNode.Remove();
                                        }
                                    }

                                    // MARK NODES <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                                    node.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                    if (xmlFileType == EXmlFileType.Animations)
                                    {
                                        // Mark animations assetPos nodes:
                                        foreach (XElement assetPos in node.DescendantsAndSelf("assetPos"))
                                        {
                                            assetPos.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                            assetPos.SetAttributeValue(NodeType.ATTRIBUTE_LIBRARY, src);
                                        }
                                    }
                                    else if (xmlFileType == EXmlFileType.Audio)
                                    {
                                        // Mark audio nodes:
                                        foreach (XElement a in node.DescendantsAndSelf("a"))
                                        {
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_LIBRARY, src);
                                        }
                                    }
                                    else if (xmlFileType == EXmlFileType.Textures)
                                    {
                                        // Mark sprite sheet nodes:
                                        foreach (XElement a in node.DescendantsAndSelf("t"))
                                        {
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_LIBRARY, src);
                                        }
                                        // Mark sprite nodes:
                                        foreach (XElement a in node.DescendantsAndSelf("re"))
                                        {
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_LIBRARY, src);
                                        }
                                    }

                                    // Add new node:
                                    parentNode.Add(new XElement(node));
                                }
                            }
                        }

                        // STRONG PERFORMANCE HIT => Maybe add options for generating detailed intermediary files?
                        // Save merged Space Haven XML file to mod merge directory:
                        //if (!await spaceHavenXmlFile.TrySaveToAsync(IOUtils.CombineAsOSPath(mod.BuildMergeDirectory, spaceHavenXmlFile.RelativePath), Log, CT))
                        //    return false;
                    }
                }
                finally
                {
                    // Done with this mod.
                    MergeXmlFiles?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            }

            // Save merged XML files to build merge directory:
            foreach (XmlFile spaceHavenXmlFile in mergedSpaceHavenXmlFiles)
            {
                if (!await spaceHavenXmlFile.TrySaveToAsync(IOUtils.CombineAsOSPath(Paths.BuildMergeDirectory, spaceHavenXmlFile.RelativePath), Log, CT))
                    return false;
                // Update line numbers:
                if (!await spaceHavenXmlFile.TryReparse(Log, CT))
                    return false;
            }

            // Done.
            MergeXmlFiles?.Complete();
            Log.Debug($"{MergeXmlFiles} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildMergeDirectory);
            Log.Success("XML merge completed", Paths.BuildMergeDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable to merge XML files: {ex}", Paths.BuildMergeDirectory);
            return false;
        }
    }


















    public async Task<bool> TryPatchXML()
    {
        try
        {
            Log.Info("Patching XML files...", Paths.BuildPatchDirectory);
            Clock.Restart();

            // Select mods with library XML files:
            foreach (ModBuildData mod in Build.Mods)
            {
                CT.ThrowIfCancellationRequested();

                HashSet<XmlFile> spaceHavenModifiedFiles = [];
                try
                {
                    ILogger modLog = mod.Log;

                    if (!mod.HasPatchXml)
                    {
                        modLog.Debug("This mod has no XML patch files", mod.Directory);
                        continue;
                    }

                    modLog.Debug("Performing XML patch operations...", mod.BuildPatchDirectory);

                    // Create mod patch dir:
                    if (!await IOUtils.TryCreateDirectoryAsync(mod.BuildPatchDirectory, modLog, CT))
                    {
                        modLog.Error($@"Unable to create directory: ""{mod.BuildPatchDirectory}""", Paths.BuildPatchDirectory);
                        return false;
                    }

                    foreach (XmlFile modPatchXmlFile in mod.XmlFiles[EXmlFileType.Patch].Values)
                    {
                        CT.ThrowIfCancellationRequested();

                        if (modPatchXmlFile.IsIgnored)
                        {
                            modLog.Warn($@"Ignoring ""{modPatchXmlFile}"" as defined by '{XmlFile.ATTRIBUTE_IGNORE}' attribute in root node", modPatchXmlFile.Path);
                            continue;
                        }

                        // Get the target XML file:
                        if (!XmlFile.TryGetPatchXmlFileType(modPatchXmlFile, out EXmlFileType targetXmlType))
                        {
                            modLog.Error($@"Unable to detect target XML file of patches in file ""{modPatchXmlFile}""", modPatchXmlFile.Path);
                            return false;
                        }

                        // Check for unsupported target patch file:
                        if (!SupportedXmlPatchFileTypes.Contains(targetXmlType))
                        {
                            modLog.Error($@"Patching '{targetXmlType}' with ""{modPatchXmlFile.Path}"" is not supported", modPatchXmlFile.Path);
                            return false;
                        }

                        // Get target file:
                        if (!Build.XmlFile.TryGetValue(targetXmlType, out XmlFile spaceHavenXmlFile))
                        {
                            modLog.Error($@"Unable to get target XML file of type '{targetXmlType}'", Paths.BuildStageDirectory);
                            return false;
                        }
                        spaceHavenModifiedFiles.Add(spaceHavenXmlFile);

                        // Start patching:
                        modLog.Debug($@"Executing patch operations from file ""{modPatchXmlFile}"" to the {spaceHavenXmlFile.FileName} file...", modPatchXmlFile.Path);

                        // Strip XML comments out:
                        modPatchXmlFile.Root.DescendantNodesAndSelf().OfType<XComment>().Remove();

                        // Iterate over patch nodes:
                        List<XElement> nodes = modPatchXmlFile.Root.Elements("Operation").ToList();
                        modLog.Debug($@"Processing {nodes.Count} patch nodes from ""{modPatchXmlFile}""...", modPatchXmlFile.Path);
                        foreach (XElement patchNode in nodes)
                        {
                            CT.ThrowIfCancellationRequested();

                            // Parse patch operation:
                            if (!XmlPatchOperation.TryCreate(modPatchXmlFile, mod.Variables, patchNode, out XmlPatchOperation patch, modLog))
                                return false;

                            // Skip if disabled by patch logic:
                            if (!patch.IsEnabled)
                            {
                                modLog.Info($@"Skipping DISABLED patch node {Environment.NewLine}{patch}", modPatchXmlFile.Path);
                                continue;
                            }

                            // Validate XPATH unevaluated variables:
                            if (patch.XPath.ContainsAny('{', '}'))
                                modLog.Warn($@"The evaluated XPATH '{patch.XPath}' could still contain undefined variables. {Environment.NewLine}{patch}", modPatchXmlFile.Path);

                            // Execute XPATH:
                            if (!spaceHavenXmlFile.TryRunXPath(patch.XPath, out List<XElement> targetNodes, modLog))
                            {
                                modLog.Error($@"Failed to execute the evaluated xpath='{patch.XPath}'. {Environment.NewLine}{patch}", modPatchXmlFile.Path);
                                return false;
                            }

                            // No target nodes?
                            if (targetNodes.Count <= 0)
                            {
                                modLog.Warn($@"The evaluated XPATH returned ZERO RESULTS => This could be an ERROR {Environment.NewLine}{patch}", modPatchXmlFile.Path);
                                continue; // Nothing else to do...
                            }

                            // Too many target nodes?
                            if (targetNodes.Count > 25)
                                modLog.Warn($@"The evaluated XPATH is targeting {targetNodes.Count} NODES => This could be an ERROR {Environment.NewLine}{patch}", modPatchXmlFile.Path);


                            // MARK NODES <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                            // (1) Attribute Operation
                            if (patch.IsAttributePatchOperation)
                            {
                                // Mark audio nodes:
                                if (targetXmlType == EXmlFileType.Audio)
                                {
                                    foreach (XElement targetNode in targetNodes.Where(n => n.Name == "a"))
                                    {
                                        string src = $"{mod.Name}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                        string targetAttribute = patch.PatchNode.Element(XmlPatchOperation.ATTRIBUTE)?.Value;
                                        if (targetAttribute == "filename" || targetAttribute == "mp3" || targetAttribute == "ogg")
                                        {
                                            targetNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                            targetNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                        }
                                    }
                                }

                                // Mark animations <assetPos> nodes which have the attribute 'filename':
                                else if (targetXmlType == EXmlFileType.Animations)
                                {
                                    foreach (XElement targetNode in targetNodes.Where(n => n.Name == "assetPos"))
                                    {
                                        string src = $"{mod.Name}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                        if (patch.PatchNode.Element(XmlPatchOperation.ATTRIBUTE)?.Value == "filename")
                                        {
                                            targetNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                            targetNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                        }
                                    }
                                }

                                // Mark texture nodes:
                                else if (targetXmlType == EXmlFileType.Textures)
                                {
                                    foreach (XElement targetNode in targetNodes.Where(n => n.Name == "t"))
                                    {
                                        string src = $"{mod.Name}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                        if (patch.PatchNode.Element(XmlPatchOperation.ATTRIBUTE)?.Value == "i")
                                        {
                                            targetNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                            targetNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                        }
                                    }
                                    foreach (XElement targetNode in targetNodes.Where(n => n.Name == "re"))
                                    {
                                        string src = $"{mod.Name}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                        if (patch.PatchNode.Element(XmlPatchOperation.ATTRIBUTE)?.Value == "n")
                                        {
                                            targetNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                            targetNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                        }
                                    }
                                }
                            }

                            // MARK NODES <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                            // (2) Node Operation
                            else if (patch.IsNodePatchOperation && patch.Operation != EPatchOperation.RemoveNode)
                            {
                                // Mark audio nodes:
                                if (targetXmlType == EXmlFileType.Audio)
                                {
                                    string src = $"{mod.Name}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                    foreach (XElement valueNode in patch?.PatchNode?.Element(XmlPatchOperation.VALUE)?.Descendants("a") ?? [])
                                    {
                                        valueNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                        valueNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                    }
                                }

                                // Mark animations <assetPos> nodes which have the attribute 'filename':
                                else if (targetXmlType == EXmlFileType.Animations)
                                {
                                    string src = $"{mod.Name}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                    foreach (XElement valueNode in patch?.PatchNode?.Element(XmlPatchOperation.VALUE)?.Descendants("assetPos").Where(n => n.Attribute("filename") != null) ?? [])
                                    {
                                        valueNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                        valueNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                    }
                                }

                                // Mark textures nodes:
                                else if (targetXmlType == EXmlFileType.Textures)
                                {
                                    string src = $"{mod.Name}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                    foreach (XElement valueNode in patch?.PatchNode?.Element(XmlPatchOperation.VALUE)?.Descendants("t") ?? [])
                                    {
                                        valueNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                        valueNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                    }
                                    foreach (XElement valueNode in patch?.PatchNode?.Element(XmlPatchOperation.VALUE)?.Descendants("re") ?? [])
                                    {
                                        valueNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.Name);
                                        valueNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                    }
                                }
                            }

                            // Perform patch operation:
                            if (!patch.TryRun(targetNodes, modLog))
                            {
                                modLog.Error($@"Patch operation has FAILED. {Environment.NewLine}{patch}", modPatchXmlFile.Path);
                                return false;
                            }

                            // Done with this patch operation.
                            modLog.Debug($@"Patch operation applied to {targetNodes.Count} target node(s). {Environment.NewLine}{patch}", modPatchXmlFile.Path);
                        }
                    }

                    // Save Space Haven XML files modified by this mod, for debugging:
                    foreach (XmlFile spaceHavenXmlFile in spaceHavenModifiedFiles)
                    {
                        // STRONG PERFORMANCE HIT => Maybe add options for generating detailed intermediary files?

                        //if (!await spaceHavenXmlFile.TrySaveToAsync(IOUtils.CombineAsOSPath(mod.BuildPatchDirectory, spaceHavenXmlFile.RelativePath), Log, CT))
                        //    return false;
                        // Save and reload to update line numbers of XML nodes:
                        //if (!await spaceHavenXmlFile.TrySaveAndReloadAsync(Log, CT))
                        //    return false;
                    }
                }
                finally
                {
                    PatchXmlFiles?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            }

            foreach (XmlFile spaceHavenXmlFile in Build.XmlFile.Values)
            {
                if (!await spaceHavenXmlFile.TrySaveToAsync(IOUtils.CombineAsOSPath(Paths.BuildPatchDirectory, spaceHavenXmlFile.RelativePath), Log, CT))
                    return false;
                // Update line numbers:
                if (!await spaceHavenXmlFile.TryReparse(Log, CT))
                    return false;
            }

            // Done.
            PatchXmlFiles?.Complete();
            Log.Debug($"{PatchXmlFiles} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildPatchDirectory);
            Log.Success("XML patch completed", Paths.BuildPatchDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable to patch XML files: {ex}", Paths.BuildPatchDirectory);
            return false;
        }
    }







    private async Task<bool> TryFixTexts()
    {
        try
        {
            Log.Info($@"Fixing TEXT entries...", Paths.BuildAudioDirectory);

            // Get texts document:
            XmlFile spaceHavenTextsXmlFile = Build.XmlFile[EXmlFileType.Texts];

            // Also save here, for debugging:
            if (!await spaceHavenTextsXmlFile.TrySaveToAsync(Paths.BuildTextsFile, Log, CT))
                return false;

            // Supported "languages
            string[] languages = Enum.GetNames<ELanguage>();
            const string emptyContent = " ";

            // Text entries, sorted descending by line number:
            // (since we will add nodes, the line number information shifts)
            try
            {
                (XElement t, int)[] nodes = spaceHavenTextsXmlFile.Root.Descendants("t").Select(t => (t, t.Line())).OrderByDescending(tuple => tuple.Item2).ToArray();
                foreach ((XElement t, int line) in nodes)
                {
                    CT.ThrowIfCancellationRequested();

                    if (!int.TryParse(t.Attribute("id")?.Value ?? "-1", out int id) || id <= 0)
                    {
                        Log.Error($"Invalid text entry with missing or invalid attribute id='{t.Attribute("id")?.Value}' in texts file at {line}", Paths.BuildTextsFile);
                        return false;
                    }

                    // Get content to be replicated:
                    XElement master = t.Element("EN") ?? t.Elements().FirstOrDefault();
                    string textContent = master?.Value ?? emptyContent;

                    // Add missing translations:
                    foreach (string language in languages)
                    {

                        XElement languageNode = t.Element(language);
                        if (languageNode == null)
                        {
                            Log.Debug($"Fixing text entry <t> id={id} with missing translation to language '{language}'", Paths.BuildTextsFile);
                            t.Add(languageNode = new XElement(language, textContent));
                        }
                        else if (languageNode.Value.IsNullOrEmpty())
                        {
                            Log.Debug($"Fixing text entry <t> id={id} with missing text content for translation to language '{language}'", Paths.BuildTextsFile);
                            languageNode.Value = textContent;
                        }
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                // Save texts document, for debugging:
                try { await spaceHavenTextsXmlFile.TrySaveToAsync(Paths.BuildTextsFile, Log, CT); }
                catch { }
                throw;
            }

            // Update line numbers:
            if (!await spaceHavenTextsXmlFile.TryReparse(Log, CT))
                return false;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to fix TEXT entries: {ex}", Paths.BuildAudioDirectory);
            return false;
        }
    }



    private async Task<bool> TryMergeAudio()
    {
        try
        {
            Log.Info($@"Merging AUDIO...", Paths.BuildAudioDirectory);
            Clock.Restart();

            // Get and save animations document, for debugging:
            XmlFile spaceHavenAudioXmlFile = Build.XmlFile[EXmlFileType.Audio];
            if (!await spaceHavenAudioXmlFile.TrySaveToAsync(Paths.BuildAudioFile, Log, CT))
                return false;

#warning TODO: Check for audio name collisions!

            // Collect all assetPos filename references and save it to spriteReference objects:
            bool errors = false;
            OrderedDictionary<int, AudioBuildData> audioById = []; // keep original order!
            OrderedDictionary<string, AudioBuildData> audioByName = []; // keep original order!
            List<XElement> audioNodes = spaceHavenAudioXmlFile.Root.Descendants("a").ToList();
            foreach (XElement audioNode in audioNodes)
            {
                CT.ThrowIfCancellationRequested();

                // Only modded audio:
                string owner = audioNode.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value;
                if (owner.IsNullOrWhiteSpace())
                    continue;

                // Get mod:
                ModBuildData mod = Build.Mods.FirstOrDefault(m => m.Name == owner);
                if (mod == null)
                {
                    Log.Error($@"Unable to find owner mod for audio entry at line {audioNode.Line()}", Paths.BuildAudioFile);
                    return false;
                }

                // Parse audio:
                AudioBuildData audio = new(Paths, mod, audioNode, mod.Log);
                if (!audio.TryParse(Build.Mods))
                {
                    errors = true;
                    continue;
                }

                // Audio entry uses original game audio:
                if (audio.IsOriginalAudioFile)
                    continue;

                // Check for duplicate audio ID:
                if (audioById.TryGetValue(audio.Id, out AudioBuildData existingAudio1))
                {
                    Log.Error($@"Duplicate audio ID: {Environment.NewLine}{existingAudio1} {Environment.NewLine}{audio}", Paths.BuildAudioFile);
                    return false;
                }
                else audioById[audio.Id] = audio;

                // Check for duplicate audio NAME:
                if (audioByName.TryGetValue(audio.Name, out AudioBuildData existingAudio2))
                {
                    Log.Error($@"Duplicate audio NAME: {Environment.NewLine}{existingAudio2} {Environment.NewLine}{audio}", Paths.BuildAudioFile);
                    return false;
                }
                else audioByName[audio.Name] = audio;
            }
            if (errors)
                return false;

            // Copy audio files:
            string escapedBuildStageDirectory = $"{Paths.BuildStageDirectory}{Path.DirectorySeparatorChar}";
            foreach (AudioBuildData audio in audioByName.Values)
            {
                string targetAbsolutePath = Path.GetFullPath(IOUtils.CombineAsOSPath(Paths.BuildStageDirectory, audio.TargetRelativePath)).AsOSPath();
                if (!targetAbsolutePath.StartsWith(escapedBuildStageDirectory, StringComparison.Ordinal))
                {
                    Log.Error($@"Invalid target audio file path ""{targetAbsolutePath}"" for {audio}");
                    return false;
                }

                if (!await IOUtils.TryCopyFileAsync(audio.SourceAbsolutePath, targetAbsolutePath, true, Log, CT))
                    return false;
            }

            // Done.
            MergeAudio?.Complete();
            Log.Debug($"{MergeAudio} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildAudioDirectory);
            Log.Success($"AUDIO files ready", Paths.BuildAudioDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to merge AUDIO : {ex}", Paths.BuildAudioDirectory);
            return false;
        }
    }











    private async Task<bool> TryGenerateTextures()
    {
        try
        {
            Log.Info($@"Generating TEXTURES...", Paths.BuildTexturesDirectory);
            Clock.Restart();

            // Get and save textures document, for debugging:
            XmlFile spaceHavenTexturesXmlFile = Build.XmlFile[EXmlFileType.Textures];
            if (!await spaceHavenTexturesXmlFile.TrySaveToAsync(Paths.BuildStageTexturesXmlPath, Log, CT))
                return false;

            // Get and save animations document, for debugging:
            XmlFile spaceHavenAnimationsXmlFile = Build.XmlFile[EXmlFileType.Animations];
            if (!await spaceHavenAnimationsXmlFile.TrySaveToAsync(Paths.BuildStageAnimationsXmlPath, Log, CT))
                return false;

            // Get last original game spritesheet ID:
            int lastOriginalSpriteSheetKey = Build.GetLastUsedKey(EKeyPool.TexturesCim);

            // Get last original game sprite ID:
            int lastOriginalSpriteName = Build.GetLastUsedKey(EKeyPool.TexturesRegion);

            // Get last original game sprite NAME:
            int lastOriginalSpriteId = Build.LastOriginalSpriteId;
            int lastUsedSpriteId = Build.LastOriginalSpriteId;

            // Collect all available sprite sheets:
            IReadOnlyList<string> spriteSheetPaths = Build.Mods.SelectMany(mod => mod.SpriteSheetPaths).OrderBy(path => path).ToArray();

            // Collect all available sprites:
            IReadOnlyList<string> textureFilePaths = Build.Mods.SelectMany(mod => mod.SpritePaths).OrderBy(path => path).ToArray();

            // Remap sprite sheet ID:
            SpriteAtlasBuildData cimAtlas = new("CIM");
            Dictionary<string, int> RemappedSpriteSheetIDs = [];
            List<XElement> modifiedSpriteSheetNodes =
                spaceHavenTexturesXmlFile.Root
                .Descendants("t")
                .Where(t => !(t.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value.IsNullOrWhiteSpace() ?? true))
                .ToList();
            foreach (XElement t in modifiedSpriteSheetNodes)
            {
                string modName = t.Attribute(NodeType.ATTRIBUTE_OWNER).Value;
                if (modName.IsNullOrWhiteSpace())
                    continue;

                // library operation (OPTIONAL):
                string libraryOperation = t?.Attribute(NodeType.ATTRIBUTE_LIBRARY)?.Value;
                // patch operation (OPTIONAL):
                string patchOperation = t?.Attribute(NodeType.ATTRIBUTE_PATCH)?.Value;
                // last operation:
                string lastOperation = patchOperation ?? libraryOperation ?? "unknown mod operation";
                // pretty print:

                string key = NodeType.TexturesCim.KeyAttribute;
                string spriteSheetKeyStr = t.Attribute(key)?.Value?.Trim();
                if (spriteSheetKeyStr.IsNullOrEmpty())
                {
                    Log.Error($@"Unable to remap ID of <t> sprite sheet node with {key}=""{spriteSheetKeyStr}"" in {spaceHavenTexturesXmlFile.FileName}, line={t.Line()}, owned by mod ""{modName}"" last modified by {lastOperation}", Paths.BuildStageTexturesXmlPath);
                    continue;
                }

                if (RemappedSpriteSheetIDs.ContainsKey(spriteSheetKeyStr))
                {
                    Log.Error($@"Unable ro remap <t> sprite sheet node with duplicate {key}=""{spriteSheetKeyStr}"" in {spaceHavenTexturesXmlFile.FileName}, line={t.Line()}, owned by mod ""{modName}"" last modified by {lastOperation}", Paths.BuildStageTexturesXmlPath);
                    continue;
                }

                int remappedSpriteSheetKey = Build.AllocateNextNumericId(EKeyPool.TexturesCim);
                RemappedSpriteSheetIDs[spriteSheetKeyStr] = remappedSpriteSheetKey;
                Log.Debug($@"Remapped <t> sprite sheet node from i=""{remappedSpriteSheetKey}"" to {key}=""{remappedSpriteSheetKey}"" in {spaceHavenTexturesXmlFile.FileName}, line={t.Line()}, owned by mod ""{modName}"" last modified by {lastOperation}", Paths.BuildStageTexturesXmlPath);

                if (spriteSheetKeyStr.TryParse(out int spriteSheetId) && spriteSheetId <= lastOriginalSpriteSheetKey)
                    continue;

                ModBuildData mod = Build.Mods.FirstOrDefault(mod => mod.Name == modName);
                if (mod == null)
                {
                    Log.Error($@"Unable to retrieve mod '{modName}' owning <t> sprite sheet node with {key}=""{spriteSheetKeyStr}"", in in {spaceHavenTexturesXmlFile.FileName}, line={t.Line()}", Paths.BuildStageAnimationsXmlPath);
                    continue;
                }

                // Check for exiting sprite image:
                string[] expectedAbsolutePaths =
                [
                    IOUtils.CombineAsOSPath(mod.SpritesDirectory, $"{spriteSheetKeyStr}.png"),
                    IOUtils.CombineAsOSPath(mod.SpritesDirectory, spriteSheetKeyStr),
                ];
                string absolutePath = mod.SpriteSheetPaths.FirstOrDefault(path => expectedAbsolutePaths.Any(expected => expected.Equals(path, StringComparison.Ordinal)));
                absolutePath ??= mod.SpriteSheetPaths.FirstOrDefault(path => expectedAbsolutePaths.Any(expected => expected.Equals(path, StringComparison.OrdinalIgnoreCase)));
                if (absolutePath == null)
                {
                    Log.Error($@"Unable to locate image file '{spriteSheetKeyStr}' for <t> sprite sheet node with {key}=""{spriteSheetKeyStr}"", in in {spaceHavenTexturesXmlFile.FileName}, line={t.Line()}", Paths.BuildStageAnimationsXmlPath);
                    return false;
                }

                SpriteSheetBuildData spriteSheet = new(remappedSpriteSheetKey, absolutePath, cimAtlas) { GlobalId = remappedSpriteSheetKey, };
                cimAtlas.Add(spriteSheet);
            }


            // Generate directly provided CIM files:
            await Parallel.ForEachAsync(cimAtlas.SpriteSheets, ParallelOptions, async (spriteSheet, ct) =>
            {
                try
                {
                    Log.Debug($"Generating sprite sheet {spriteSheet.GlobalId}...", Paths.BuildTexturesDirectory);

                    if (!spriteSheet.TryRenderFromSprites(Log))
                    {
                        Log.Error($@"Unable to generate sprite sheet '{spriteSheet.LocalId}'");
                        Fail();
                    }

                    string cimFilename = $"{spriteSheet.GlobalId}.cim";

                    // Export to CIM to build stage directory:
                    await spriteSheet.TryExportToCimAsync(IOUtils.CombineAsOSPath(Paths.BuildStageLibraryDirectory, cimFilename), Log, ct);

                    // Export to PNG, for debugging:
                    spriteSheet.TryExportToPng(IOUtils.CombineAsOSPath(Paths.BuildTexturesDirectory, $"{spriteSheet.GlobalId}.png"), Log, ct);
                }
                finally
                {
                    //lock (WriteSpriteSheets)
                    //    WriteSpriteSheets.IncrementNormalized((1.0 / spriteAtlas.SpriteSheets.Count) * (spriteRefs.Count / (double)spriteReferences.Count));
                }
            });

            // Remap sprite NAME:
            Dictionary<string, int> RemappedSpriteNames = [];
            List<XElement> modifiedSpriteNodes =
                spaceHavenTexturesXmlFile.Root
                .Descendants("re")
                .Where(re => !(re.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value.IsNullOrWhiteSpace() ?? true))
                .ToList();
            foreach (XElement re in modifiedSpriteNodes)
            {
                string modName = re.Attribute(NodeType.ATTRIBUTE_OWNER).Value;
                if (modName.IsNullOrWhiteSpace())
                    continue;

                // library operation (OPTIONAL):
                string libraryOperation = re?.Attribute(NodeType.ATTRIBUTE_LIBRARY)?.Value;
                // patch operation (OPTIONAL):
                string patchOperation = re?.Attribute(NodeType.ATTRIBUTE_PATCH)?.Value;
                // last operation:
                string lastOperation = patchOperation ?? libraryOperation ?? "unknown mod operation";
                // pretty print:

                // NAME remapping:
                {
                    string spriteNameStr = re.Attribute("n")?.Value?.Trim();
                    if (spriteNameStr.IsNullOrEmpty())
                    {
                        Log.Error($@"Unable to get sprite NAME for <re> sprite region node with n=""{spriteNameStr}"" in {spaceHavenTexturesXmlFile.FileName}, line={re.Line()}, owned by mod ""{modName}"" last modified by {lastOperation}", Paths.BuildStageTexturesXmlPath);
                        continue;
                    }

                    if (RemappedSpriteNames.ContainsKey(spriteNameStr))
                    {
                        Log.Error($@"Unable ro remap <re> sprite region node with duplicate n=""{spriteNameStr}"" in {spaceHavenTexturesXmlFile.FileName}, line={re.Line()}, owned by mod ""{modName}"" last modified by {lastOperation}", Paths.BuildStageTexturesXmlPath);
                        continue;
                    }

                    // Ignore original sprite names:
                    if (spriteNameStr.TryParse(out int spriteName) && spriteName <= lastOriginalSpriteName)
                    {
                        Log.Debug($@"Skipping remapping of original <re> sprite region node n=""{spriteNameStr}"", owned by mod ""{modName}"" last modified by {lastOperation}", Paths.BuildStageTexturesXmlPath);
                        continue;
                    }

                    int remappedSpriteName = Build.AllocateNextNumericId(EKeyPool.TexturesRegion);
                    RemappedSpriteNames[spriteNameStr] = remappedSpriteName;
                    Log.Debug($@"Remapped <re> sprite region node from n=""{spriteNameStr}"" to n=""{remappedSpriteName}"", owned by mod ""{modName}"" last modified by {lastOperation}", Paths.BuildStageTexturesXmlPath);
                }

                // ID remapping:
                {
                    string spriteIdStr = re.Attribute("id")?.Value?.Trim();
                    int remappedSpriteId = ++lastUsedSpriteId;
                    Log.Debug($@"Remapped <re> sprite region node from id=""{spriteIdStr}"" to id=""{remappedSpriteId}"", owned by mod ""{modName}"" last modified by {lastOperation}", Paths.BuildStageTexturesXmlPath);
                }
            }

            // Adjust animations assetPos "a" references to sprites with remapped names:
            List<XElement> allAssetPosNodes = spaceHavenAnimationsXmlFile.Root.Descendants("assetPos").ToList();
            foreach (XElement assetPos in allAssetPosNodes)
            {
                string remappedSpriteNameStr = assetPos?.Attribute("a")?.Value ?? string.Empty;
                if (!RemappedSpriteNames.TryGetValue(remappedSpriteNameStr, out int remappedSpriteName))
                    continue;

                Log.Debug($@"Remapped <assetPos> sprite from a=""{remappedSpriteNameStr}"" to id=""{remappedSpriteName}"", in {spaceHavenAnimationsXmlFile.FileName} line {assetPos.Line()}", Paths.BuildStageAnimationsXmlPath);
                assetPos.SetAttributeValue("a", remappedSpriteName);
            }



            // Collect all assetPos filename references and save it to spriteReference objects:
            bool errors = false;
            int localSpriteId = 0;
            SortedDictionary<string, SpriteReference> spriteReferences = [];
            foreach (XElement assetPos in allAssetPosNodes)
            {
                CT.ThrowIfCancellationRequested();
                try
                {
                    // filename reference:
                    XAttribute filenameAttribute = assetPos?.Attribute("filename");
                    if (filenameAttribute == null)
                        continue;
                    string assetPosFilenameReference = filenameAttribute.Value;
                    if (assetPosFilenameReference.IsNullOrWhiteSpace())
                    {
                        Log.Error($@"Invalid <assetPos> node with empty 'filename' reference, in animations file line {assetPos.Line()}", Paths.BuildStageAnimationsXmlPath);
                        errors = true;
                        continue;
                    }

                    // owner mod:
                    string modName = assetPos?.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value;
                    if (modName.IsNullOrWhiteSpace())
                    {
                        Log.Error($@"No '{NodeType.ATTRIBUTE_OWNER}' attribute exists in <assetPos> node with 'filename' reference ""{assetPosFilenameReference}"", in animations file line {assetPos.Line()}", Paths.BuildStageAnimationsXmlPath);
                        errors = true;
                        continue;
                    }
                    ModBuildData mod = Build.Mods.FirstOrDefault(mod => mod.Name == modName);
                    if (mod == null)
                    {
                        Log.Error($@"Unable to retrieve mod '{modName}' owning <assetPos> node with 'filename' reference ""{assetPosFilenameReference}"", in animations file line {assetPos.Line()}", Paths.BuildStageAnimationsXmlPath);
                        errors = true;
                        continue;
                    }

                    // filter (OPTIONAL):
                    string filterStr = assetPos?.Attribute("filter")?.Value;
                    if (!filterStr.TryParse(out ETextureFilter filter) && !filterStr.TryParseFromNumericValue(out filter))
                        filter = ETextureFilter.Nearest; // use 'nearest' as default filter

                    // library operation (OPTIONAL):
                    string libraryOperation = assetPos?.Attribute(NodeType.ATTRIBUTE_LIBRARY)?.Value;

                    // patch operation (OPTIONAL):
                    string patchOperation = assetPos?.Attribute(NodeType.ATTRIBUTE_PATCH)?.Value;

                    // last operation:
                    string lastOperation = patchOperation ?? libraryOperation ?? "unknown mod operation";

                    // pretty print:
                    string pretty = $@"<assetPos> with filename reference ""{assetPosFilenameReference}"" owned by mod ""{modName}"" last modified by {lastOperation}";
                    Log.Debug($@"Registering sprite reference for {pretty}", Paths.BuildStageAnimationsXmlPath);

                    // local name:
                    string localName = SpriteReference.GetLocalName(mod.Name, assetPosFilenameReference, filter);

                    // Get or create sprite reference:
                    if (!spriteReferences.TryGetValue(localName, out SpriteReference spriteRef))
                    {
                        spriteRef = new(localName, ++localSpriteId, mod, assetPosFilenameReference, filter);

                        // Add current assetPos reference:
                        spriteRef.AssetPosNodes.Add(assetPos);

                        // Check for exiting sprite image:
                        string absolutePath = textureFilePaths.FirstOrDefault(path => path.Equals(spriteRef.AbsolutePath, StringComparison.Ordinal));
                        absolutePath ??= textureFilePaths.FirstOrDefault(path => path.Equals(spriteRef.AbsolutePath, StringComparison.OrdinalIgnoreCase));
                        absolutePath ??= textureFilePaths.FirstOrDefault(path => path.Equals(spriteRef.AbsolutePathWithoutFileExtension, StringComparison.Ordinal));
                        absolutePath ??= textureFilePaths.FirstOrDefault(path => path.Equals(spriteRef.AbsolutePathWithoutFileExtension, StringComparison.OrdinalIgnoreCase));
                        if (absolutePath == null)
                        {
                            Log.Error($@"Unable to locate image file '{spriteRef.AbsolutePath}' for {pretty}", Paths.BuildStageAnimationsXmlPath);
                            errors = true;
                            continue;
                        }
                        spriteRef.AbsolutePath = absolutePath.AsOSPath(); // any case sensitiveness is adjusted here

                        // Add the created sprite reference:
                        spriteReferences.Add(localName, spriteRef);
                    }
                    else
                    {
                        // Add current assetPos reference:
                        spriteRef.AssetPosNodes.Add(assetPos);
                    }
                }
                finally
                {
                    CollectSpriteRefs.IncrementNormalized(1.0 / allAssetPosNodes.Count);
                }
            }
            if (errors)
                return false;

            Log.Debug($"{CollectSpriteRefs} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDirectory);
            CT.ThrowIfCancellationRequested();


            // Create sprite atlases by texture filter type, and pack sprites into sprite sheets:
            foreach (ETextureFilter filter in Enum.GetValues<ETextureFilter>().OrderByDescending(e => (int)e))
            {
                Clock.Restart();
                CT.ThrowIfCancellationRequested();

                List<SpriteReference> spriteRefs = spriteReferences.Values.Where(s => s.Filter == filter).ToList();
                if (spriteRefs.Count <= 0)
                {
                    Log.Info($"No mod sprite references were found requiring the texture filter '{filter}'", Paths.BuildTexturesDirectory);
                    continue;
                }
                CT.ThrowIfCancellationRequested();

                // Load sprite images:
                SortedDictionary<string, SpriteBuildData> sprites = [];
                await Parallel.ForEachAsync(spriteRefs, ParallelOptions, async (spriteRef, ct) =>
                {
                    try
                    {
                        Log.Debug($"Loading sprite {spriteRef.LocalName}...", Paths.BuildTexturesDirectory);

                        SpriteBuildData sprite = new(spriteRef.LocalName, spriteRef.LocalID, spriteRef.AbsolutePath);
                        lock (sprites)
                        {
                            sprites[spriteRef.LocalName] = sprite;
                            spriteRef.Sprite = sprite;
                        }
                    }
                    finally
                    {
                        lock (LoadSprites)
                            LoadSprites.IncrementNormalized(1.0 / spriteReferences.Count);
                    }
                });
                Log.Debug($"{LoadSprites} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDirectory);
                Clock.Restart();
                CT.ThrowIfCancellationRequested();

                // Assign global ID and global Name to each sprite:
                foreach (SpriteBuildData sprite in sprites.Values)
                {
                    sprite.GlobalName = Build.AllocateNextNumericId(EKeyPool.TexturesRegion).ToString();
                    sprite.GlobalId = ++lastUsedSpriteId;
                }
                CT.ThrowIfCancellationRequested();

                // Pack sprites into sprite sheets:
                using SpriteAtlasBuildData spriteAtlas = new(filter.ToString().ToUpperInvariant())
                {
                    SpriteSheetSize = filter == ETextureFilter.Linear ? 4096 : 2048,
                    SpriteSpacing = filter == ETextureFilter.Linear ? 0 : 4,
                };

                if (!await Task.Run(() => spriteAtlas.Add(sprites.Values, Log, CT)))
                {
                    Log.Error($@"Unable to pack sprites into sprite atlas '{spriteAtlas.Name}'", Paths.BuildStageAnimationsXmlPath);
                    return false;
                }
                PackSprites.IncrementNormalized(spriteRefs.Count / (double)spriteReferences.Count);
                Log.Debug($"{PackSprites} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDirectory);
                Clock.Restart();
                CT.ThrowIfCancellationRequested();

                // Assign global ID to each sprite sheet:
                foreach (SpriteSheetBuildData spriteSheet in spriteAtlas.SpriteSheets)
                    spriteSheet.GlobalId = Build.AllocateNextNumericId(EKeyPool.TexturesCim);
                CT.ThrowIfCancellationRequested();

                // Draw sprites to the sprite sheets, save sprite sheets as CIM and PNG:
                await Parallel.ForEachAsync(spriteAtlas.SpriteSheets, ParallelOptions, async (spriteSheet, ct) =>
                {
                    try
                    {
                        Log.Debug($"Generating sprite sheet {spriteSheet.GlobalId}...", Paths.BuildTexturesDirectory);

                        if (!spriteSheet.TryRenderFromSprites(Log))
                        {
                            Log.Error($@"Unable to generate sprite sheet '{spriteSheet.LocalId}'");
                            Fail();
                        }

                        string cimFilename = $"{spriteSheet.GlobalId}.cim";

                        // Export to CIM to build stage directory:
                        await spriteSheet.TryExportToCimAsync(IOUtils.CombineAsOSPath(Paths.BuildStageLibraryDirectory, cimFilename), Log, ct);

                        // Export to PNG, for debugging:
                        spriteSheet.TryExportToPng(IOUtils.CombineAsOSPath(Paths.BuildTexturesDirectory, $"{spriteSheet.GlobalId}.png"), Log, ct);
                    }
                    finally
                    {
                        lock (WriteSpriteSheets)
                            WriteSpriteSheets.IncrementNormalized((1.0 / spriteAtlas.SpriteSheets.Count) * (spriteRefs.Count / (double)spriteReferences.Count));
                    }
                });
                Log.Debug($"{WriteSpriteSheets} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDirectory);
                Clock.Restart();
                CT.ThrowIfCancellationRequested();

                // For each sprite sheet, add a CIM texture entry to the textures XML file:
                Log.Debug($@"Assigning IDs to new sprite sheets...", Paths.BuildTexturesDirectory);
                XElement parentTexturesCimNode = spaceHavenTexturesXmlFile.GetParentNode(NodeType.TexturesCim);
                foreach (SpriteSheetBuildData spriteSheet in spriteAtlas.SpriteSheets.OrderBy(s => s.LocalId))
                {
                    XElement t = new("t");
                    t.SetAttributeValue("i", spriteSheet.GlobalId);
                    t.SetAttributeValue("w", spriteSheet.Width);
                    t.SetAttributeValue("h", spriteSheet.Height);
                    t.SetAttributeValue("f", 1);
                    t.SetAttributeValue("min", (int)filter);
                    t.SetAttributeValue("max", (int)filter);
                    parentTexturesCimNode.Add(t);

                    WriteTextureXmlFiles.IncrementNormalized(0.1 * (1.0 / spriteAtlas.SpriteSheets.Count) * (spriteRefs.Count / (double)spriteReferences.Count));
                }

                // For each sprite, add a texture region to the textures XML file:
                XElement parentTexturesRegionNode = spaceHavenTexturesXmlFile.GetParentNode(NodeType.TexturesRegion);
                foreach (SpriteBuildData sprite in sprites.Values)
                {
                    CT.ThrowIfCancellationRequested();

                    XElement re = new("re");
                    re.SetAttributeValue("n", sprite.GlobalName);
                    re.SetAttributeValue("t", sprite.SpriteSheet.GlobalId);
                    re.SetAttributeValue("x", sprite.X);
                    re.SetAttributeValue("y", sprite.Y);
                    re.SetAttributeValue("w", sprite.Width);
                    re.SetAttributeValue("h", sprite.Height);
                    re.SetAttributeValue("id", sprite.GlobalId);
                    re.SetAttributeValue("_src", sprite.LocalName);
                    parentTexturesRegionNode.Add(re);

                    WriteTextureXmlFiles.IncrementNormalized(0.1 * (1.0 / sprites.Count) * (spriteRefs.Count / (double)spriteReferences.Count));
                }

                // For each assetPos of a sprite reference, set the a="..." attribute with the sprite name:
                foreach (SpriteReference spriteRef in spriteRefs)
                {
                    foreach (XElement assetPos in spriteRef.AssetPosNodes)
                        assetPos.SetAttributeValue("a", spriteRef.Sprite.GlobalName);

                    WriteTextureXmlFiles.IncrementNormalized(0.1 * (1.0 / spriteReferences.Count));
                }

                // Save current state of textures XML file:
                if (!await spaceHavenTexturesXmlFile.TrySaveToAsync(Paths.BuildStageTexturesXmlPath, Log, CT))
                    return false;
                WriteTextureXmlFiles.IncrementNormalized(0.3 * (1.0 / spriteReferences.Count));

                // Save current state of animations XML file:
                if (!await spaceHavenAnimationsXmlFile.TrySaveToAsync(Paths.BuildStageAnimationsXmlPath, Log, CT))
                    return false;
                WriteTextureXmlFiles.IncrementNormalized(0.4 * (1.0 / spriteReferences.Count));

                Log.Debug($"{WriteTextureXmlFiles} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDirectory);
            }

            // Done.
            Log.Success($"TEXTURES ready", Paths.BuildTexturesDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to generate TEXTURES: {ex}", Paths.BuildTexturesDirectory);
            return false;
        }
    }







    private async Task<bool> TryComposeGameCredits()
    {
        try
        {
            Log.Debug($"Adding mod authors to '{SpaceHavenConstants.EXTRA_CREDITS_TXT}' file...", Paths.CacheDirectory);

            StringBuilder sb = new();

            // Mod authors:
            List<string> authors = [];
            foreach (string author in Build.Mods.Select(m => m.Author).Distinct())
                if (!authors.Any(a => a.Equals(author, StringComparison.OrdinalIgnoreCase)))
                    authors.Add(author);
            authors = authors.OrderBy(s => s.ToLowerInvariant()).ToList();
            if (authors.Count > 0)
            {
                sb.AppendLine("[Topic]Mod Authors");
                foreach (string author in authors)
                    sb.AppendLine(author);
                sb.AppendLine();
            }

            // Space Haven Launcher devs and maintainers:
            sb.AppendLine("[Topic]Space Haven Launcher");
            sb.AppendLine("Ghostkeeper666");
            sb.AppendLine("KaiserManny");
            sb.AppendLine();

            // Add original content now:
            sb.AppendLine(await IOUtils.TryReadAllTextAsync(Paths.TemplateExtraCreditsTxtPath, Log, CT) ?? string.Empty);

            // Save File:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.BuildStageExtraCreditsTxtPath, sb.ToString(), Log, CT))
                return false;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose final '{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' file: {ex}", Paths.CacheDirectory);
            return false;
        }
    }





    private async Task<bool> TryCreateModifiedSpaceHavenJarFile()
    {
        try
        {
            Log.Info($"Composing '{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' file...", Paths.CacheDirectory);
            Clock.Restart();

            // Select files to add to template JAR:
            DirectoryInfo di = new(Paths.BuildStageDirectory);
            FileInfo[] files = di.GetFiles("*.*", SearchOption.AllDirectories);
            JarAppender jar = new();
            if (!await jar.AppendTo(Paths.TemplateJarPath, Paths.CacheJarPath, Paths.BuildStageDirectory, files, Log, ParallelOptions))
                return false;

            BuildJarFile.SetNormalized(0.85);

            // Calculate hash of modified JAR:
            string hash = await XxHash64Calculator.ComputeFromFileAsync(Paths.CacheJarPath, Log, CT);
            if (!await IOUtils.TryWriteAllTextAsync(Paths.CacheModifiedJarHashPath, hash, Log, CT))
                return false;

            // Copy original JAR hash file:
            if (!await IOUtils.TryCopyFileAsync(Paths.TemplateJarHashPath, Paths.BuildJarHashPath, true, Log, CT))
                return false;
            if (!await IOUtils.TryCopyFileAsync(Paths.BuildJarHashPath, Paths.CacheJarHashPath, true, Log, CT))
                return false;

            BuildJarFile.SetNormalized(0.90);

            // Write the required class paths to jars.txt so the Bootstrap class can load them:
            List<string> classPaths = [];
            classPaths.AddRange(
                Build.Mods
                .Where(m => m.IsJavaMod)
                .SelectMany(m => m.JavaFilePaths)
                .Where(path => path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) && IOUtils.FileExists(path))
                .Select(path => path.AsStdPath())
                .ToList());

            string jarsTxtPath = IOUtils.CombineAsOSPath(Paths.CacheDirectory, "jars.txt");
            if (!await IOUtils.TryWriteAllTextAsync(jarsTxtPath, classPaths.JoinToString("\r\n"), Log, CT))
                return false;

            BuildJarFile.SetNormalized(0.95);

            // Deploy aop / java agent libs to cache dir:
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, ModdingConstants.ASPECTJ), IOUtils.CombineAsOSPath(Paths.CacheDirectory, ModdingConstants.ASPECTJ), true, Log, CT))
                return false;
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, ModdingConstants.ASPECTJWEAVER), IOUtils.CombineAsOSPath(Paths.CacheDirectory, ModdingConstants.ASPECTJWEAVER), true, Log, CT))
                return false;
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, "LauncherAgent.jar"), IOUtils.CombineAsOSPath(Paths.CacheDirectory, "LauncherAgent.jar"), true, Log, CT))
                return false;

            // Done.
            BuildJarFile?.Complete();
            Log.Debug($"{BuildJarFile} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.CacheDirectory);
            Log.Success($"'{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' is ready", Paths.CacheDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose final '{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' file: {ex}", Paths.CacheDirectory);
            return false;
        }
    }











    private async Task<bool> TryWriteModsJson()
    {
        try
        {
            string content = Build.ModsJsonFile.ToJsonString();

            // Build directory:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.BuildModsJsonPath, content, Log, CT))
                return false;

            // Cache directory:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.CacheModsJsonPath, content, Log, CT))
                return false;

            //Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log?.Error($"Unable to create {SpaceHavenConstants.CONFIG_JSON}: {ex}", Paths.CacheDirectory);
            return false;
        }
    }











    #region IAsyncDisposable
    public volatile bool IsDisposed;
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        try { await FileLogger.DisposeAsync(); } catch { }
    }
    #endregion
}
