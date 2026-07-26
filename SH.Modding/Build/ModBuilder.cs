using SH.Content;
using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Framework.Tasks;
using SH.Modding.Models;
using SkiaSharp;
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
    private readonly BuildSettings BuildSettings;
    private PathData Paths => BuildSettings.Paths;

    private readonly ILogger Log;

    private BuildInfo Build;
    private ParallelOptions ParallelOptions => BuildSettings.ParallelOptions;
    private CancellationTokenSource CTS => BuildSettings.InternalCTS;
    private CancellationToken CT => BuildSettings.CT;

    public bool needsNewJar { get; private set; }
    public bool NeedsXmlBuild { get; private set; }
    public bool NeedsJavaBuild { get; private set; }


    private IProgressInfo Initialization => BuildSettings.InitializationProgress;
    private IProgressInfo XmlBuild => BuildSettings.XmlBuildProgress;
    private IProgressInfo JavaBuild => BuildSettings.JavaBuildProgress;

    private IProgressInfo ResetBuildStage;
    private IProgressInfo LoadSpaceHavenXml;
    private IProgressInfo ResetXmlBuild;
    private IProgressInfo MergeXml;
    private IProgressInfo PatchXml;
    private IProgressInfo ComposeAudio;
    private IProgressInfo ComposeTextures;
    private IProgressInfo FixTexts;
    private IProgressInfo DeployJavaHash;
    private IProgressInfo DeployXmlHash;
    private IProgressInfo WriteVersionInfo;
    private IProgressInfo ComposeCredits;
    private IProgressInfo WriteSpaceHavenXml;
    private IProgressInfo ComposeSpaceHavenJar;
    private IProgressInfo ComposeModsJson;

    private IProgressInfo ComposeTextures_LoadPredefinedSpriteSheets;
    private IProgressInfo ComposeTextures_LoadPredefinedSprites;
    private IProgressInfo ComposeTextures_WritePredefinedSpriteSheets;
    private IProgressInfo ComposeTextures_ReadReferencedSprites;
    private IProgressInfo ComposeTextures_LoadReferencedSprites;
    private IProgressInfo ComposeTextures_PackReferencedSprites;
    private IProgressInfo ComposeTextures_WriteReferencedSpriteSheets;
    private IProgressInfo ComposeTextures_ComposeTexturesXml;






    public ModBuilder(BuildSettings settings, ILogger log)
    {
        BuildSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        Log = log ?? new VoidLogger();
    }






    private void Fail() =>
        BuildSettings.Fail();



    private void OnProgressChanged(object sender, ProgressEventArgs e)
    {
        //try
        //{
        //    if (e.Progress.HasCompleted)
        //        Log.Success($"{e?.Progress?.Name} completed within {e?.Progress?.Clock?.ElapsedMilliseconds ?? -1} ms");
        //}
        //catch (Exception ex)
        //{ 
        //    Debug.WriteLine(ex.ToString());
        //}
    }




    private async Task<bool> TryInitializeProgressAsync()
    {
        // TODO: auto-estimation of progess weights!
        try
        {
            // STARTUP:
            ResetBuildStage = new ProgressInfo("Reset Build Stage") { Max = 10 };
            ResetBuildStage.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(ResetBuildStage, 175);
            JavaBuild.AddChild(ResetBuildStage, 175);

            LoadSpaceHavenXml = new ProgressInfo("Load Space Haven XML") { Max = 10 };
            LoadSpaceHavenXml.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(LoadSpaceHavenXml, 1000);
            JavaBuild.AddChild(LoadSpaceHavenXml, 1000);



            // JAVA BUILD:
            DeployJavaHash = new ProgressInfo("Deploy JAVA Hash") { Max = 10 };
            DeployJavaHash.ProgressChanged += OnProgressChanged;
            JavaBuild.AddChild(DeployJavaHash, 1);



            // XML BUILD:
            ResetXmlBuild = new ProgressInfo("Reset XML Build") { Max = 10 };
            ResetXmlBuild.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(ResetXmlBuild, 1);

            MergeXml = new ProgressInfo("Merge XML") { Max = 10 };
            MergeXml.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(MergeXml, 20);

            PatchXml = new ProgressInfo("Patch XML") { Max = 10 };
            PatchXml.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(PatchXml, 25);

            ComposeAudio = new ProgressInfo("Compose Audio") { Max = 10 };
            ComposeAudio.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(ComposeAudio, 1);

            ComposeTextures_LoadPredefinedSpriteSheets = new ProgressInfo("Compose Textures: Load Predefined Sprite Sheets");
            ComposeTextures_LoadPredefinedSpriteSheets.ProgressChanged += OnProgressChanged;

            ComposeTextures_LoadPredefinedSprites = new ProgressInfo("Compose Textures: Load Predefined Sprites");
            ComposeTextures_LoadPredefinedSprites.ProgressChanged += OnProgressChanged;

            ComposeTextures_WritePredefinedSpriteSheets = new ProgressInfo("Compose Textures: Write Predefined Sprite Sheets");
            ComposeTextures_WritePredefinedSpriteSheets.ProgressChanged += OnProgressChanged;

            ComposeTextures_ReadReferencedSprites = new ProgressInfo("Compose Textures: Read Referenced Sprites");
            ComposeTextures_ReadReferencedSprites.ProgressChanged += OnProgressChanged;

            ComposeTextures_LoadReferencedSprites = new ProgressInfo("Compose Textures: Load Referenced Sprites");
            ComposeTextures_LoadReferencedSprites.ProgressChanged += OnProgressChanged;

            ComposeTextures_PackReferencedSprites = new ProgressInfo("Compose Textures: Pack Referenced Sprites");
            ComposeTextures_PackReferencedSprites.ProgressChanged += OnProgressChanged;

            ComposeTextures_WriteReferencedSpriteSheets = new ProgressInfo("Compose Textures: Write Referenced Sprite Sheets");
            ComposeTextures_WriteReferencedSpriteSheets.ProgressChanged += OnProgressChanged;

            ComposeTextures_ComposeTexturesXml = new ProgressInfo("Compose Textures: Compose textures.xml");
            ComposeTextures_ComposeTexturesXml.ProgressChanged += OnProgressChanged;

            ComposeTextures = new ProgressInfo("Compose Textures",
            [
                (ComposeTextures_LoadPredefinedSpriteSheets, 200),
                (ComposeTextures_LoadPredefinedSprites, 1200),
                (ComposeTextures_WritePredefinedSpriteSheets, 9000),
                (ComposeTextures_ReadReferencedSprites, 10),
                (ComposeTextures_LoadReferencedSprites, 10),
                (ComposeTextures_PackReferencedSprites, 50),
                (ComposeTextures_WriteReferencedSpriteSheets, 400),
                (ComposeTextures_ComposeTexturesXml, 400),
            ])
            { Max = 10 };
            ComposeTextures.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(ComposeTextures, 27000);

            FixTexts = new ProgressInfo("Fix Texts") { Max = 10 };
            FixTexts.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(FixTexts, 110);

            DeployXmlHash = new ProgressInfo("Deploy Xml Hash") { Max = 10 };
            DeployXmlHash.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(DeployXmlHash, 1);



            // DEPLOYMENT:
            WriteVersionInfo = new ProgressInfo("Write Version Info") { Max = 10 };
            WriteVersionInfo.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(WriteVersionInfo, 1);
            JavaBuild.AddChild(WriteVersionInfo, 1);

            ComposeCredits = new ProgressInfo("Compose Credits") { Max = 10 };
            ComposeCredits.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(ComposeCredits, 2);
            JavaBuild.AddChild(ComposeCredits, 2);

            WriteSpaceHavenXml = new ProgressInfo("Write Space Haven XML") { Max = 10 };
            WriteSpaceHavenXml.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(WriteSpaceHavenXml, 1000);
            JavaBuild.AddChild(WriteSpaceHavenXml, 1000);

            ComposeSpaceHavenJar = new ProgressInfo("Compose spacehaven.jar") { Max = 10 };
            ComposeSpaceHavenJar.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(ComposeSpaceHavenJar, 1500);
            JavaBuild.AddChild(ComposeSpaceHavenJar, 1500);

            ComposeModsJson = new ProgressInfo("Compose mods.json") { Max = 10 };
            ComposeModsJson.ProgressChanged += OnProgressChanged;
            XmlBuild.AddChild(ComposeModsJson, 25);
            JavaBuild.AddChild(ComposeModsJson, 25);



            // Done.
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BuildDir);
            return false;
        }
    }


    public async Task<bool> TryBuildAsync()
    {
        try
        {
            Log.Info($"Starting mod build...", Paths.BuildDir);
            Stopwatch totalTime = Stopwatch.StartNew();

            Build = new BuildInfo(BuildSettings, Log);

            // No mods?
            if ((BuildSettings?.Mods?.Count ?? 0) <= 0)
            {
                Log.Error("There are no mods enabled");
                return false;
            }

            // Progress setup:
            if (!await TryInitializeProgressAsync())
                return false;

            // Build Initialization:
            if (!await TryInitializeAsync())
                return false;




            // Skip JAVA build?
            if (NeedsJavaBuild)
                JavaBuild.Start();
            else
            {
                Log.Info("JAVA build skipped");
                JavaBuild.RemoveAll(); // avoid further updates from children
                JavaBuild.Complete();
            }

            // Skip XML build?
            if (NeedsXmlBuild)
                XmlBuild.Start();
            else
            {
                Log.Info("XML build skipped");
                XmlBuild.RemoveAll(); // avoid further updates from children
                XmlBuild.Complete();
            }

            // All builds skipped?
            if (!NeedsJavaBuild && !NeedsXmlBuild)
            {
                Log.Success($"Mod build was VALIDATED and SKIPPED in {(int)totalTime.Elapsed.TotalSeconds}s", Paths.BuildDir);
                return true;
            }




            // Build Stage initialization:
            if (NeedsXmlBuild || (NeedsJavaBuild && !Build.HasXmlMods))
            {
                // Reset build stage files:
                ResetBuildStage.Start();
                Log.Info("Resetting build stage files...");
                if (!await TryResetBuildStageDirAsync())
                    return false;
                if (!await IOUtils.TryCopyDirAsync(Paths.TemplateStageDir, Paths.BuildStageDir, true, Log, ParallelOptions))
                    return false;
                ResetBuildStage.Complete();
            }
            else
            {
                // If a JAVA build is required, but XML mods exist and don't need a new XML build,
                // then it's because build stage dir already contains files from previous XML build.

                // Reuse build stage:
                Log.Info("Reusing build stage files");
                ResetBuildStage.Start();
                ResetBuildStage.Complete();
            }




            // Space Haven XML:
            if (NeedsJavaBuild || NeedsXmlBuild)
            {
                // Read Space Haven XML files:
                if (!await Build.TryLoadSpaceHavenXmlFilesAsync(CT, LoadSpaceHavenXml))
                    return false;
            }




            // JAVA specific:
            if (NeedsJavaBuild)
            {
                // Deploy JAVA hash:
                DeployJavaHash.Start();
                if (!await IOUtils.TryCopyFileAsync(Paths.BuildJavaHashPath, Paths.CacheJavaHashPath, true, Log, CT))
                    return false;
                DeployJavaHash.Complete();

                Log.Info("JAVA build has completed");
            }




            // XML specific:
            if (NeedsXmlBuild)
            {
                // Clear XML build directories:
                if (!await TryResetXmlBuildDirectories())
                    return false;

                // Merge and Patch XML:
                if (!await TryBuildXml())
                    return false;

                // Deploy XML hash:
                DeployXmlHash.Start();
                if (!await IOUtils.TryCopyFileAsync(Paths.BuildXmlHashPath, Paths.CacheXmlHashPath, true, Log, CT))
                    return false;
                DeployXmlHash.Complete();

                Log.Info("XML build has completed");
            }




            // Final deployment to cache directory:
            if (NeedsJavaBuild || NeedsXmlBuild)
            {
                // Write version to haven.xml AND to version.txt:
                if (!await TryWriteVersionInfoAsync(CT))
                    return false;

                // Credits.txt:
                await TryWriteCreditsAsync();

                // Save all XML files to build stage:
                WriteSpaceHavenXml.Start();
                await Parallel.ForEachAsync(Build.XmlFile.Values, BuildSettings.ParallelOptions, async (xmlFile, ct) =>
                {
                    if (!await xmlFile.TrySaveAsync(Log, ct))
                        throw new StopException("Unable to save game XML files", Paths.CacheDir, CTS);
                    lock(WriteSpaceHavenXml)
                        WriteSpaceHavenXml.IncrementNormalized(1.0 / Build.XmlFile.Count);
                });
                WriteSpaceHavenXml.Complete();

                // Create/Deploy the modified spacehaven.jar:
                if (!await TryWriteSpaceHavenJarAsync())
                    return false;

                // Create mods.json:
                if (!await TryComposeModsJsonAsync())
                    return false;
            }




            // Build completed:
            if (NeedsJavaBuild)
                JavaBuild.Complete();
            if (NeedsXmlBuild)
                XmlBuild.Complete();

            // Done.
            Log.Success($"Mod build has COMPLETED in {(int)totalTime.Elapsed.TotalSeconds}s", Paths.BuildDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BuildDir);
            Log.Error($"Build has failed", Paths.BuildDir);
            return false;
        }
    }












    private async Task<bool> TryInitializeAsync()
    {
        try
        {
            Log.Debug("Initializing mod build...", Paths.BuildDir);
            Initialization?.Start();

            // Initialize build data, and start logging build to file, right after the build directory reset:
            Build = new(BuildSettings, Log);

            // Add mods:
            Build.AddMods(BuildSettings.Mods);
            Initialization?.SetNormalized(0.45);

            // Load mod variables:
            foreach (Mod mod in Build.Mods)
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
            Initialization?.SetNormalized(0.95);

            // Done.
            Initialization?.Complete();
            Log.Info("Mod build initialization is complete", Paths.BuildDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to initialize mod build: {ex}", Paths.BuildDir);
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

            // BUILD!
            if (!BuildSettings.SkipRebuilding)
                return true;

            // By mods?
            NeedsXmlBuild = Build.HasXmlMods;
            NeedsJavaBuild = Build.HasJavaMods;

            // By JAR?
            string templateJarHash = IOUtils.FileExists(Paths.TemplateJarHashPath) ? await IOUtils.TryReadAllTextAsync(Paths.TemplateJarHashPath, Log, CT) : null;
            templateJarHash ??= string.Empty;
            string cacheJarHash = IOUtils.FileExists(Paths.CacheJarHashPath) ? await IOUtils.TryReadAllTextAsync(Paths.CacheJarHashPath, Log, CT) : null;
            cacheJarHash ??= string.Empty;
            needsNewJar = templateJarHash != cacheJarHash || cacheJarHash.IsNullOrWhiteSpace() || !IOUtils.FileExists(Paths.CacheJarPath) || !IOUtils.FileExists(Paths.CacheJarHashPath);
            if (needsNewJar)
            {
                Log.Debug($"A new {SpaceHavenConstants.SpaceHavenName} must be generated");
                NeedsXmlBuild = true;
                NeedsJavaBuild = true;
                return true;
            }

            // By XML inputs?
            string prevXmlHash = await IOUtils.TryReadAllTextAsync(Paths.CacheXmlHashPath, Log, CT) ?? string.Empty;
            NeedsXmlBuild = Build.XmlHash != prevXmlHash;

            // By JAVA inputs?
            string prevJavaHash = await IOUtils.TryReadAllTextAsync(Paths.CacheJavaHashPath, Log, CT) ?? string.Empty;
            NeedsJavaBuild = Build.JavaHash != prevJavaHash;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable to compute mod build hashes: {ex}");
            return false;
        }
    }










    private async Task<bool> TryResetBuildStageDirAsync()
    {
        try
        {
            Log.Info($@"Resetting build stage directory...");

            if (!await IOUtils.TryDeleteDirContentAsync(Paths.BuildStageDir, Log, CT))
                return false;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable reset build stage directory: {ex}");
            return false;
        }
    }











    private async Task<bool> TryResetXmlBuildDirectories()
    {
        try
        {
            Log.Info($@"Resetting XML build...");
            ResetXmlBuild.Start();

            if (!await IOUtils.TryDeleteDirContentAsync(Paths.BuildAudioDir, Log, CT))
                return false;

            ResetXmlBuild.SetNormalized(0.10);

            if (!await IOUtils.TryDeleteDirContentAsync(Paths.BuildTexturesDir, Log, CT))
                return false;

            ResetXmlBuild.SetNormalized(0.20);

            if (!await IOUtils.TryDeleteDirContentAsync(Paths.BuildMergeDir, Log, CT))
                return false;

            ResetXmlBuild.SetNormalized(0.50);

            if (!await IOUtils.TryDeleteDirContentAsync(Paths.BuildPatchDir, Log, CT))
                return false;

            // Done.
            ResetXmlBuild.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable reset XML build: {ex}");
            return false;
        }
    }













    private async Task<bool> TryBuildXml()
    {
        try
        {
            Log.Info($@"Merging and patching XML files...", Paths.BuildDir);
            MergeXml.Start();
            PatchXml.Start();

            EXmlFileType[] xmlFileTypes =
            [
                EXmlFileType.Haven,
                EXmlFileType.Animations,
                EXmlFileType.Textures,
                EXmlFileType.Texts,
                EXmlFileType.Audio,
                EXmlFileType.SpaceHavenSettings,
            ];

            SortedSet<EXmlFileType> completed = [];

            // Only merge supported files!
            await Parallel.ForEachAsync(xmlFileTypes, BuildSettings.ParallelOptions, async (targetXmlFileType, ct) =>
            {
                ct.ThrowIfCancellationRequested();

                
                // Get target file:
                if (!Build.XmlFile.TryGetValue(targetXmlFileType, out XmlFile spaceHavenXmlFile))
                    throw new StopException($@"Unable to locate space haven target XML file of type '{targetXmlFileType}' in ""{Paths.BuildStageDir}""", Paths.BuildStageDir, CTS);


                // For each mod, MERGE and PATCH:
                foreach (Mod mod in Build.Mods)
                {
                    // Get mod-specific logger:
                    ILogger modLog = mod.Log;


                    // Get MERGE files:
                    if(!await mod.TryLoadLibraryXmlFilesAsync(targetXmlFileType))
                        throw new StopException($@"[{mod}] Unable to load mod LIBRARY XML files of type '{targetXmlFileType}'", mod.Dir, CTS);

                    XmlFile[] mergeFiles =
                        mod.LibraryXmlFiles[targetXmlFileType].Values
                        .OrderBy(xmlFile => xmlFile.Path.AsStdPath().ToLowerInvariant())
                        .ToArray();

                    // MERGE
                    if (mergeFiles.Length > 0)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Create mod merge dir:
                        if (!await IOUtils.TryCreateDirAsync(mod.BuildMergeDir, modLog, CT))
                            throw new StopException($@"[{mod}] Unable to create directory: ""{mod.BuildMergeDir}""", Paths.BuildPatchDir, CTS);

                        // Merge each file:
                        modLog.Debug($@"Performing {targetXmlFileType} XML merge operations...", mod.Dir);
                        foreach (XmlFile modXmlFile in mergeFiles)
                        {
                            ct.ThrowIfCancellationRequested();

                            // Ignore?
                            if (modXmlFile.IsIgnored)
                            {
                                modLog.Warn($@"Ignoring ""{modXmlFile}"" as defined by '{XmlFile.ATTRIBUTE_IGNORE}' attribute in root node", modXmlFile.Path);
                                continue;
                            }

                            // Merge by registered node type:
                            foreach (NodeType nodeType in NodeType.RegisteredTypes.Values.Where(n => n.XmlFileType == targetXmlFileType))
                            {
                                ct.ThrowIfCancellationRequested();

                                // Get parent node:
                                XElement parentNode = spaceHavenXmlFile.GetParentNode(nodeType) ??
                                    throw new StopException($@"[{mod}] Unable to find target parent node with xpath '{nodeType.ParentXPath}' for registered node type '{nodeType}'", spaceHavenXmlFile.Path, CTS);

                                // List all nodes:
                                List<XElement> nodes = modXmlFile.GetNodes(nodeType)?.ToList() ?? [];
                                if (nodes.Count <= 0)
                                    continue;

                                modLog.Debug($@"Merging {nodes.Count} node(s) of type '{nodeType}' from file ""{modXmlFile}""", modXmlFile.Path);

                                foreach (XElement node in nodes)
                                {
                                    ct.ThrowIfCancellationRequested();

                                    // Strip XML comments out:
                                    node.DescendantNodesAndSelf().OfType<XComment>().Remove();

                                    // Read id and name:
                                    string key = nodeType.KeyAttribute != null ? node.Attribute(nodeType.KeyAttribute)?.Value : null;
                                    string src = $"{mod.UniqueName}, {modXmlFile.RelativePath}, line {node.Line()}";

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
                                            ct.ThrowIfCancellationRequested();

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
                                            else if (existingMod == mod.UniqueName)
                                                modLog.Warn($@"Replacing {prettyExistingNode} with {prettyNewNode}. The node was modified by the same mod => This could be an ERROR", modXmlFile.Path);
                                            else
                                                modLog.Warn($@"Replacing {prettyExistingNode} with {prettyNewNode} => This is a potential MOD INCOMPATIBILITY", modXmlFile.Path);
                                            existingNode.Remove();
                                        }
                                    }


                                    // MARK NODES <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                                    node.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                    if (targetXmlFileType == EXmlFileType.Animations)
                                    {
                                        // Mark animations assetPos nodes:
                                        foreach (XElement assetPos in node.DescendantsAndSelf("assetPos"))
                                        {
                                            assetPos.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                            assetPos.SetAttributeValue(NodeType.ATTRIBUTE_LIBRARY, src);
                                        }
                                    }
                                    else if (targetXmlFileType == EXmlFileType.Audio)
                                    {
                                        // Mark audio nodes:
                                        foreach (XElement a in node.DescendantsAndSelf("a"))
                                        {
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_LIBRARY, src);
                                        }
                                    }
                                    else if (targetXmlFileType == EXmlFileType.Textures)
                                    {
                                        // Mark sprite sheet nodes:
                                        foreach (XElement a in node.DescendantsAndSelf("t"))
                                        {
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_LIBRARY, src);
                                        }
                                        // Mark sprite nodes:
                                        foreach (XElement a in node.DescendantsAndSelf("re"))
                                        {
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                            a.SetAttributeValue(NodeType.ATTRIBUTE_LIBRARY, src);
                                        }
                                    }


                                    // Add to parent node:
                                    parentNode.Add(new XElement(node));
                                }
                            }
                        }
                    }


                    // =========================================================================================


                    // Get PATCH files:
                    if(!await mod.TryLoadPatchXmlFilesAsync(targetXmlFileType))
                        throw new StopException($@"[{mod}] Unable to load PATCH XML files of type '{targetXmlFileType}'", Paths.BuildPatchDir, CTS);

                    XmlFile[] patchFiles =
                        mod.PatchXmlFiles[targetXmlFileType].Values
                        .OrderBy(xmlFile => xmlFile.Path.AsStdPath().ToLowerInvariant())
                        .ToArray();

                    // PATCH
                    if (patchFiles.Length > 0)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Create mod patch dir:
                        if (!await IOUtils.TryCreateDirAsync(mod.BuildPatchDir, modLog, CT))
                            throw new StopException($@"[{mod}] Unable to create directory: ""{mod.BuildPatchDir}""", Paths.BuildPatchDir, CTS);

                        // Patch each file:
                        modLog.Debug($@"Performing {targetXmlFileType} XML patch operations...", mod.BuildPatchDir);
                        foreach (XmlFile modPatchXmlFile in patchFiles)
                        {
                            CT.ThrowIfCancellationRequested();

                            // Ignore?
                            if (modPatchXmlFile.IsIgnored)
                            {
                                modLog.Warn($@"Ignoring ""{modPatchXmlFile}"" as defined by '{XmlFile.ATTRIBUTE_IGNORE}' attribute in root node", modPatchXmlFile.Path);
                                continue;
                            }

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
                                    throw new StopException($@"[{mod}] Unable to create patch operation, in file ""{modPatchXmlFile.RelativePath}"" line {patchNode.Line()}", Paths.BuildStageDir, CTS);

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
                                    throw new StopException($@"[{mod}] Failed to execute the evaluated xpath='{patch.XPath}'. {Environment.NewLine}{patch}", modPatchXmlFile.Path, CTS);

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
                                    if (targetXmlFileType == EXmlFileType.Audio)
                                    {
                                        foreach (XElement targetNode in targetNodes.Where(n => n.Name == "a"))
                                        {
                                            string src = $"{mod.UniqueName}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                            string targetAttribute = patch.PatchNode.Element(XmlPatchOperation.ATTRIBUTE)?.Value;
                                            if (targetAttribute == "filename" || targetAttribute == "mp3" || targetAttribute == "ogg")
                                            {
                                                targetNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                                targetNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                            }
                                        }
                                    }

                                    // Mark animations <assetPos> nodes which have the attribute 'filename':
                                    else if (targetXmlFileType == EXmlFileType.Animations)
                                    {
                                        foreach (XElement targetNode in targetNodes.Where(n => n.Name == "assetPos"))
                                        {
                                            string src = $"{mod.UniqueName}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                            if (patch.PatchNode.Element(XmlPatchOperation.ATTRIBUTE)?.Value == "filename")
                                            {
                                                targetNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                                targetNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                            }
                                        }
                                    }

                                    // Mark texture nodes:
                                    else if (targetXmlFileType == EXmlFileType.Textures)
                                    {
                                        foreach (XElement targetNode in targetNodes.Where(n => n.Name == "t"))
                                        {
                                            string src = $"{mod.UniqueName}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                            if (patch.PatchNode.Element(XmlPatchOperation.ATTRIBUTE)?.Value == "i")
                                            {
                                                targetNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                                targetNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                            }
                                        }
                                        foreach (XElement targetNode in targetNodes.Where(n => n.Name == "re"))
                                        {
                                            string src = $"{mod.UniqueName}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                            if (patch.PatchNode.Element(XmlPatchOperation.ATTRIBUTE)?.Value == "n")
                                            {
                                                targetNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
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
                                    if (targetXmlFileType == EXmlFileType.Audio)
                                    {
                                        string src = $"{mod.UniqueName}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                        foreach (XElement valueNode in patch?.PatchNode?.Element(XmlPatchOperation.VALUE)?.Descendants("a") ?? [])
                                        {
                                            valueNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                            valueNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                        }
                                    }

                                    // Mark animations <assetPos> nodes which have the attribute 'filename':
                                    else if (targetXmlFileType == EXmlFileType.Animations)
                                    {
                                        string src = $"{mod.UniqueName}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                        foreach (XElement valueNode in patch?.PatchNode?.Element(XmlPatchOperation.VALUE)?.Descendants("assetPos").Where(n => n.Attribute("filename") != null) ?? [])
                                        {
                                            valueNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                            valueNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                        }
                                    }

                                    // Mark textures nodes:
                                    else if (targetXmlFileType == EXmlFileType.Textures)
                                    {
                                        string src = $"{mod.UniqueName}, {modPatchXmlFile.RelativePath}, line {patchNode.Line()}";
                                        foreach (XElement valueNode in patch?.PatchNode?.Element(XmlPatchOperation.VALUE)?.Descendants("t") ?? [])
                                        {
                                            valueNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                            valueNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                        }
                                        foreach (XElement valueNode in patch?.PatchNode?.Element(XmlPatchOperation.VALUE)?.Descendants("re") ?? [])
                                        {
                                            valueNode.SetAttributeValue(NodeType.ATTRIBUTE_OWNER, mod.UniqueName);
                                            valueNode.SetAttributeValue(NodeType.ATTRIBUTE_PATCH, src);
                                        }
                                    }
                                }

                                // Perform patch operation:
                                if (!patch.TryRun(targetNodes, modLog))
                                {
                                    modLog.Error($@"Patch operation has FAILED. {Environment.NewLine}{patch}", modPatchXmlFile.Path);
                                    Fail();
                                    return;
                                }

                                // Done with this patch operation.
                                modLog.Debug($@"Patch operation applied to {targetNodes.Count} target node(s). {Environment.NewLine}{patch}", modPatchXmlFile.Path);
                            }

                        }

                    } // END OF PATCH

                } // FOREACH MOD


                // Save merged/patched XML file:
                if (!await spaceHavenXmlFile.TrySaveAsync(Log, ct))
                    throw new StopException($@"Unable to save ""{spaceHavenXmlFile.Path}""", Paths.BuildStageLibraryDir, CTS);


                // Mark XML file as completed:
                bool composeTextures;
                lock (completed)
                {
                    completed.Add(targetXmlFileType);
                    composeTextures = completed.Contains(EXmlFileType.Animations) && completed.Contains(EXmlFileType.Textures);
                }


                // POST PROCESSING TASKS:
                switch (targetXmlFileType)
                {
                    case EXmlFileType.Haven:
                        break;

                    case EXmlFileType.Texts:
                        if (!await TryFixTextsAsync())
                            throw new StopException("Unable to fix texts file", Paths.BuildDir, CTS);
                        break;

                    case EXmlFileType.Audio:
                        if (!await TryComposeAudioAsync())
                            throw new StopException("Unable to compose audio", Paths.BuildDir, CTS);
                        break;

                    case EXmlFileType.Textures:
                    case EXmlFileType.Animations:
                        if(!composeTextures)
                            break;
                        if(!await TryComposeTexturesAsync())
                            throw new StopException("Unable to compose textures", Paths.BuildDir, CTS);
                        break;

                    case EXmlFileType.SpaceHavenSettings:
                        break;

                    default:
                        throw new NotImplementedException($"{nameof(EXmlFileType)} = {targetXmlFileType}");
                }

            }); // PARALLEL FOREACH XMLFILETYPE

            // Done.
            MergeXml?.Complete();
            PatchXml?.Complete();
            Log.Success("XML merge completed", Paths.BuildMergeDir);
            return true;
        }
        catch (Exception ex) when (ex.IsStop(out StopException error))
        {
            Log.Error(error.Message, error.Location);
            return false;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable to merge XML files: {ex}", Paths.BuildMergeDir);
            return false;
        }
    }


















    private async Task<bool> TryFixTextsAsync()
    {
        try
        {
            Log.Info($@"Fixing TEXT entries...", Paths.BuildAudioDir);
            FixTexts.Start();

            // Get texts document:
            XmlFile spaceHavenTextsXmlFile = Build.XmlFile[EXmlFileType.Texts];

            // Supported "languages
            string[] languages = Enum.GetNames<ELanguage>();
            const string emptyContent = " ";

            // Text entries, sorted descending by line number:
            // (since we will add nodes, the line number information shifts)
            try
            {
                List<(XElement t, int)> nodes = spaceHavenTextsXmlFile.Root.Descendants("t").Select(t => (t, t.Line())).OrderByDescending(tuple => tuple.Item2).ToList();
                int count = 0;
                foreach ((XElement t, int line) in nodes)
                {
                    ++count;
                    try
                    {
                        CT.ThrowIfCancellationRequested();

                        if (!int.TryParse(t.Attribute("id")?.Value ?? "-1", out int id) || id <= 0)
                        {
                            Log.Error($"Invalid text entry with missing or invalid attribute id='{t.Attribute("id")?.Value}' in texts file at {line}", Paths.BuildTextsXmlPath);
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
                                Log.Debug($"Fixing text entry <t> id={id} with missing translation to language '{language}'", Paths.BuildTextsXmlPath);
                                t.Add(languageNode = new XElement(language, textContent));
                            }
                            else if (languageNode.Value.IsNullOrEmpty())
                            {
                                Log.Debug($"Fixing text entry <t> id={id} with missing text content for translation to language '{language}'", Paths.BuildTextsXmlPath);
                                languageNode.Value = textContent;
                            }
                        }
                    }
                    finally
                    {
                        FixTexts.SetNormalized((count / 1000) / (nodes.Count * 0.001));
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                // Save texts document, for debugging:
                try { await spaceHavenTextsXmlFile.TrySaveToAsync(Paths.BuildTextsXmlPath, Log, CT); }
                catch { }
                throw;
            }

            // Update line numbers:
            if (!await spaceHavenTextsXmlFile.TryReparse(Log, CT))
                return false;

            // Done.
            FixTexts.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to fix TEXT entries: {ex}", Paths.BuildAudioDir);
            return false;
        }
    }












    private async Task<bool> TryComposeAudioAsync()
    {
        try
        {
            Log.Info($@"Composing AUDIO...", Paths.BuildAudioDir);
            ComposeAudio.Start();

            // Get and save animations document, for debugging:
            XmlFile spaceHavenAudioXmlFile = Build.XmlFile[EXmlFileType.Audio];
            if (!await spaceHavenAudioXmlFile.TrySaveToAsync(Paths.BuildAudioFilePath, Log, CT))
                return false;

            // Collect all assetPos filename references and save it to spriteReference objects:
            bool errors = false;
            OrderedDictionary<int, Audio> audioById = []; // keep original order!
            OrderedDictionary<string, Audio> audioByName = []; // keep original order!
            List<XElement> audioNodes = spaceHavenAudioXmlFile.Root.Descendants("a").ToList();
            foreach (XElement audioNode in audioNodes)
            {
                try
                {
                    CT.ThrowIfCancellationRequested();

                    // Only modded audio:
                    string owner = audioNode.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value;
                    if (owner.IsNullOrWhiteSpace())
                        continue;

                    // Get mod:
                    Mod mod = Build.Mods.FirstOrDefault(m => m.UniqueName == owner);
                    if (mod == null)
                    {
                        Log.Error($@"Unable to find owner mod for audio entry at line {audioNode.Line()}", Paths.BuildAudioFilePath);
                        return false;
                    }

                    // Parse audio:
                    Audio audio = new(Paths, mod, audioNode, mod.Log);
                    if (!audio.TryParse(Build.Mods))
                    {
                        errors = true;
                        continue;
                    }

                    // Audio entry uses original game audio:
                    if (audio.IsOriginalAudioFile)
                        continue;

                    // Check for duplicate audio ID:
                    if (audioById.TryGetValue(audio.Id, out Audio existingAudio1))
                    {
                        Log.Error($@"Duplicate audio ID: {Environment.NewLine}{existingAudio1} {Environment.NewLine}{audio}", Paths.BuildAudioFilePath);
                        return false;
                    }
                    else audioById[audio.Id] = audio;

                    // Check for duplicate audio NAME:
                    if (audioByName.TryGetValue(audio.Name, out Audio existingAudio2))
                    {
                        Log.Error($@"Duplicate audio NAME: {Environment.NewLine}{existingAudio2} {Environment.NewLine}{audio}", Paths.BuildAudioFilePath);
                        return false;
                    }
                    else audioByName[audio.Name] = audio;
                }
                finally
                {
                    ComposeAudio.IncrementNormalized(1.0 / audioNodes.Count);
                }
            }
            if (errors)
                return false;

            // Copy audio files:
            foreach (Audio audio in audioByName.Values)
            {
                string targetAbsolutePath = Paths.BuildStageDir.CombineAsOSPath(audio.TargetRelativePath);
                if (targetAbsolutePath.EscapesDir(Paths.BuildStageDir))
                {
                    Log.Error($@"Invalid target audio file path ""{targetAbsolutePath}"" for {audio}");
                    return false;
                }

                if (!await IOUtils.TryCopyFileAsync(audio.SourceAbsolutePath, targetAbsolutePath, true, Log, CT))
                    return false;
            }




            // Done.
            ComposeAudio?.Complete();
            Log.Success($"AUDIO files ready", Paths.BuildAudioDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose AUDIO: {ex}", Paths.BuildAudioDir);
            return false;
        }
    }











    private async Task<bool> TryComposeTexturesAsync()
    {
        try
        {
            int progress;

            Log.Info($@"Composing TEXTURES...", Paths.BuildTexturesDir);
            ComposeTextures.Start();

            // Get and save textures document, for debugging:
            XmlFile spaceHavenTexturesXmlFile = Build.XmlFile[EXmlFileType.Textures];
            if (!await spaceHavenTexturesXmlFile.TrySaveToAsync(Paths.BuildStageTexturesXmlPath, Log, CT))
                return false;

            // Get and save animations document, for debugging:
            XmlFile spaceHavenAnimationsXmlFile = Build.XmlFile[EXmlFileType.Animations];
            if (!await spaceHavenAnimationsXmlFile.TrySaveToAsync(Paths.BuildStageAnimationsXmlPath, Log, CT))
                return false;

            // Get last original game spritesheet ID:
            int lastOriginalSpriteSheetKey = Build.GetLastUsedKey(EKeyPool.SpriteSheet);

            // Get last original game sprite NAME:
            int lastOriginalSpriteName = Build.GetLastUsedKey(EKeyPool.Sprite);

            // Get last original game sprite NAME:
            int lastOriginalSpriteId = Build.LastOriginalSpriteId;
            int lastGlobalSpriteId = Build.LastOriginalSpriteId;







            // THIS LOADS PREDEFINED SPRITES AND SPRITESHEETS GIVEN BY MODIFICATIONS IN TEXTURES.XML:
            SpriteAtlas predefinedAtlas = new("PREDEFINED");
            try
            {
                List<XElement> modifiedSpriteSheetNodes =
                    spaceHavenTexturesXmlFile.Root
                    .Descendants("t")
                    .Where(t => !(t.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value.IsNullOrWhiteSpace() ?? true))
                    .ToList();

                List<XElement> modifiedSpriteNodes =
                    spaceHavenTexturesXmlFile.Root
                    .Descendants("re")
                    .Where(re => !(re.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value.IsNullOrWhiteSpace() ?? true))
                    .ToList();

                List<XElement> modifiedAssetPosWithARef =
                    spaceHavenAnimationsXmlFile.Root
                    .Descendants("assetPos")
                    .Where(assetPos => !(assetPos.Attribute(NodeType.ATTRIBUTE_OWNER)?.Value.IsNullOrWhiteSpace() ?? true) && int.TryParse(assetPos.Attribute("a")?.Value, out int localSpriteName) && localSpriteName > lastOriginalSpriteName)
                    .ToList();



                // Map local sprite name to assetPos references:
                SortedDictionary<string, List<XElement>> spriteLocalNameToAssetPos = [];
                foreach (XElement assetPos in modifiedAssetPosWithARef)
                {
                    string localSpriteNameStr = assetPos.Attribute("a").Value;
                    if (!spriteLocalNameToAssetPos.TryGetValue(localSpriteNameStr, out List<XElement> assetPosList))
                        spriteLocalNameToAssetPos[localSpriteNameStr] = assetPosList = [];
                    assetPosList.Add(assetPos);
                }

                CT.ThrowIfCancellationRequested();



                Log.Info("Compose Textures: Loading Predefined Sprite Sheets (0%) -> This may take some time...");
                ComposeTextures_LoadPredefinedSpriteSheets.Start();
                progress = 0;

                // Spritesheet global keys must be sequential:
                Dictionary<XElement, int> spriteSheetGlobalIds =
                    modifiedSpriteSheetNodes.ToDictionary(t => t, t => Build.AllocateNextNumericId(EKeyPool.SpriteSheet));

                // Load predefined spritesheets (those declared in mod textures.xml files):
                await Parallel.ForEachAsync(modifiedSpriteSheetNodes, ParallelOptions, async (t, ct) =>
                //await Parallel.ForEachAsync(modifiedSpriteSheetNodes, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (t, ct) =>
                {
                    // mod:
                    string modName = t.Attribute(NodeType.ATTRIBUTE_OWNER).Value;
                    Mod mod = Build.Mods.FirstOrDefault(mod => mod.UniqueName == modName);
                    if (mod == null)
                    {
                        Log.Error($@"Unknown mod for <t> node in file ""{spaceHavenTexturesXmlFile.FileName}"" line {t.Line()}", spaceHavenTexturesXmlFile.Path);
                        Fail();
                        return;
                    }

                    // key:
                    string keyStr = t.Attribute("i").Value;
                    if (!int.TryParse(keyStr, out int key)) key = -1;
                    if (key < 0)
                    {
                        Log.Error($@"<t> node contains invalid or undefined 'i' attribute in file ""{spaceHavenTexturesXmlFile.FileName}"" line {t.Line()}", spaceHavenTexturesXmlFile.Path);
                        Fail();
                        return;
                    }

                    // remap data:
                    int globalId;
                    lock (Build)
                        globalId = spriteSheetGlobalIds[t];

                    // spritesheet image:
                    string absoluteImagePath = IOUtils.CombineAsOSPath(mod.SpriteSheetsDir, key.ToString()).FindFile();

                    // instantiate:
                    if (!absoluteImagePath.IsNullOrWhiteSpace())
                    {
                        Log.Debug($@"Creating rendered spritesheet '{globalId}' (without a predefined image)", spaceHavenTexturesXmlFile.Path);
                        SpriteSheet spriteSheet = new(key, absoluteImagePath, predefinedAtlas) { GlobalId = globalId, };
                        lock (predefinedAtlas)
                            predefinedAtlas.Add(spriteSheet);
                    }
                    else
                    {
                        Log.Debug($@"Creating predefined spritesheet '{globalId}' (with a predefined image)", spaceHavenTexturesXmlFile.Path);
                        if (!int.TryParse(t.Attribute("w").Value, out int width))
                            width = -1;
                        if (!int.TryParse(t.Attribute("h").Value, out int height))
                            height = -1;
                        SpriteSheet spriteSheet = new(key, width, height, 2 * modifiedSpriteNodes.Count + 1, 0, predefinedAtlas) { GlobalId = globalId, };
                        lock (predefinedAtlas)
                            predefinedAtlas.Add(spriteSheet);
                    }

                    // remap:
                    t.SetAttributeValue("i", globalId);

                    // progress:
                    lock (ComposeTextures_LoadPredefinedSpriteSheets)
                    {
                        ComposeTextures_LoadPredefinedSpriteSheets.IncrementNormalized(1.0 / modifiedSpriteSheetNodes.Count);
                        int newProgress = 10 * (int)(ComposeTextures_LoadPredefinedSpriteSheets.NormalizedValue * 10.0);
                        if (newProgress > progress)
                            Log.Info($@"Compose Textures: Loading Predefined Sprite Sheets ({progress = newProgress}%)");
                    }
                });
                ComposeTextures_LoadPredefinedSpriteSheets.Complete();
                if (progress != 100)
                    Log.Info($@"Compose Textures: Loading Predefined Sprite Sheets (100%)");

                CT.ThrowIfCancellationRequested();




                Log.Info("Compose Textures: Loading Predefined Sprites (0%) -> This may take some time...");
                ComposeTextures_LoadPredefinedSprites.Start();
                progress = 0;

                // Sprite global names must be sequential:
                Dictionary<XElement, int> spriteSheetGlobalNames =
                    modifiedSpriteNodes.ToDictionary(re => re, re => Build.AllocateNextNumericId(EKeyPool.Sprite));

                // Load predefined sprites
                int lastLocalSpriteId = 0;
                await Parallel.ForEachAsync(modifiedSpriteNodes, ParallelOptions, async (re, ct) =>
                //await Parallel.ForEachAsync(modifiedSpriteNodes, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (re, ct) =>
                {
                    // mod:
                    string modName = re.Attribute(NodeType.ATTRIBUTE_OWNER).Value;
                    Mod mod = Build.Mods.FirstOrDefault(mod => mod.UniqueName == modName);
                    if (mod == null)
                    {
                        Log.Error($@"Unknown mod for <re> node in file ""{spaceHavenTexturesXmlFile.FileName}"" line {re.Line()}", spaceHavenTexturesXmlFile.Path);
                        Fail();
                        return;
                    }

                    // spritesheet:
                    _ = int.TryParse(re.Attribute("t")?.Value, out int spriteSheetLocalID);
                    SpriteSheet spriteSheet = predefinedAtlas.SpriteSheets.FirstOrDefault(ss => ss.LocalId == spriteSheetLocalID);
                    if (spriteSheet == null)
                    {
                        Log.Error($@"<re> node maps to unknown <t> node in file ""{spaceHavenTexturesXmlFile.FileName}"" line {re.Line()}", spaceHavenTexturesXmlFile.Path);
                        Fail();
                        return;
                    }

                    // sprite name:
                    string localNameStr = re.Attribute("n")?.Value;
                    if (localNameStr.IsNullOrWhiteSpace())
                    {
                        Log.Error($@"<re> has missing or invalid 'n' attribute in file ""{spaceHavenTexturesXmlFile.FileName}"" line {re.Line()}", spaceHavenTexturesXmlFile.Path);
                        Fail();
                        return;
                    }

                    // read sprite location within its spritesheet:
                    _ = int.TryParse(re.Attribute("w")?.Value, out int width);
                    _ = int.TryParse(re.Attribute("h")?.Value, out int height);
                    _ = int.TryParse(re.Attribute("x")?.Value, out int x);
                    _ = int.TryParse(re.Attribute("y")?.Value, out int y);

                    // remap data:
                    Sprite sprite;
                    int globalName;
                    int spriteLocalId = Interlocked.Increment(ref lastLocalSpriteId);
                    int globalId = Interlocked.Increment(ref lastGlobalSpriteId);
                    lock (Build)
                        globalName = spriteSheetGlobalNames[re];

                    // create sprite:
                    if (spriteSheet.IsPredefined)
                    {
                        Log.Debug($@"Creating sprite with n=""{localNameStr}""=>""{globalName}"" as a predefined region of spritesheet i=""{spriteSheet.GlobalId}""", spaceHavenTexturesXmlFile.Path);

                        // create sprite from spritesheet region:
                        sprite = new(localNameStr, spriteLocalId, spriteSheet, width, height, x, y)
                        {
                            GlobalName = globalName.ToString(),
                            GlobalId = globalId,
                        };
                    }
                    else
                    {
                        // sprite image:
                        _ = localNameStr.TryParse(out int localName);
                        string spriteAbsolutePath =
                            mod.SpritePaths.Count <= 0 ? null :
                            (
                                IOUtils.CombineAsOSPath(mod.SpritesDir, $"{localNameStr}.png").FindFile() ??
                                IOUtils.CombineAsOSPath(mod.SpritesDir, localNameStr).FindFile() ??
                                IOUtils.CombineAsOSPath(mod.SpritesDir, $"{localName}.png").FindFile() ??
                                IOUtils.CombineAsOSPath(mod.SpritesDir, localName.ToString()).FindFile()
                            );
                        if (spriteAbsolutePath.IsNullOrWhiteSpace())
                        {
                            Log.Error($@"<re> node with n=""{localNameStr}""=>""{globalName}"" does not have any corresponding sprite image ""{localNameStr}"", in file ""{spaceHavenTexturesXmlFile.FileName}"" line {re.Line()}", spaceHavenTexturesXmlFile.Path);
                            Fail();
                            return;
                        }

                        Log.Debug($@"Creating sprite with n=""{localNameStr}""=>""{globalName}"" from image file ""{spriteAbsolutePath}""", spaceHavenTexturesXmlFile.Path);

                        // create sprite from image:
                        sprite = new(localNameStr, spriteLocalId, spriteAbsolutePath)
                        {
                            GlobalName = globalName.ToString(),
                            GlobalId = globalId,
                            X = x,
                            Y = y,
                        };

                        // fix sprite width and height:
                        if (width != sprite.Width || height != sprite.Height)
                        {
                            Log.Warn($@"Fixing incoherent width/height provided by <re> node n=""{localNameStr}""=>""{globalName}"" in file ""{spaceHavenTexturesXmlFile.FileName}"" line {re.Line()}", spaceHavenTexturesXmlFile.Path);
                            re.SetAttributeValue("w", sprite.Width);
                            re.SetAttributeValue("h", sprite.Height);
                        }
                    }

                    // assign to spritesheet:
                    lock (spriteSheet)
                        spriteSheet.Add(sprite);

                    // remap:
                    re.SetAttributeValue("n", globalName);
                    re.SetAttributeValue("id", globalId);
                    re.SetAttributeValue("t", spriteSheet.GlobalId);

                    // progress:
                    lock (ComposeTextures_LoadPredefinedSprites)
                    {
                        ComposeTextures_LoadPredefinedSprites.IncrementNormalized(1.0 / modifiedSpriteNodes.Count);
                        int newProgress = 10 * (int)(ComposeTextures_LoadPredefinedSprites.NormalizedValue * 10.0);
                        if (newProgress > progress)
                            Log.Info($@"Compose Textures: Loading Predefined Sprites ({progress = newProgress}%)");
                    }
                });
                ComposeTextures_LoadPredefinedSprites.Complete();
                if (progress != 100)
                    Log.Info($@"Compose Textures: Loading Predefined Sprites (100%)");



                CT.ThrowIfCancellationRequested();




                // Remap <assetPos a="..."> in animations.xml:
                List<Sprite> allSprites = predefinedAtlas.Sprites;
                foreach ((string spriteLocalName, List<XElement> assetPosList) in spriteLocalNameToAssetPos.ToTuples())
                {
                    CT.ThrowIfCancellationRequested();

                    string globalSpriteName = allSprites.FirstOrDefault(s => s.LocalName.Equals(spriteLocalName, StringComparison.OrdinalIgnoreCase))?.GlobalName;
                    if (globalSpriteName == null)
                    {
                        foreach (XElement assetPos in assetPosList)
                            Log.Error($@"<assetPos> has invalid reference a=""{spriteLocalName}"", in file ""{spaceHavenAnimationsXmlFile.FileName}"" line {assetPos.Line()}", spaceHavenAnimationsXmlFile.Path);
                        return false;
                    }
                    foreach (XElement assetPos in assetPosList)
                        assetPos.SetAttributeValue("a", globalSpriteName);
                }

                CT.ThrowIfCancellationRequested();




                Log.Info("Compose Textures: Writing Predefined Sprite Sheets (0%) -> This may take some time...");
                ComposeTextures_WritePredefinedSpriteSheets.Start();
                progress = 0;

                // Render spritesheets
                await Parallel.ForEachAsync(predefinedAtlas.SpriteSheets, ParallelOptions, async (spriteSheet, ct) =>
                //await Parallel.ForEachAsync(predefinedAtlas.SpriteSheets, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (spriteSheet, ct) =>
                {
                    if (!spriteSheet.IsPredefined && !spriteSheet.IsRendered)
                    {
                        spriteSheet.TryRenderFromSprites(Log, ct);

                        // Save rendered spritesheets as PNG file, for debugging:
                        //if (!await spriteSheet.TryExportToPngAsync(IOUtils.CombineAsOSPath(Paths.BuildTexturesDir, $"{spriteSheet.GlobalId}.png"), Log, ct))
                        //    Fail();
                    }

                    // Save as CIM file to build stage directory:
                    if (!await spriteSheet.TryExportToCimAsync(IOUtils.CombineAsOSPath(Paths.BuildStageLibraryDir, $"{spriteSheet.GlobalId}.cim"), Log, ct))
                        Fail();

                    // progress:
                    lock (ComposeTextures_WritePredefinedSpriteSheets)
                    {
                        ComposeTextures_WritePredefinedSpriteSheets.IncrementNormalized(1.0 / predefinedAtlas.SpriteSheets.Count);
                        int newProgress = 10 * (int)(ComposeTextures_WritePredefinedSpriteSheets.NormalizedValue * 10.0);
                        if (newProgress > progress)
                            Log.Info($@"Compose Textures: Writing Predefined Sprite Sheets ({progress = newProgress}%)");
                    }
                });
                ComposeTextures_WritePredefinedSpriteSheets.Complete();
                if (progress != 100)
                    Log.Info($@"Compose Textures: Writing Predefined Sprite Sheets (100%)");


            }
            finally
            {
                predefinedAtlas?.Dispose();
            }

            CT.ThrowIfCancellationRequested();































            // THIS LOADS SPRITES REFERENCED BY THEIR RELATIVE PATH IN ANIMATIONS.XML:

            // Collect all assetPos filename references and save it to spriteReference objects:
            Log.Info("Compose Textures: Reading Referenced Sprites...");
            ComposeTextures_ReadReferencedSprites.Start();
            bool errors = false;
            int localSpriteId = 0;
            SortedDictionary<string, SpriteReference> spriteReferences = [];
            List<XElement> allAssetPosNodes = spaceHavenAnimationsXmlFile.Root.Descendants("assetPos").ToList();
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
                    Mod mod = Build.Mods.FirstOrDefault(mod => mod.UniqueName == modName);
                    if (mod == null)
                    {
                        Log.Error($@"Unable to retrieve mod owning <assetPos> node with 'filename' reference ""{assetPosFilenameReference}"", in animations file line {assetPos.Line()}", Paths.BuildStageAnimationsXmlPath);
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
                    string spriteRefKey = SpriteReference.GetKey(mod.UniqueName, assetPosFilenameReference, filter);

                    // Get or create sprite reference:
                    if (!spriteReferences.TryGetValue(spriteRefKey, out SpriteReference spriteRef))
                    {
                        spriteRef = new(spriteRefKey, ++localSpriteId, mod, assetPosFilenameReference, filter);

                        // Add current assetPos reference:
                        spriteRef.AssetPosNodes.Add(assetPos);

                        // Check for exiting sprite image:
                        spriteRef.AbsolutePath =
                            IOUtils.CombineAsOSPath(mod.SpritesDir, $"{spriteRef.RelativePathWithoutExtension}.png").FindFile() ??
                            IOUtils.CombineAsOSPath(mod.SpritesDir, spriteRef.RelativePathWithoutExtension).FindFile() ??
                            IOUtils.CombineAsOSPath(mod.SpritesDir, $"{spriteRef.FilenameWithoutExtension}.png").FindFile() ??
                            IOUtils.CombineAsOSPath(mod.SpritesDir, spriteRef.FilenameWithoutExtension).FindFile();

                        if (spriteRef.AbsolutePath == null)
                        {
                            Log.Error($@"Unable to locate sprite image file for {pretty}", Paths.BuildStageAnimationsXmlPath);
                            errors = true;
                            continue;
                        }

                        // Add the created sprite reference:
                        spriteReferences.Add(spriteRefKey, spriteRef);
                    }
                    else
                    {
                        // Add current assetPos reference:
                        spriteRef.AssetPosNodes.Add(assetPos);
                    }
                }
                finally
                {
                    ComposeTextures_ReadReferencedSprites.IncrementNormalized(1.0 / allAssetPosNodes.Count);
                }
            }
            ComposeTextures_ReadReferencedSprites.Complete();
            if (errors)
                return false;

            CT.ThrowIfCancellationRequested();






            // Create sprite atlases by texture filter type, and pack sprites into sprite sheets:
            ComposeTextures_LoadReferencedSprites.Start();
            ComposeTextures_PackReferencedSprites.Start();
            ComposeTextures_WriteReferencedSpriteSheets.Start();
            ComposeTextures_ComposeTexturesXml.Start();
            foreach (ETextureFilter filter in Enum.GetValues<ETextureFilter>().OrderByDescending(e => (int)e))
            {

                // Load sprite images:
                List<SpriteReference> spriteRefs = spriteReferences.Values.Where(s => s.Filter == filter).ToList();
                if (spriteRefs.Count <= 0)
                {
                    Log.Info($"No mod sprite references were found requiring the texture filter '{filter}'", Paths.BuildTexturesDir);
                    continue;
                }

                Log.Info($"Compose Textures: Loading Referenced Sprites ({filter.ToString()})...");

                CT.ThrowIfCancellationRequested();

                // Load referenced sprites:
                SortedDictionary<string, Sprite> sprites = [];
                await Parallel.ForEachAsync(spriteRefs, ParallelOptions, async (spriteRef, ct) =>
                //await Parallel.ForEachAsync(spriteRefs, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (spriteRef, ct) =>
                {
                    try
                    {
                        Log.Debug($"Loading sprite {spriteRef.Key}...", Paths.BuildTexturesDir);

                        Sprite sprite = new(spriteRef.Key, spriteRef.LocalID, spriteRef.AbsolutePath);

                        lock (sprites)
                        {
                            sprites[spriteRef.Key] = sprite;
                            spriteRef.Sprite = sprite;
                        }
                    }
                    finally
                    {
                        lock (ComposeTextures_LoadReferencedSprites)
                            ComposeTextures_LoadReferencedSprites.IncrementNormalized(1.0 / spriteReferences.Count);
                    }
                });

                CT.ThrowIfCancellationRequested();






                // Pack referenced sprites:
                Log.Info($"Compose Textures: Packing Referenced Sprites ({filter.ToString()})...");
                using SpriteAtlas spriteAtlas = new(filter.ToString().ToUpperInvariant())
                {
                    SpriteSheetSize = filter == ETextureFilter.Linear ? 4096 : 2048,
                    SpriteSpacing = filter == ETextureFilter.Linear ? 0 : 4,
                };

                if (!await Task.Run(() => spriteAtlas.Add(sprites.Values, Log, CT)))
                {
                    Log.Error($@"Unable to pack sprites into sprite atlas '{spriteAtlas.Name}'", Paths.BuildStageAnimationsXmlPath);
                    return false;
                }

                ComposeTextures_PackReferencedSprites.IncrementNormalized(spriteRefs.Count / (double)spriteReferences.Count);







                // Sequentially assign spritesheet KEY and sprite NAME/ID: 
                foreach (SpriteSheet spriteSheet in spriteAtlas.SpriteSheets)
                {
                    spriteSheet.GlobalId = Build.AllocateNextNumericId(EKeyPool.SpriteSheet);
                    foreach (Sprite sprite in spriteSheet.Sprites)
                    {
                        sprite.GlobalId = ++lastGlobalSpriteId;
                        sprite.GlobalName = Build.AllocateNextNumericId(EKeyPool.Sprite).ToString();
                    }
                }

                CT.ThrowIfCancellationRequested();





                // Write referenced sprite sheets:
                Log.Info("Writing Referenced Sprite Sheets...");

                // Draw sprites to the sprite sheets, save sprite sheets as CIM and PNG:
                await Parallel.ForEachAsync(spriteAtlas.SpriteSheets, ParallelOptions, async (spriteSheet, ct) =>
                {
                    try
                    {
                        Log.Debug($"Generating sprite sheet {spriteSheet.GlobalId}...", Paths.BuildTexturesDir);

                        if (!spriteSheet.TryRenderFromSprites(Log, ct))
                        {
                            Log.Error($@"Unable to generate sprite sheet '{spriteSheet.LocalId}'");
                            Fail();
                        }

                        string cimFilename = $"{spriteSheet.GlobalId}.cim";

                        // Export to CIM to build stage directory:
                        if (!await spriteSheet.TryExportToCimAsync(IOUtils.CombineAsOSPath(Paths.BuildStageLibraryDir, $"{spriteSheet.GlobalId}.cim"), Log, ct))
                            Fail();

                        // Export to PNG, for debugging:
                        //if (!await spriteSheet.TryExportToPngAsync(IOUtils.CombineAsOSPath(Paths.BuildTexturesDir, $"{spriteSheet.GlobalId}.png"), Log, ct))
                        //    Fail();
                    }
                    finally
                    {
                        lock (ComposeTextures_WriteReferencedSpriteSheets)
                            ComposeTextures_WriteReferencedSpriteSheets.IncrementNormalized((1.0 / spriteAtlas.SpriteSheets.Count) * (spriteRefs.Count / (double)spriteReferences.Count));
                    }
                });
                CT.ThrowIfCancellationRequested();








                // For each sprite sheet, add a CIM texture entry to the textures XML file:
                Log.Info($@"Composing textures.xml...", Paths.BuildTexturesDir);
                XElement parentTexturesCimNode = spaceHavenTexturesXmlFile.GetParentNode(NodeType.TexturesCim);
                foreach (SpriteSheet spriteSheet in spriteAtlas.SpriteSheets.OrderBy(s => s.GlobalId))
                {
                    XElement t = new("t");
                    t.SetAttributeValue("i", spriteSheet.GlobalId);
                    t.SetAttributeValue("w", spriteSheet.Width);
                    t.SetAttributeValue("h", spriteSheet.Height);
                    t.SetAttributeValue("f", 1);
                    t.SetAttributeValue("min", (int)filter);
                    t.SetAttributeValue("max", (int)filter);
                    parentTexturesCimNode.Add(t);

                    ComposeTextures_ComposeTexturesXml.IncrementNormalized(0.1 * (1.0 / spriteAtlas.SpriteSheets.Count) * (spriteRefs.Count / (double)spriteReferences.Count));
                }

                // For each sprite, add a texture region to the textures XML file:
                XElement parentTexturesRegionNode = spaceHavenTexturesXmlFile.GetParentNode(NodeType.TexturesRegion);
                foreach (Sprite sprite in sprites.Values.OrderBy(sprite => sprite.GlobalName))
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

                    ComposeTextures_ComposeTexturesXml.IncrementNormalized(0.1 * (1.0 / sprites.Count) * (spriteRefs.Count / (double)spriteReferences.Count));
                }

                // For each assetPos of a sprite reference, set the a="..." attribute with the sprite name:
                foreach (SpriteReference spriteRef in spriteRefs)
                {
                    foreach (XElement assetPos in spriteRef.AssetPosNodes)
                        assetPos.SetAttributeValue("a", spriteRef.Sprite.GlobalName);

                    ComposeTextures_ComposeTexturesXml.IncrementNormalized(0.1 * (1.0 / spriteReferences.Count));
                }

                // Save current state of textures XML file:
                if (!await spaceHavenTexturesXmlFile.TrySaveToAsync(Paths.BuildStageTexturesXmlPath, Log, CT))
                    return false;
                ComposeTextures_ComposeTexturesXml.IncrementNormalized(0.3 * (1.0 / spriteReferences.Count));

                // Save current state of animations XML file:
                if (!await spaceHavenAnimationsXmlFile.TrySaveToAsync(Paths.BuildStageAnimationsXmlPath, Log, CT))
                    return false;
                ComposeTextures_ComposeTexturesXml.IncrementNormalized(0.4 * (1.0 / spriteReferences.Count));
            }
            ComposeTextures_LoadReferencedSprites.Complete();
            ComposeTextures_PackReferencedSprites.Complete();
            ComposeTextures_WriteReferencedSpriteSheets.Complete();
            ComposeTextures_ComposeTexturesXml.Complete();



            // Done.
            ComposeTextures.Complete();
            Log.Success($"TEXTURES ready", Paths.BuildTexturesDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose TEXTURES: {ex}", Paths.BuildTexturesDir);
            return false;
        }
    }








    private async Task<bool> TryWriteCreditsAsync()
    {
        try
        {
            Log.Debug($"Adding mod authors to '{SpaceHavenConstants.EXTRA_CREDITS_TXT}' file...", Paths.CacheDir);
            ComposeCredits.Start();

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

            // Space Haven Launcher devs, maintainers, supporters:
            sb.AppendLine("[Topic]Space Haven Launcher");
            sb.AppendLine("Ghostkeeper666");
            sb.AppendLine("KaiserManny");
            sb.AppendLine();

#warning TODO: Add supporters to credits!

            // Add original content now:
            sb.AppendLine(await IOUtils.TryReadAllTextAsync(Paths.TemplateExtraCreditsTxtPath, Log, CT) ?? string.Empty);

            // Save File:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.BuildStageExtraCreditsTxtPath, sb.ToString(), Log, CT))
                return false;

            // Done.
            ComposeCredits.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose final '{SpaceHavenConstants.SPACEHAVEN_JAR}' file: {ex}", Paths.CacheDir);
            return false;
        }
    }









    private async Task<bool> TryWriteSpaceHavenJarAsync()
    {
        try
        {
            Log.Info($"Composing '{SpaceHavenConstants.SPACEHAVEN_JAR}' file...", Paths.CacheDir);
            ComposeSpaceHavenJar.Start();

            // Select files to add to template JAR:
            DirectoryInfo di = new(Paths.BuildStageDir);
            FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
            JarAppender jar = new();
            if (!await jar.AppendTo(Paths.TemplateJarPath, Paths.CacheJarPath, Paths.BuildStageDir, files, Log, ParallelOptions))
                return false;

            ComposeSpaceHavenJar.SetNormalized(0.85);

            // Copy original JAR hash file:
            if (!await IOUtils.TryCopyFileAsync(Paths.TemplateJarHashPath, Paths.CacheJarHashPath, true, Log, CT))
                return false;

            ComposeSpaceHavenJar.SetNormalized(0.90);

            // Write the required class paths to jars.txt so the Bootstrap class can load them:
            List<string> classPaths = [];
            classPaths.AddRange(
                Build.Mods
                .Where(m => m.IsJavaMod)
                .SelectMany(m => m.JarFilePaths)
                .Where(path => path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) && IOUtils.FileExists(path))
                .Select(path => path.AsStdPath())
                .ToList());

            string jarsTxtPath = IOUtils.CombineAsOSPath(Paths.CacheDir, "jars.txt");
            if (!await IOUtils.TryWriteAllTextAsync(jarsTxtPath, classPaths.JoinToString("\r\n"), Log, CT))
                return false;

            ComposeSpaceHavenJar.SetNormalized(0.95);

            // Deploy aop / java agent libs to cache dir:
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, ModdingConstants.ASPECTJ), IOUtils.CombineAsOSPath(Paths.CacheDir, ModdingConstants.ASPECTJ), true, Log, CT))
                return false;
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, ModdingConstants.ASPECTJWEAVER), IOUtils.CombineAsOSPath(Paths.CacheDir, ModdingConstants.ASPECTJWEAVER), true, Log, CT))
                return false;
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, "LauncherAgent.jar"), IOUtils.CombineAsOSPath(Paths.CacheDir, "LauncherAgent.jar"), true, Log, CT))
                return false;

            // Done.
            ComposeSpaceHavenJar?.Complete();
            Log.Success($"'{SpaceHavenConstants.SPACEHAVEN_JAR}' is ready", Paths.CacheDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose final '{SpaceHavenConstants.SPACEHAVEN_JAR}' file: {ex}", Paths.CacheDir);
            return false;
        }
    }











    private async Task<bool> TryComposeModsJsonAsync()
    {
        try
        {
            ComposeModsJson.Start();

            ModsJsonFile m = new();

            // Absolute Base Paths:
            m.BasePaths.OriginalGameJarDir = Paths.SpaceHavenJarDir.AsStdPath();
            m.BasePaths.ClassicModsDir = Paths.ClassicModsDir.AsStdPath();
            m.BasePaths.SteamModsDir = Paths.SteamModsDir.AsStdPath();

            // Game environment:
            m.GameEnvironment.SpaceHavenVersion = BuildSettings.SpaceHavenVersion.ToString();
            m.GameEnvironment.SpaceHavenLauncherVersion = BuildSettings.AppVersion.ToString();
            m.GameEnvironment.Aspectj = ModdingConstants.ASPECTJ;
            m.GameEnvironment.AspectjWeaver = ModdingConstants.ASPECTJWEAVER;
            m.GameEnvironment.GamePlatform = BuildSettings.GamePlatform;

            // Mods:
            foreach (Mod mod in Build.Mods)
            {
                ModInfo modInfo = new();
                m.Mods.Add(modInfo);

                modInfo.UniqueName = mod.UniqueName;
                modInfo.Version = mod.Version.ToString();
                modInfo.ID = mod.ID;

                // Relative Paths:
                string modDir = mod.Dir.AsStdPath();

                modInfo.Dir = modDir;
                if (!m.BasePaths.ClassicModsDir.IsNullOrWhiteSpace())
                    modInfo.Dir = modInfo.Dir.Replace(m.BasePaths.ClassicModsDir, "[ClassicMods]", StringComparison.OrdinalIgnoreCase);
                if (!m.BasePaths.SteamModsDir.IsNullOrWhiteSpace())
                    modInfo.Dir = modInfo.Dir.Replace(m.BasePaths.SteamModsDir, "[SteamMods]", StringComparison.OrdinalIgnoreCase);

                modDir += '/';
                modInfo.InfoXml = mod.InfoXmlPath.AsStdPath().Replace(modDir, string.Empty, StringComparison.OrdinalIgnoreCase);
                modInfo.SpriteTextures.AddRange(mod.SpritePaths.Select(path => path.AsStdPath().Replace(modDir, string.Empty, StringComparison.OrdinalIgnoreCase)));
                modInfo.SpriteSheetTextures.AddRange(mod.SpriteSheetPaths.Select(path => path.AsStdPath().Replace(modDir, string.Empty, StringComparison.OrdinalIgnoreCase)));
                modInfo.AudioFiles.AddRange(mod.AudioPaths.Select(path => path.AsStdPath().Replace(modDir, string.Empty, StringComparison.OrdinalIgnoreCase)));
                modInfo.JarFiles.AddRange(mod.JarFilePaths.Select(path => path.AsStdPath().Replace(modDir, string.Empty, StringComparison.OrdinalIgnoreCase)));
                modInfo.OtherFiles.AddRange(mod.OtherFilesPaths.Select(path => path.AsStdPath().Replace(modDir, string.Empty, StringComparison.OrdinalIgnoreCase)));

                // Variables:
                foreach (Var var in mod.Variables.Values)
                {
                    VarInfo varInfo = new();
                    varInfo.Name = var.Name;
                    varInfo.Type = var.Type;
                    varInfo.Value = var.StrValue;
                    modInfo.Vars.Add(varInfo);
                }
            }

            string content = m.ToJsonString();

            ComposeModsJson.SetNormalized(0.50);

            // Write to cache directory:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.CacheModsJsonPath, content, Log, CT))
                return false;

            //Done.
            ComposeModsJson.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log?.Error($"Unable to create {ModdingConstants.MODS_JSON}: {ex}", Paths.CacheDir);
            return false;
        }
    }









    public async Task<bool> TryWriteVersionInfoAsync(CancellationToken ct)
    {
        try
        {
            WriteVersionInfo.Start();

            string[] lines = [BuildSettings.SpaceHavenVersion.ToString(), "(modified)"];

            // Haven.xml:
            Build.XmlFile[EXmlFileType.Haven].Xml.Root.SetAttributeValue("libVersion", lines.JoinToString(" "));
            WriteVersionInfo.SetNormalized(0.50);

            // Version.txt:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.BuildStageVersionPath, lines.JoinToString("\n"), Log, ct))
                return false;
            WriteVersionInfo.Complete();

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
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
        try
        {
            try { await Build.DisposeAsync(); } catch { }
            Build = null;

            Initialization?.RemoveAll(); // owned by caller, do not dispose!
            JavaBuild?.RemoveAll(); // owned by caller, do not dispose!
            XmlBuild?.RemoveAll(); // owned by caller, do not dispose!

            ResetBuildStage?.Dispose();
            LoadSpaceHavenXml?.Dispose();
            ResetXmlBuild?.Dispose();
            MergeXml?.Dispose();
            PatchXml?.Dispose();
            ComposeAudio?.Dispose();
            FixTexts?.Dispose();
            DeployXmlHash?.Dispose();
            DeployJavaHash?.Dispose();
            WriteVersionInfo?.Dispose();
            ComposeCredits?.Dispose();
            WriteSpaceHavenXml?.Dispose();
            ComposeSpaceHavenJar?.Dispose();
            ComposeModsJson?.Dispose();

            ComposeTextures?.RemoveAll();
            ComposeTextures?.Dispose();
            ComposeTextures_LoadPredefinedSpriteSheets?.Dispose();
            ComposeTextures_LoadPredefinedSprites?.Dispose();
            ComposeTextures_WritePredefinedSpriteSheets?.Dispose();
            ComposeTextures_ReadReferencedSprites?.Dispose();
            ComposeTextures_LoadReferencedSprites?.Dispose();
            ComposeTextures_PackReferencedSprites?.Dispose();
            ComposeTextures_WriteReferencedSpriteSheets?.Dispose();
            ComposeTextures_ComposeTexturesXml?.Dispose();

            SKGraphics.PurgeResourceCache();
            SKGraphics.PurgeFontCache();
            SKGraphics.PurgeAllCaches();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }
        catch { }
    }
    #endregion
}

