using SH.Content;
using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Buffers;
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
    private BuildPathData Paths => BuildSettings.Paths;

    private readonly LoggerCollection Log;
    private FileLogger FileLogger;

    private BuildData Build;
    private ParallelOptions ParallelOptions => BuildSettings.ParallelOptions;
    private CancellationToken CT => BuildSettings.CT;

    public bool IsNewJar { get; private set; }
    public bool NeedsXmlBuild { get; private set; }
    public bool NeedsJavaBuild { get; private set; }


    private IProgressInfo Initialization => BuildSettings.InitializationProgress;
    private IProgressInfo XmlBuild => BuildSettings.XmlBuildProgress;
    private IProgressInfo JavaBuild => BuildSettings.JavaBuildProgress;

    private IProgressInfo ResetBuildStage;
    private IProgressInfo LoadSpaceHavenXml;
    private IProgressInfo ResetXmlBuild;
    private IProgressInfo LoadModsXml;
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
    private IProgressInfo DeployConfigJson;
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
        BuildSettings.Paths = new(
            settings.AppDir,
            settings.WorkDir,
            settings.SpaceHavenDir,
            settings.SpaceHavenJarDir
        );
        Log = new LoggerCollection(log);
    }






    private void Fail() =>
        BuildSettings.Fail();



    private void ResetBuildStage_ProgressChanged(object sender, ProgressEventArgs e)
    {
        try
        {
            if (e.Progress.HasCompleted)
                Log.Success($"{e?.Progress?.Name} completed within {e?.Progress?.Clock?.ElapsedMilliseconds ?? -1} ms");
        }
        catch (Exception ex)
        { 
            Debug.WriteLine(ex.ToString());
        }
    }




    private async Task<bool> TryInitializeProgressAsync()
    {
        // TODO: auto-estimation of progess weights!
        try
        {
            // STARTUP:
            ResetBuildStage = new ProgressInfo("Reset Build Stage") { Max = 10 };
            ResetBuildStage.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(ResetBuildStage, 175);
            JavaBuild.AddChild(ResetBuildStage, 175);

            LoadSpaceHavenXml = new ProgressInfo("Load Space Haven XML") { Max = 10 };
            LoadSpaceHavenXml.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(LoadSpaceHavenXml, 1000);
            JavaBuild.AddChild(LoadSpaceHavenXml, 1000);



            // JAVA BUILD:
            DeployJavaHash = new ProgressInfo("Deploy JAVA Hash") { Max = 10 };
            DeployJavaHash.ProgressChanged += ResetBuildStage_ProgressChanged;
            JavaBuild.AddChild(DeployJavaHash, 1);



            // XML BUILD:
            ResetXmlBuild = new ProgressInfo("Reset XML Build") { Max = 10 };
            ResetXmlBuild.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(ResetXmlBuild, 1);

            LoadModsXml = new ProgressInfo("Load Mods XML") { Max = 10 };
            LoadModsXml.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(LoadModsXml, 1);

            MergeXml = new ProgressInfo("Merge XML") { Max = 10 };
            MergeXml.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(MergeXml, 20);

            PatchXml = new ProgressInfo("Patch XML") { Max = 10 };
            PatchXml.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(PatchXml, 25);

            ComposeAudio = new ProgressInfo("Compose Audio") { Max = 10 };
            ComposeAudio.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(ComposeAudio, 1);

            ComposeTextures_LoadPredefinedSpriteSheets = new ProgressInfo("Compose Textures: Load Predefined Sprite Sheets");
            ComposeTextures_LoadPredefinedSpriteSheets.ProgressChanged += ResetBuildStage_ProgressChanged;

            ComposeTextures_LoadPredefinedSprites = new ProgressInfo("Compose Textures: Load Predefined Sprites");
            ComposeTextures_LoadPredefinedSprites.ProgressChanged += ResetBuildStage_ProgressChanged;

            ComposeTextures_WritePredefinedSpriteSheets = new ProgressInfo("Compose Textures: Write Predefined Sprite Sheets");
            ComposeTextures_WritePredefinedSpriteSheets.ProgressChanged += ResetBuildStage_ProgressChanged;

            ComposeTextures_ReadReferencedSprites = new ProgressInfo("Compose Textures: Read Referenced Sprites");
            ComposeTextures_ReadReferencedSprites.ProgressChanged += ResetBuildStage_ProgressChanged;

            ComposeTextures_LoadReferencedSprites = new ProgressInfo("Compose Textures: Load Referenced Sprites");
            ComposeTextures_LoadReferencedSprites.ProgressChanged += ResetBuildStage_ProgressChanged;

            ComposeTextures_PackReferencedSprites = new ProgressInfo("Compose Textures: Pack Referenced Sprites");
            ComposeTextures_PackReferencedSprites.ProgressChanged += ResetBuildStage_ProgressChanged;

            ComposeTextures_WriteReferencedSpriteSheets = new ProgressInfo("Compose Textures: Write Referenced Sprite Sheets");
            ComposeTextures_WriteReferencedSpriteSheets.ProgressChanged += ResetBuildStage_ProgressChanged;

            ComposeTextures_ComposeTexturesXml = new ProgressInfo("Compose Textures: Compose textures.xml");
            ComposeTextures_ComposeTexturesXml.ProgressChanged += ResetBuildStage_ProgressChanged;

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
            ComposeTextures.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(ComposeTextures, 27000);

            FixTexts = new ProgressInfo("Fix Texts") { Max = 10 };
            FixTexts.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(FixTexts, 110);

            DeployXmlHash = new ProgressInfo("Deploy Xml Hash") { Max = 10 };
            DeployXmlHash.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(DeployXmlHash, 1);



            // DEPLOYMENT:
            WriteVersionInfo = new ProgressInfo("Write Version Info") { Max = 10 };
            WriteVersionInfo.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(WriteVersionInfo, 1);
            JavaBuild.AddChild(WriteVersionInfo, 1);

            ComposeCredits = new ProgressInfo("Compose Credits") { Max = 10 };
            ComposeCredits.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(ComposeCredits, 2);
            JavaBuild.AddChild(ComposeCredits, 2);

            WriteSpaceHavenXml = new ProgressInfo("Write Space Haven XML") { Max = 10 };
            WriteSpaceHavenXml.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(WriteSpaceHavenXml, 1000);
            JavaBuild.AddChild(WriteSpaceHavenXml, 1000);

            ComposeSpaceHavenJar = new ProgressInfo("Compose spacehaven.jar") { Max = 10 };
            ComposeSpaceHavenJar.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(ComposeSpaceHavenJar, 1500);
            JavaBuild.AddChild(ComposeSpaceHavenJar, 1500);

            DeployConfigJson = new ProgressInfo("Deploy config.json") { Max = 10 };
            DeployConfigJson.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(DeployConfigJson, 1);
            JavaBuild.AddChild(DeployConfigJson, 1);

            ComposeModsJson = new ProgressInfo("Compose mods.json") { Max = 10 };
            ComposeModsJson.ProgressChanged += ResetBuildStage_ProgressChanged;
            XmlBuild.AddChild(ComposeModsJson, 25);
            JavaBuild.AddChild(ComposeModsJson, 25);



            // Done.
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BuildDirectory);
            return false;
        }
    }


    public async Task<bool> TryBuildAsync()
    {
        try
        {
            Log.Info($"Starting Build...", Paths.BuildDirectory);

            Build = new BuildData(BuildSettings, Log);

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
                Log.Success("JAVA build skipped");
                JavaBuild.Complete();
                JavaBuild.RemoveAll(); // avoid further updates from children
            }

            // Skip XML build?
            if (NeedsXmlBuild)
                XmlBuild.Start();
            else
            {
                Log.Success("XML build skipped");
                XmlBuild.Complete();
                XmlBuild.RemoveAll(); // avoid further updates from children
            }

            // All builds skipped?
            if (!NeedsJavaBuild && !NeedsXmlBuild)
                return true;




            // Build Stage initialization:
            if (NeedsXmlBuild || (NeedsJavaBuild && !Build.HasXmlMods))
            {
                // Reset build stage files:
                ResetBuildStage.Start();
                Log.Info("Resetting build stage files...");
                if (!await TryResetBuildStageDirectoryAsync())
                    return false;
                if (!await IOUtils.TryCopyDirectoryAsync(Paths.TemplateStageDirectory, Paths.BuildStageDirectory, true, Log, ParallelOptions))
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
            }




            // XML specific:
            if (NeedsXmlBuild)
            {
                // Clear XML build directories:
                if (!await TryResetXmlBuildDirectories())
                    return false;

                // Read mod XML files, evaluating with previously loaded variable values:
                await TryLoadModsXmlAsync();

                // Merge XML:
                if (!await TryMergeXmlAsync())
                    return false;

                // Patch XML:
                if (!await TryPatchXmlAsync())
                    return false;

                // Merge Audio:
                if (!await TryComposeAudioAsync())
                    return false;

                // Generate Textures:
                if (!await TryComposeTexturesAsync())
                    return false;

                // Fix Text entries:
                if (!await TryFixTextsAsync())
                    return false;

                // Deploy XML hash:
                DeployXmlHash.Start();
                if (!await IOUtils.TryCopyFileAsync(Paths.BuildXmlHashPath, Paths.CacheXmlHashPath, true, Log, CT))
                    return false;
                DeployXmlHash.Complete();
            }




            // Deployment:
            if (NeedsJavaBuild || NeedsXmlBuild)
            {
                // Write version to haven.xml AND to version.txt:
                if (!await TryWriteVersionInfoAsync(CT))
                    return false;

                // Credits.txt:
                await TryWriteCreditsAsync();

                // Save all XML files to build stage:
                WriteSpaceHavenXml.Start();
                foreach (XmlFile xmlFile in Build.XmlFile.Values)
                {
                    if (!await xmlFile.TrySaveAsync(Log, CT))
                        return false;
                    WriteSpaceHavenXml.IncrementNormalized(1.0 / Build.XmlFile.Count);
                }
                WriteSpaceHavenXml.Complete();

                // Create/Deploy the modified spacehaven.jar:
                if (!await TryWriteSpaceHavenJarAsync())
                    return false;

                // Deploy config.json:
                DeployConfigJson.Start();
                if (!await IOUtils.TryCopyFileAsync(Paths.TemplateConfigJsonPath, Paths.CacheConfigJsonPath, true, Log, CT))
                    return false;
                DeployConfigJson.Complete();

                // Create mods.json:
                ComposeModsJson.Start();
                if (!await TryWriteModsJsonAsync())
                    return false;
                ComposeModsJson.Complete();
            }




            // Build completed:
            if (NeedsJavaBuild)
            {
                JavaBuild.RemoveAll();
                JavaBuild.Complete();
            }
            if (NeedsXmlBuild)
            {
                XmlBuild.RemoveAll();
                XmlBuild.Complete();
            }

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

            Initialization?.RemoveAll(); // owned by caller, do not dispose!
            JavaBuild?.RemoveAll(); // owned by caller, do not dispose!
            XmlBuild?.RemoveAll(); // owned by caller, do not dispose!

            ResetBuildStage?.Dispose();
            LoadSpaceHavenXml?.Dispose();
            ResetXmlBuild?.Dispose();
            LoadModsXml?.Dispose();
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
            DeployConfigJson?.Dispose();
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
        }
    }














    private async Task TryLoadModsXmlAsync()
    {
        LoadModsXml.Start();
        await Parallel.ForEachAsync(Build.Mods, ParallelOptions, async (mod, ct) =>
        {
            try
            {
                if (!await mod.TryLoadXmlFiles())
                {
                    Fail();
                    return;
                }
            }
            finally
            {
                lock (LoadModsXml)
                    LoadModsXml?.IncrementNormalized(1.0 / Build.Mods.Count);
            }
        });
        LoadModsXml.Complete();
    }















    private async Task<bool> TryInitializeAsync()
    {
        try
        {
            Log.Debug("Initializing build...", Paths.BuildDirectory);
            Initialization?.Start();

            // Initialize build data, and start logging build to file, right after the build directory reset:
            Build = new(BuildSettings, Log);

            // Add mods:
            Build.AddMods(BuildSettings.Mods);
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
            Initialization?.SetNormalized(0.95);

            // 'mods.json' file:
            Build.ModsJsonFile.GamePlatform = BuildSettings.GamePlatform;
            Build.ModsJsonFile.GameVersion = BuildSettings.SpaceHavenVersion.ToString();
            Build.ModsJsonFile.GameJarDir = Paths.SpaceHavenJarDir;
            Build.ModsJsonFile.AOPLibs.Add(Paths.CacheAspectjPath);
            Build.ModsJsonFile.AOPLibs.Add(Paths.CacheAspectjWeaverPath);
            Build.ModsJsonFile.AOPLibs.Sort();

            foreach (ModBuildData mod in Build.Mods)
            {
                ModInfo modInfo = new();
                modInfo.SchemaVersion = "1";
                modInfo.Name = mod.Name;
                modInfo.Version = mod.Version.ToString();
                modInfo.Directory = mod.Dir.AsStdPath();
                modInfo.ID = mod.ID;
                modInfo.Textures.AddRange(mod.SpritePaths.Select(path => path.AsStdPath()));
                modInfo.Audio.AddRange(mod.AudioPaths.Select(path => path.AsStdPath()));
                modInfo.Java.AddRange(mod.JarFilePaths.Select(path => path.AsStdPath()));
                modInfo.Other.AddRange(mod.OtherFilesPaths.Select(path => path.AsStdPath()));
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
            Initialization?.Complete();
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
            if (BuildSettings.SkipRebuilding)
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










    private async Task<bool> TryResetBuildStageDirectoryAsync()
    {
        try
        {
            Log.Info($@"Resetting build stage directory...");

            if (!await IOUtils.TryDeleteDirectoryContentAsync(Paths.BuildStageDirectory, Log, CT))
                return false;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable reset XML build: {ex}");
            return false;
        }
    }











    private async Task<bool> TryResetXmlBuildDirectories()
    {
        try
        {
            Log.Info($@"Resetting XML build...");
            ResetXmlBuild.Start();

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













    private async Task<bool> TryMergeXmlAsync()
    {
        try
        {
            Log.Info($@"Merging XML files...", Paths.BuildMergeDirectory);
            MergeXml.Start();

            EXmlFileType[] supportedXmlMergeFileTypes =
            [
                EXmlFileType.SpaceHavenSettings,
                EXmlFileType.Audio,
                EXmlFileType.Textures,
                EXmlFileType.Animations,
                EXmlFileType.Texts,
                EXmlFileType.Haven,
            ];

            HashSet<XmlFile> mergedSpaceHavenXmlFiles = [];

            foreach (ModBuildData mod in Build.Mods)
            {
                CT.ThrowIfCancellationRequested();

                try
                {
                    ILogger modLog = mod.Log;

                    if (!mod.HasLibraryXml)
                    {
                        modLog.Debug($@"This mod has no XML library files", mod.Dir);
                        continue;
                    }

                    modLog.Debug($"Performing XML merge operations...", mod.Dir);

                    HashSet<XmlFile> mergedModXmlFiles = [];

                    // Only merge supported files!
                    foreach (EXmlFileType xmlFileType in supportedXmlMergeFileTypes)
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
                    MergeXml?.IncrementNormalized(1.0 / Build.Mods.Count);
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
            MergeXml?.Complete();
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














    public async Task<bool> TryPatchXmlAsync()
    {
        try
        {
            Log.Info("Patching XML files...", Paths.BuildPatchDirectory);
            PatchXml.Start();

            EXmlFileType[] SupportedXmlPatchFileTypes =
            [
                EXmlFileType.SpaceHavenSettings,
                EXmlFileType.Audio,
                EXmlFileType.Textures,
                EXmlFileType.Animations,
                EXmlFileType.Texts,
                EXmlFileType.Haven,
            ];

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
                        modLog.Debug("This mod has no XML patch files", mod.Dir);
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
                    PatchXml?.IncrementNormalized(1.0 / Build.Mods.Count);
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
            PatchXml?.Complete();
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












    private async Task<bool> TryFixTextsAsync()
    {
        try
        {
            Log.Info($@"Fixing TEXT entries...", Paths.BuildAudioDirectory);
            FixTexts.Start();

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
                try { await spaceHavenTextsXmlFile.TrySaveToAsync(Paths.BuildTextsFile, Log, CT); }
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
            Log.Error($"Unable to fix TEXT entries: {ex}", Paths.BuildAudioDirectory);
            return false;
        }
    }












    private async Task<bool> TryComposeAudioAsync()
    {
        try
        {
            Log.Info($@"Composing AUDIO...", Paths.BuildAudioDirectory);
            ComposeAudio.Start();

            // Get and save animations document, for debugging:
            XmlFile spaceHavenAudioXmlFile = Build.XmlFile[EXmlFileType.Audio];
            if (!await spaceHavenAudioXmlFile.TrySaveToAsync(Paths.BuildAudioFile, Log, CT))
                return false;

            // Collect all assetPos filename references and save it to spriteReference objects:
            bool errors = false;
            OrderedDictionary<int, AudioBuildData> audioById = []; // keep original order!
            OrderedDictionary<string, AudioBuildData> audioByName = []; // keep original order!
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
                finally
                {
                    ComposeAudio.IncrementNormalized(1.0 / audioNodes.Count);
                }
            }
            if (errors)
                return false;

            // Copy audio files:
            foreach (AudioBuildData audio in audioByName.Values)
            {
                string targetAbsolutePath = Paths.BuildStageDirectory.CombineAsOSPath(audio.TargetRelativePath);
                if (targetAbsolutePath.EscapesDirectory(Paths.BuildStageDirectory))
                {
                    Log.Error($@"Invalid target audio file path ""{targetAbsolutePath}"" for {audio}");
                    return false;
                }

                if (!await IOUtils.TryCopyFileAsync(audio.SourceAbsolutePath, targetAbsolutePath, true, Log, CT))
                    return false;
            }




            // Done.
            ComposeAudio?.Complete();
            Log.Success($"AUDIO files ready", Paths.BuildAudioDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose AUDIO: {ex}", Paths.BuildAudioDirectory);
            return false;
        }
    }











    private async Task<bool> TryComposeTexturesAsync()
    {




        try
        {
            Log.Info($@"Composing TEXTURES...", Paths.BuildTexturesDirectory);
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
            SpriteAtlasBuildData predefinedAtlas = new("PREDEFINED");
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



                Log.Info("Compose Textures: Loading Predefined Sprite Sheets...");
                ComposeTextures_LoadPredefinedSpriteSheets.Start();

                // Spritesheet global keys must be sequential:
                Dictionary<XElement, int> spriteSheetGlobalIds =
                    modifiedSpriteSheetNodes.ToDictionary(t => t, t => Build.AllocateNextNumericId(EKeyPool.SpriteSheet));

                // Load predefined spritesheets (those declared in mod textures.xml files):
                await Parallel.ForEachAsync(modifiedSpriteSheetNodes, ParallelOptions, async (t, ct) =>
                //await Parallel.ForEachAsync(modifiedSpriteSheetNodes, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (t, ct) =>
                {
                    // mod:
                    string modName = t.Attribute(NodeType.ATTRIBUTE_OWNER).Value;
                    ModBuildData mod = Build.Mods.FirstOrDefault(mod => mod.Name == modName);
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
                        SpriteSheetBuildData spriteSheet = new(key, absoluteImagePath, predefinedAtlas) { GlobalId = globalId, };
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
                        SpriteSheetBuildData spriteSheet = new(key, width, height, 2 * modifiedSpriteNodes.Count + 1, 0, predefinedAtlas) { GlobalId = globalId, };
                        lock (predefinedAtlas)
                            predefinedAtlas.Add(spriteSheet);
                    }

                    // remap:
                    t.SetAttributeValue("i", globalId);

                    // progress:
                    lock (ComposeTextures_LoadPredefinedSpriteSheets)
                        ComposeTextures_LoadPredefinedSpriteSheets.IncrementNormalized(1.0 / modifiedSpriteSheetNodes.Count);
                });
                ComposeTextures_LoadPredefinedSpriteSheets.Complete();

                CT.ThrowIfCancellationRequested();




                Log.Info("Compose Textures: Loading Predefined Sprites...");
                ComposeTextures_LoadPredefinedSprites.Start();

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
                    ModBuildData mod = Build.Mods.FirstOrDefault(mod => mod.Name == modName);
                    if (mod == null)
                    {
                        Log.Error($@"Unknown mod for <re> node in file ""{spaceHavenTexturesXmlFile.FileName}"" line {re.Line()}", spaceHavenTexturesXmlFile.Path);
                        Fail();
                        return;
                    }

                    // spritesheet:
                    _ = int.TryParse(re.Attribute("t")?.Value, out int spriteSheetLocalID);
                    SpriteSheetBuildData spriteSheet = predefinedAtlas.SpriteSheets.FirstOrDefault(ss => ss.LocalId == spriteSheetLocalID);
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
                    SpriteBuildData sprite;
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
                        ComposeTextures_LoadPredefinedSprites.IncrementNormalized(1.0 / modifiedSpriteNodes.Count);
                });
                ComposeTextures_LoadPredefinedSprites.Complete();



                CT.ThrowIfCancellationRequested();




                // Remap <assetPos a="..."> in animations.xml:
                List<SpriteBuildData> allSprites = predefinedAtlas.Sprites;
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




                Log.Info("Compose Textures: Writing Predefined Sprite Sheets...");
                ComposeTextures_WritePredefinedSpriteSheets.Start();

                // Render spritesheets
                await Parallel.ForEachAsync(predefinedAtlas.SpriteSheets, ParallelOptions, async (spriteSheet, ct) =>
                //await Parallel.ForEachAsync(predefinedAtlas.SpriteSheets, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (spriteSheet, ct) =>
                {
                    if (spriteSheet.IsRendered)
                    {
                        spriteSheet.TryRenderFromSprites(Log);
                        string pngFilename = $"{spriteSheet.GlobalId}.png";

                        // Save rendered spritesheets as PNG file, for debugging:
                        if (!await spriteSheet.TryExportToPngAsync(IOUtils.CombineAsOSPath(Paths.BuildTexturesDirectory, pngFilename), Log, ct))
                        {
                            Fail();
                            return;
                        }
                    }

                    // Save as CIM file to build stage directory:
                    string cimFilename = $"{spriteSheet.GlobalId}.cim";
                    if (!await spriteSheet.TryExportToCimAsync(IOUtils.CombineAsOSPath(Paths.BuildStageLibraryDirectory, cimFilename), Log, ct))
                    {
                        Fail();
                        return;
                    }

                    // progress:
                    lock (ComposeTextures_WritePredefinedSpriteSheets)
                        ComposeTextures_WritePredefinedSpriteSheets.IncrementNormalized(1.0 / predefinedAtlas.SpriteSheets.Count);
                });
                ComposeTextures_WritePredefinedSpriteSheets.Complete();
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
                    ModBuildData mod = Build.Mods.FirstOrDefault(mod => mod.Name == modName);
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
                    string spriteRefKey = SpriteReference.GetKey(mod.Name, assetPosFilenameReference, filter);

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
                    Log.Info($"No mod sprite references were found requiring the texture filter '{filter}'", Paths.BuildTexturesDirectory);
                    continue;
                }

                Log.Info($"Compose Textures: Loading Referenced Sprites ({filter.ToString()})...");

                CT.ThrowIfCancellationRequested();

                // Load referenced sprites:
                SortedDictionary<string, SpriteBuildData> sprites = [];
                await Parallel.ForEachAsync(spriteRefs, ParallelOptions, async (spriteRef, ct) =>
                //await Parallel.ForEachAsync(spriteRefs, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (spriteRef, ct) =>
                {
                    try
                    {
                        Log.Debug($"Loading sprite {spriteRef.Key}...", Paths.BuildTexturesDirectory);

                        SpriteBuildData sprite = new(spriteRef.Key, spriteRef.LocalID, spriteRef.AbsolutePath);

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

                ComposeTextures_PackReferencedSprites.IncrementNormalized(spriteRefs.Count / (double)spriteReferences.Count);







                // Sequentially assign spritesheet KEY and sprite NAME/ID: 
                foreach (SpriteSheetBuildData spriteSheet in spriteAtlas.SpriteSheets)
                {
                    spriteSheet.GlobalId = Build.AllocateNextNumericId(EKeyPool.SpriteSheet);
                    foreach (SpriteBuildData sprite in spriteSheet.Sprites)
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
                        await spriteSheet.TryExportToPngAsync(IOUtils.CombineAsOSPath(Paths.BuildTexturesDirectory, $"{spriteSheet.GlobalId}.png"), Log, ct);
                    }
                    finally
                    {
                        lock (ComposeTextures_WriteReferencedSpriteSheets)
                            ComposeTextures_WriteReferencedSpriteSheets.IncrementNormalized((1.0 / spriteAtlas.SpriteSheets.Count) * (spriteRefs.Count / (double)spriteReferences.Count));
                    }
                });
                CT.ThrowIfCancellationRequested();








                // For each sprite sheet, add a CIM texture entry to the textures XML file:
                Log.Info($@"Composing textures.xml...", Paths.BuildTexturesDirectory);
                XElement parentTexturesCimNode = spaceHavenTexturesXmlFile.GetParentNode(NodeType.TexturesCim);
                foreach (SpriteSheetBuildData spriteSheet in spriteAtlas.SpriteSheets.OrderBy(s => s.GlobalId))
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
                foreach (SpriteBuildData sprite in sprites.Values.OrderBy(sprite => sprite.GlobalName))
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
            Log.Success($"TEXTURES ready", Paths.BuildTexturesDirectory);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose TEXTURES: {ex}", Paths.BuildTexturesDirectory);
            return false;
        }
    }








    private async Task<bool> TryWriteCreditsAsync()
    {
        try
        {
            Log.Debug($"Adding mod authors to '{SpaceHavenConstants.EXTRA_CREDITS_TXT}' file...", Paths.CacheDirectory);
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
            Log.Error($"Unable to compose final '{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' file: {ex}", Paths.CacheDirectory);
            return false;
        }
    }









    private async Task<bool> TryWriteSpaceHavenJarAsync()
    {
        try
        {
            Log.Info($"Composing '{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' file...", Paths.CacheDirectory);
            ComposeSpaceHavenJar.Start();

            // Select files to add to template JAR:
            DirectoryInfo di = new(Paths.BuildStageDirectory);
            FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
            JarAppender jar = new();
            if (!await jar.AppendTo(Paths.TemplateJarPath, Paths.CacheJarPath, Paths.BuildStageDirectory, files, Log, ParallelOptions))
                return false;

            ComposeSpaceHavenJar.SetNormalized(0.85);

            // Calculate hash of modified JAR:
            string hash = await XxHash64Calculator.ComputeFromFileAsync(Paths.CacheJarPath, Log, CT);
            if (!await IOUtils.TryWriteAllTextAsync(Paths.CacheModifiedJarHashPath, hash, Log, CT))
                return false;

            // Copy original JAR hash file:
            if (!await IOUtils.TryCopyFileAsync(Paths.TemplateJarHashPath, Paths.BuildJarHashPath, true, Log, CT))
                return false;
            if (!await IOUtils.TryCopyFileAsync(Paths.BuildJarHashPath, Paths.CacheJarHashPath, true, Log, CT))
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

            string jarsTxtPath = IOUtils.CombineAsOSPath(Paths.CacheDirectory, "jars.txt");
            if (!await IOUtils.TryWriteAllTextAsync(jarsTxtPath, classPaths.JoinToString("\r\n"), Log, CT))
                return false;

            ComposeSpaceHavenJar.SetNormalized(0.95);

            // Deploy aop / java agent libs to cache dir:
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, ModdingConstants.ASPECTJ), IOUtils.CombineAsOSPath(Paths.CacheDirectory, ModdingConstants.ASPECTJ), true, Log, CT))
                return false;
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, ModdingConstants.ASPECTJWEAVER), IOUtils.CombineAsOSPath(Paths.CacheDirectory, ModdingConstants.ASPECTJWEAVER), true, Log, CT))
                return false;
            if (!await IOUtils.TryCopyFileAsync(IOUtils.CombineAsOSPath(Paths.AppDir, "LauncherAgent.jar"), IOUtils.CombineAsOSPath(Paths.CacheDirectory, "LauncherAgent.jar"), true, Log, CT))
                return false;

            // Done.
            ComposeSpaceHavenJar?.Complete();
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











    private async Task<bool> TryWriteModsJsonAsync()
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
        try { await FileLogger.DisposeAsync(); } catch { }
    }
    #endregion
}
