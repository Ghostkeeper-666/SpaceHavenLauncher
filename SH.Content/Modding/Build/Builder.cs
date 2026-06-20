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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Content.Modding.Build;

public sealed class Builder : IAsyncDisposable
{
    private readonly BuildPathData Paths;
    private readonly BuildSettings BuildSettings;

    private readonly LoggerCollection Log;
    private FileLogger FileLogger;

    private BuildData Build;
    private IReadOnlyList<ModData> Mods => BuildSettings.Mods;
    private ParallelOptions ParallelOptions => BuildSettings.ParallelOptions;
    private CancellationToken CT => BuildSettings.CT;

    private IProgressInfo Initialization => BuildSettings.Initialization;

    private IProgressInfo XmlBuild => BuildSettings.XmlBuild;
    private IProgressInfo ResetXmlBuild;
    private IProgressInfo CopyTemplateFiles;
    private IProgressInfo LoadTemplateXml;
    private IProgressInfo MergeAudio;
    private IProgressInfo PackTextures;
    private IProgressInfo AssignCimFileID;
    private IProgressInfo WriteCimFiles;
    private IProgressInfo AssignTextureID;
    private IProgressInfo MapAnimationToTexture;
    private IProgressInfo MergeXmlFiles;
    private IProgressInfo PatchXmlFiles;
    private IProgressInfo BuildJarFile;

    private IProgressInfo JavaBuild => BuildSettings.JavaBuild;
    private IProgressInfo ResetJavaBuild;
    private IProgressInfo PrepareJavaFiles;

    public bool IsNewJar { get; private set; }
    public bool NeedsXmlBuild { get; private set; }
    public bool NeedsJavaBuild { get; private set; }

    private readonly EXmlFileType[] XmlMergeFileTypes =
    [
        EXmlFileType.SpaceHavenSettings,
        EXmlFileType.Animations,
        EXmlFileType.Texts,
        EXmlFileType.Haven,
    ];

    private readonly Stopwatch Clock = new();




    public Builder(BuildPathData paths, BuildSettings buildSettings, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        BuildSettings = buildSettings ?? throw new ArgumentNullException(nameof(buildSettings));
        Log = new LoggerCollection(logger);
    }



    public async Task<bool> TryBuildAsync()
    {
        try
        {
            Log.Info($"Starting {this}...", Paths.BuildDir);

            // No mods?
            if (!(Mods?.Any() ?? false))
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

                // JAVA build completed:
                JavaBuild.Complete();
            }
            else
            {
                Log.Success("JAVA build skipped");
                JavaBuild.Complete();
            }

            // XML Build:
            if (NeedsXmlBuild)
            {
                // Clear build directories:
                if (!await TryResetXmlBuild())
                    return false;

                // Copy template stage:
                Clock.Restart();
                if (!await IOUtils.TryCopyDirectoryAsync(Paths.TemplateStageDir, Paths.BuildStageDir, true, Log, ParallelOptions))
                    return false;
                CopyTemplateFiles.Complete();
                Log.Debug($"{ResetXmlBuild} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildDir);

                // Load build XML files:
                Clock.Restart();
                if (!await Build.TryLoadXmlFiles(CT))
                    return false;
                Log.Debug($"{ResetXmlBuild} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildDir);
                LoadTemplateXml.Complete();

                // Write modified version:
                if (!await Build.TryWriteVersion(Log, CT))
                    return false;

                // Merge Audio:
                if (!await TryMergeEveryAudio())
                    return false;

                // Merge Textures:
                if (!await TryMergeTextures())
                    return false;

                // Merge XML:
                if (!await TryMergeXML())
                    return false;

                // Patch XML:
                if (!await TryPatchXML())
                    return false;

                // Build JAR:
                if (!await TryCreateSpaceHavenJarFile())
                    return false;

                // Save all XML files:
                foreach (XmlFile xmlFile in Build.XmlFile.Values)
                    if (!await xmlFile.TrySaveAsync(Log, CT))
                        return false;

                // Copy XML hash file:
                if (!await IOUtils.TryCopyFileAsync(Paths.BuildXmlHashPath, Paths.CacheXmlHashPath, true, Log, CT))
                    return false;

                // XML build completed:
                XmlBuild.Complete();
            }
            else
            {
                Log.Success("XML build skipped");
                XmlBuild.Complete();
            }

            // Create a fresh new config.json
            if (!await TryCreateConfigJson())
                return false;

            // Create file for JAVA modders:
            if (!await TryCreatModsJson())
                return false;
            // Done.
            Log.Success($"{this} has completed", Paths.BuildDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BuildDir);
            Log.Error($"{this} has failed", Paths.BuildDir);
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
            LoadTemplateXml?.Dispose();
            MergeAudio?.Dispose();
            PackTextures?.Dispose();
            AssignCimFileID?.Dispose();
            WriteCimFiles?.Dispose();
            AssignTextureID?.Dispose();
            MapAnimationToTexture?.Dispose();
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
            XmlBuild.Add(ResetXmlBuild, 2000);

            CopyTemplateFiles = new ProgressInfo("Copy Template Files");
            XmlBuild.Add(CopyTemplateFiles, 2000);

            LoadTemplateXml = new ProgressInfo("Load Template Xml");
            XmlBuild.Add(LoadTemplateXml, 2000);

            MergeAudio = new ProgressInfo("Merge Audio");
            XmlBuild.Add(MergeAudio, 35);

            PackTextures = new ProgressInfo("Pack Textures");
            XmlBuild.Add(PackTextures, 2270);

            AssignCimFileID = new ProgressInfo("Assign CIM file ID");
            XmlBuild.Add(AssignCimFileID, 1);

            WriteCimFiles = new ProgressInfo("Write CIM files");
            XmlBuild.Add(WriteCimFiles, 270);

            AssignTextureID = new ProgressInfo("Assign Texture ID");
            XmlBuild.Add(AssignTextureID, 430);

            MapAnimationToTexture = new ProgressInfo("Map Animation to Texture");
            XmlBuild.Add(MapAnimationToTexture, 45);

            MergeXmlFiles = new ProgressInfo("Merge XML files");
            XmlBuild.Add(MergeXmlFiles, 6900);

            PatchXmlFiles = new ProgressInfo("Patch XML files");
            XmlBuild.Add(PatchXmlFiles, 13450);

            BuildJarFile = new ProgressInfo("Create JAR file");
            XmlBuild.Add(BuildJarFile, 260);

            ResetJavaBuild = new ProgressInfo("Reset JAVA Build");
            JavaBuild.Add(ResetJavaBuild, 2000);

            PrepareJavaFiles = new ProgressInfo("Prepare JAVA Files");
            JavaBuild.Add(PrepareJavaFiles, 2000);

            // Done.
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BuildDir);
            return false;
        }
    }

    private async Task<bool> TryInitialize()
    {
        try
        {
            Log.Debug("Initializing build...", Paths.BuildDir);
            Clock.Restart();

            // Initialize build data, and start logging build to file, right after the build directory reset:
            Build = new(BuildSettings, Paths, Log);

            // Add mods:
            Build.AddMods(Mods);
            Initialization?.SetNormalized(0.45);

            // Load mod variables:
            foreach (ModBuildData mod in Build.Mods)
                if (!await mod.TryMapVariables())
                    return false;
            Initialization?.SetNormalized(0.50);

            // Read mod XML files, evaluating with previously loaded variable values:
            foreach (ModBuildData mod in Build.Mods)
                if (!await mod.TryLoadXmlFiles())
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

            // Mods file:
            Build.ModsJsonFile.AOPLibs.Add(ModdingConstants.ASPECTJ);
            Build.ModsJsonFile.AOPLibs.Add(ModdingConstants.ASPECTJWEAVER);
            Build.ModsJsonFile.AOPLibs.Sort();

            foreach (ModBuildData mod in Build.Mods)
            {
                ModInfo modInfo = new();
                modInfo.Name = mod.Name;
                modInfo.Version = mod.Version;
                modInfo.Directory = mod.Directory.AsStandardPath();
                modInfo.ID = mod.ID;
                modInfo.Textures.AddRange(mod.TextureFilePaths.Select(path => path.AsStandardPath()));
                modInfo.Audio.AddRange(mod.AudioFilePaths.Select(path => path.AsStandardPath()));
                modInfo.Java.AddRange(mod.JavaFilePaths.Select(path => path.AsStandardPath()));
                modInfo.Other.AddRange(mod.OtherFilePaths.Select(path => path.AsStandardPath()));

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
            Log.Debug($"{Initialization} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildDir);
            Clock.Restart();

            // Done.
            Log.Success("Build initialization is complete", Paths.BuildDir);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Unable to initialize build: {ex}", Paths.BuildDir);
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
            string templateJarHash = File.Exists(Paths.TemplateJarHashPath) ? await IOUtils.TryReadAllTextAsync(Paths.TemplateJarHashPath, Log, CT) : null;
            templateJarHash ??= string.Empty;
            string buildJarHash = File.Exists(Paths.BuildJarHashPath) ? await IOUtils.TryReadAllTextAsync(Paths.BuildJarHashPath, Log, CT) : null;
            buildJarHash ??= string.Empty;
            string cacheJarHash = File.Exists(Paths.CacheJarHashPath) ? await IOUtils.TryReadAllTextAsync(Paths.CacheJarHashPath, Log, CT) : null;
            cacheJarHash ??= string.Empty;
            IsNewJar = templateJarHash != buildJarHash || buildJarHash != cacheJarHash;

            // XML:
            NeedsXmlBuild = Build.HasXmlMods &&
            (
                IsNewJar ||
                !File.Exists(Paths.CacheJarPath) ||
                !File.Exists(Paths.CacheJarHashPath) ||
                (Build.XmlHash ?? string.Empty) != (await IOUtils.TryReadAllTextAsync(Paths.CacheXmlHashPath, Log, CT) ?? string.Empty)
            );

            // JAVA:
            NeedsJavaBuild = Build.HasJavaMods && (
                !File.Exists(Paths.CacheConfigJsonPath) ||
                !File.Exists(Paths.CacheJavaHashPath) ||
                (Build.JavaHash ?? string.Empty) != (await IOUtils.TryReadAllTextAsync(Paths.CacheJavaHashPath, Log, CT) ?? string.Empty)
            );

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

            if (!await IOUtils.TryDeleteDirectoryAsync(Paths.BuildAudioDir, Log, CT))
                return false;
            ResetXmlBuild.SetNormalized(0.10);

            if (!await IOUtils.TryDeleteDirectoryAsync(Paths.BuildTexturesDir, Log, CT))
                return false;
            ResetXmlBuild.SetNormalized(0.20);

            if (!await IOUtils.TryDeleteDirectoryAsync(Paths.BuildMergeDir, Log, CT))
                return false;
            ResetXmlBuild.SetNormalized(0.50);

            if (!await IOUtils.TryDeleteDirectoryAsync(Paths.BuildPatchDir, Log, CT))
                return false;
            ResetXmlBuild.SetNormalized(0.80);

            if (!await IOUtils.TryDeleteDirectoryAsync(Paths.BuildStageDir, Log, CT))
                return false;

            // Done.
            ResetXmlBuild.Complete();
            Log.Debug($"{ResetXmlBuild} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable reset XML build: {ex}");
            return false;
        }
    }

    private async Task<bool> TryMergeEveryAudio()
    {
        try
        {
            Log.Info($@"Merging AUDIO...", Paths.BuildAudioDir);
            Clock.Restart();

            foreach (ModBuildData mod in Build.Mods)
            {
                CT.ThrowIfCancellationRequested();

                ILogger modLog = mod.Log;

                try
                {
                    // Are audio files available?
                    if (!mod.HasAudio)
                    {
                        modLog.Debug($"This mod has no audio files", mod.Directory);
                        continue;
                    }

                    modLog.Debug($"Performing audio merge operations...", mod.AudioDir);

                    // Audio must be added by library XML files because the ycontain the relative path of the audio:
                    // TODO: maybe we could use the relative path inside the mod's audio folder insted?
                    if (mod.XmlFiles[EXmlFileType.Audio].Count <= 0)
                    {
                        modLog.Error($"New audio files MUST be added through a XML file in the library directory of your mod. Afterwards you may patch the audio XML nodes", mod.Directory);
                        return false;
                    }

                    modLog.Debug($@"Adding the following audio files: {mod.AudioFilePaths.Select(path => $"\n- {path}").OrderBy(str => str).JoinToString()}", mod.Directory);

                    // Get target:
                    XmlFile xmlFile = Build.XmlFile[EXmlFileType.Audio];
                    XElement parentNode = xmlFile.GetParentNode(NodeType.Audio);
                    if (parentNode == null)
                    {
                        modLog.Error("Unable to find root node of audio XML", xmlFile.Path);
                        return false;
                    }

                    // Process each source:
                    foreach (XmlFile modXmlFile in mod.XmlFiles[EXmlFileType.Audio].Values)
                    {
                        foreach (XElement node in modXmlFile.Xml.GetEveryAudio())
                        {
                            CT.ThrowIfCancellationRequested();

                            string audioName = node.Attribute(NodeType.Audio.NameAttribute)?.Value; // used for haven references
                            if (audioName.IsNullOrWhiteSpace())
                            {
                                modLog.Error($@"Invalid audio entry without a defined name ('{NodeType.Audio.NameAttribute}'), file=""{modXmlFile.FileName}"" line={node.Line()}.", modXmlFile.Path);
                                return false;
                            }
                            if (!audioName.Contains('_'))
                            {
                                // Space Haven's audio system stops working if new audio entries do not have at least one underscore character...
                                modLog.Warn($"Audio entry name '{audioName}' does not have at least 1 underscore character '_' => this could stop the game's audio system completely!", mod.AudioDir);
                            }

                            string audioType = node.Attribute("at")?.Value; // used for sorting audio entries, otherwise game audio crashes!
                            if (audioType.IsNullOrWhiteSpace())
                            {
                                modLog.Error($@"Invalid audio entry without a defined audio type ('at'), file=""{modXmlFile.FileName}"" line={node.Line()}.", modXmlFile.Path);
                                return false;
                            }

                            string mp3Path = node.Attribute("mp3")?.Value; // relative path
                            string oggPath = node.Attribute("ogg")?.Value; // relative path

                            string relativeAudioPath = mp3Path ?? oggPath;
                            string actualFileExt = Path.GetExtension(relativeAudioPath).Trim('.').ToLowerInvariant();
                            string expectedFileExt = mp3Path == null ? "ogg" : "mp3";

                            // Organize mod audio in objects:
                            AudioBuildData audio = new()
                            {
                                Name = audioName,
                                Id = int.TryParse(node.Attribute(NodeType.Audio.IdAttribute)?.Value, out int id) ? id : 0,
                                AudioEncoder = mp3Path.IsNullOrWhiteSpace() ? EAudioEncoder.ogg : EAudioEncoder.mp3,
                                AudioType = Enum.TryParse(audioType, true, out EAudioType type) ? type : EAudioType.Sound,
                                RelativePath = relativeAudioPath.AsOSPath(),
                            };
                            if (mod.Audio.ContainsKey(audio.Name))
                            {
                                modLog.Error($@"duplicate audio entry '{audio.Name}', at line {node.Line()}, file ""{modXmlFile.Path}""", modXmlFile.Path);
                                return false;
                            }
                            mod.Audio.Add(audio.Name, audio);

                            // Validate file extension:
                            if (expectedFileExt != actualFileExt)
                            {
                                modLog.Error($@"audio entry '{audio.Name}' has wrong file extension: expected={expectedFileExt} actual={actualFileExt}, at line {node.Line()}, file ""{modXmlFile.Path}""", modXmlFile.Path);
                                return false;
                            }

                            // New audio file?
                            string audioFilename = Path.GetFileNameWithoutExtension(relativeAudioPath);
                            string modAudioPath = mod.AudioFilePaths.FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Equals(audioFilename));
                            if (modAudioPath.IsNullOrWhiteSpace() || !File.Exists(modAudioPath))
                            {
                                // Mod probably references an already existing audio file:
                                audio.AbsolutePath = Path.Combine(Paths.BuildStageDir, audio.RelativePath);
                                if (!File.Exists(audio.AbsolutePath))
                                {
                                    modLog.Error($@"Audio file referenced by entry '{audio.Name}' does not exist, at line {node.Line()}, file ""{modXmlFile.Path}""", modXmlFile.Path);
                                    return false;
                                }
                            }
                            else // it must be a new audio file
                            {
                                audio.AbsolutePath = modAudioPath;

                                // Copy new audio file to target location:
                                string targetPath = Path.Combine(Paths.BuildStageDir, audio.RelativePath);

                                if (File.Exists(targetPath)) // Overwriting?
                                    modLog.Warn($@"Replacing audio file: ""{audio.RelativePath}""", Path.GetDirectoryName(targetPath));
                                else
                                    modLog.Debug($@"Adding new audio file: ""{audio.RelativePath}""", Path.GetDirectoryName(targetPath));

                                // Copy audio to build stage directory:
                                if (!await IOUtils.TryCopyFileAsync(modAudioPath, targetPath, true, modLog, CT))
                                    return false;

                                // Copy audio file to the mod's build audio folder, for debugging:
                                if (!await IOUtils.TryCopyFileAsync(modAudioPath, Path.Combine(mod.AudioDir, audio.RelativePath), true, modLog, CT))
                                    return false;
                            }

                            CT.ThrowIfCancellationRequested();

                            // Merge audio XML:
                            XElement[] existingNodes =
                                xmlFile.GetNodes(NodeType.Audio)
                                .Where(n =>
                                    n.Attribute("n")?.Value == audio.Name ||
                                    n.Attribute(NodeType.Audio.IdAttribute)?.Value == audio.Id.ToString()
                                ).ToArray() ?? [];

                            // Remove existing nodes:
                            foreach (XElement existingNode in existingNodes)
                            {
                                CT.ThrowIfCancellationRequested();

                                string existingMod = existingNode.Attribute(NodeType.MergedByMod)?.Value;
                                string existingId = existingNode.Attribute(NodeType.Audio.IdAttribute)?.Value;
                                string existingName = existingNode.Attribute(NodeType.Audio.NameAttribute)?.Value;

                                if (existingMod == null)
                                    modLog.Debug($"Replacing existing audio node with {NodeType.Audio.IdAttribute}={existingId} and {NodeType.Audio.NameAttribute}='{existingName}'", modXmlFile.Path);
                                else if (existingMod == mod.Name)
                                    modLog.Warn($"Replacing existing audio node with {NodeType.Audio.IdAttribute}={existingId} and {NodeType.Audio.NameAttribute}='{existingName}', which was previously modified by the same mod => This could be an ERROR", modXmlFile.Path);
                                else
                                    modLog.Warn($"Replacing existing audio node with {NodeType.Audio.IdAttribute}={existingId} and {NodeType.Audio.NameAttribute}='{existingName}', which was previously modified by the mod '{existingMod}' => This could be a MOD INCOMPATIBILITY", modXmlFile.Path);
                                existingNode.Remove();
                            }

                            // Locate an insertion position for the new audio node, within the same Audio Type group, and sorted ascnding by ID:
                            node.SetAttributeValue(NodeType.MergedByMod, mod.Name);
                            XElement sibling = null;
                            foreach (XElement other in parentNode.Elements("a").Reverse())
                            {
                                string otherIdStr = other.Attribute(NodeType.Audio.IdAttribute)?.Value;
                                string otherAudioTypeStr = other.Attribute("at")?.Value;

                                if (!int.TryParse(otherIdStr, out int otherId))
                                    continue;

                                if (otherId == audio.Id)
                                {
                                    // Not expected here...
                                    modLog.Error($@"An audio entry with {NodeType.Audio.IdAttribute}={audio.Id} already exists!");
                                    return false;
                                }

                                if (otherId > audio.Id)
                                    continue;

                                if (!Enum.TryParse(otherAudioTypeStr, true, out EAudioType at) || at != audio.AudioType)
                                    continue;

                                sibling = other;
                                break;
                            }

                            // Insert audio node:
                            if (sibling != null)
                                sibling.AddAfterSelf(new XElement(node));
                            else // rare situation
                                parentNode.Add(new XElement(node));
                        }

                        // Save mod audio XML file:
                        if (!await modXmlFile.TrySaveToAsync(Path.Combine(mod.AudioDir, modXmlFile.FileName), modLog, CT))
                            return false;
                    }

                    // Save audio XML file:
                    if (!await xmlFile.TrySaveAsync(modLog, CT))
                        return false;

                }
                finally
                {
                    MergeAudio?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            }

            // Done.
            MergeAudio?.Complete();
            Log.Debug($"{MergeAudio} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildAudioDir);
            Log.Success($"AUDIO files ready", Paths.BuildAudioDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to merge AUDIO : {ex}", Paths.BuildAudioDir);
            return false;
        }
    }

    private async Task<bool> TryMergeTextures()
    {
        try
        {
            Log.Info($@"Merging TEXTURES...", Paths.BuildTexturesDir);
            Clock.Restart();



            // Detect mods trying to use library/textures*.xml file for anything:
            bool errors = false;
            foreach (ModBuildData mod in Build.Mods)
            {
                ILogger modLog = mod.Log;

                XmlFile[] modXmlFiles = mod.XmlFiles[EXmlFileType.Textures].Values.ToArray();
                foreach (XmlFile modXmlFile in modXmlFiles)
                {
                    modLog.Error($@"The mod is not allowed to define ""library/texture*"" XML files! If the intention was to replace textures, do it with ""library/animations*"" and ""patch/animations*"" files");
                    errors = true;
                }
            }
            if (errors)
                return false;



            // Create a sprite atlas for each mod:
            Log.Debug($@"Packing all sprites into sprite sheets...", Paths.BuildTexturesDir);
            Clock.Restart();
            await Parallel.ForEachAsync(Build.Mods, ParallelOptions, async (mod, ct) =>
            {
                ILogger modLog = mod.Log;

                try
                {
                    // Are sprites available?
                    if (!mod.HasTextures)
                    {
                        modLog.Debug($@"This mod has no texture files", mod.Directory);
                        return;
                    }

                    // Read paths of individual Sprite files:
                    SortedDictionary<string, SpriteBuildData> sprites = [];
                    foreach (string path in mod.TextureFilePaths)
                        if (!sprites.ContainsKey(path))
                            sprites.Add(path, new(Path.GetFileNameWithoutExtension(path), sprites.Count, path));
                    if (sprites.Count <= 0)
                        return;

                    int expectedSpriteCount = sprites.Count;
                    modLog.Debug($@"Adding the following {expectedSpriteCount} texture(s): {sprites.Values.Select(sprite => sprite.FileName).OrderBy(str => str).JoinToString(", ")}", mod.Directory);

                    // Pack sprites to sprite sheets:
                    mod.SpriteAtlas.Clear();
                    bool crop = !BuildSettings.ForceSpritesheetSize2048;
                    if (!mod.SpriteAtlas.Add(sprites.Values, 2048, 2048, crop, modLog))
                    {
                        BuildSettings.Fail();
                        return;
                    }

                    // Double-check number of added sprites:
                    int actualSpriteCount = mod.SpriteAtlas.Sprites.Count;
                    if (expectedSpriteCount != actualSpriteCount)
                    {
                        modLog.Error($@"Expected {expectedSpriteCount} texture(s), but only the {actualSpriteCount} following texture(s) could be added: {mod.SpriteAtlas.Sprites.Select(sprite => sprite.FileName).OrderBy(str => str).JoinToString(", ")}", mod.Directory);
                        BuildSettings.Fail();
                        return;
                    }

                    // Draw sprites to their spritesheets:
                    foreach (SpriteSheetBuildData spriteSheet in mod.SpriteAtlas.SpriteSheets)
                    {
                        if (!spriteSheet.TryGenerateFromSprites(modLog))
                        {
                            BuildSettings.Fail();
                            return;
                        }
                    }
                    modLog.Debug($@"{mod.SpriteAtlas.SpriteCount} texture(s) found, mapped to {mod.SpriteAtlas.SpriteSheets.Count} CIM file(s)", mod.Directory);
                }
                catch (OperationCanceledException ex) { modLog.Debug(ex); }
                catch (Exception ex)
                {
                    modLog.Error(ex);
                    BuildSettings.Fail();
                    return;
                }
                finally
                {
                    PackTextures?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            });
            if (BuildSettings.BuildFailure)
                return false;
            PackTextures?.Complete();
            Log.Debug($"{PackTextures} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDir);

            CT.ThrowIfCancellationRequested();





            // Assign a global ID to each Sprite Sheet, and configure a corresponding XML node:
            Log.Debug($@"Assigning IDs to new sprite sheets...", Paths.BuildTexturesDir);
            Clock.Restart();
            XmlFile texturesXmlFile = Build.XmlFile[EXmlFileType.Textures];
            XElement parentTexturesCimNode = texturesXmlFile.GetParentNode(NodeType.TexturesCim);
            foreach (ModBuildData mod in Build.Mods)
            {
                foreach (SpriteSheetBuildData spriteSheet in mod.SpriteAtlas.SpriteSheets.OrderBy(s => s.LocalId))
                {
                    XElement t = new("t");
                    t.SetAttributeValue("i", spriteSheet.GlobalId = Build.AllocateNextNumericId(EIdPool.TexturesCim));
                    t.SetAttributeValue("w", spriteSheet.Width);
                    t.SetAttributeValue("h", spriteSheet.Height);
                    t.SetAttributeValue("f", 1);
                    t.SetAttributeValue("min", 1);
                    t.SetAttributeValue("max", 1);
                    t.SetAttributeValue(NodeType.MergedByMod, $"{mod}");
                    parentTexturesCimNode.Add(t);
                }
                AssignCimFileID?.IncrementNormalized(1.0 / Build.Mods.Count);
            }
            AssignCimFileID?.Complete();
            Log.Debug($"{AssignCimFileID} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDir);

            CT.ThrowIfCancellationRequested();






            // Write Sprite Sheet files:
            Log.Debug($@"Writing sprite sheets to CIM files...", Paths.BuildTexturesDir);
            Clock.Restart();
            await Parallel.ForEachAsync(Build.Mods, ParallelOptions, async (mod, ct) =>
            {
                try
                {
                    foreach (SpriteSheetBuildData spriteSheet in mod.SpriteAtlas.SpriteSheets)
                    {
                        string cimFilename = $"{spriteSheet.GlobalId}.cim";

                        // Export to CIM to build stage directory:
                        await spriteSheet.TryExportToCimAsync(Path.Combine(Paths.BuildStageLibraryDir, cimFilename), Log, ct);

                        // Export to PNG, for debugging:
                        spriteSheet.TryExportToPng(Path.Combine(mod.TexturesDir, $"{spriteSheet.GlobalId}.png"), Log, ct);

                        // Add CIM:
                        lock (Build.ModsJsonFile.Mods)
                            Build.ModsJsonFile.Mods.FirstOrDefault(m => m.Name == mod.Name)?.Cim.Add(cimFilename);
                    }
                }
                finally
                {
                    WriteCimFiles?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            });
            if (BuildSettings.BuildFailure)
                return false;
            WriteCimFiles?.Complete();
            Log.Debug($"{WriteCimFiles} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDir);

            CT.ThrowIfCancellationRequested();






            // Assign a global region ID to each sprite, and add a corresponding XML node:
            Log.Debug($@"Assigning a global NAME and ID to each sprite...", Paths.BuildTexturesDir);
            Clock.Restart();

            int lastSpriteName =
                texturesXmlFile.GetNodes(NodeType.TexturesRegion)?
                .Select(node => node.Attribute(NodeType.TexturesRegion.NameAttribute)?.Value ?? string.Empty)
                .Max(strId => int.TryParse(strId, out int id) ? id : 0)
                ?? 0;

            XElement parentTexturesRegionNode = Build.XmlFile[EXmlFileType.Textures].GetParentNode(NodeType.TexturesRegion);
            foreach (ModBuildData mod in Build.Mods)
            {
                try
                {
                    foreach (SpriteBuildData sprite in mod.SpriteAtlas.Sprites.OrderBy(s => s.LocalId))
                    {
                        CT.ThrowIfCancellationRequested();

                        XElement re = new("re");
                        re.SetAttributeValue("n", sprite.GlobalName = (++lastSpriteName).ToString());
                        re.SetAttributeValue("t", sprite.Sheet.GlobalId);
                        re.SetAttributeValue("x", sprite.SpriteSheetX);
                        re.SetAttributeValue("y", sprite.SpriteSheetY);
                        re.SetAttributeValue("w", sprite.Width);
                        re.SetAttributeValue("h", sprite.Height);
                        re.SetAttributeValue("id", sprite.GlobalId = Build.AllocateNextNumericId(EIdPool.TexturesRegion));
                        re.SetAttributeValue("file", Path.GetFileName(sprite.FilePath ?? string.Empty));
                        re.SetAttributeValue(NodeType.MergedByMod, $"{mod}");
                        parentTexturesRegionNode.Add(re);
                    }
                }
                finally
                {
                    AssignTextureID?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            }

            // Write textures XML file to build stage directory:
            Log.Debug($@"Writing build-generated textures XML file...", Paths.BuildTexturesDir);
            if (!await texturesXmlFile.TrySaveAsync(Log, CT))
                return false;

            // Also write to build textures folder, for debugging:
            if (!await texturesXmlFile.TrySaveToAsync(Path.Combine(Paths.BuildTexturesDir, texturesXmlFile.FileName), Log, CT))
                return false;

            AssignTextureID?.Complete();
            Log.Debug($"{AssignTextureID} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDir);






            // Remap texture references in mod animations files:
            Log.Debug($@"Remapping animation texture references...");
            Clock.Restart();
            foreach (ModBuildData mod in Build.Mods)
            {
                try
                {
                    ILogger modLog = mod.Log;

                    foreach (XmlFile modXmlFile in mod.XmlFiles[EXmlFileType.Animations].Values)
                    {
                        foreach (XElement assetPos in modXmlFile.Xml.GetEveryAssetPos().Where(assetPos => assetPos.HasAttribute("filename")))
                        {
                            CT.ThrowIfCancellationRequested();

                            string spriteFilename = assetPos?.Attribute("filename")?.Value;
                            string spriteFileNameWithoutExtension = spriteFilename.RemoveSuffix(".png", StringComparison.OrdinalIgnoreCase);

                            if (spriteFileNameWithoutExtension.IsNullOrWhiteSpace())
                            {
                                modLog.Error($@"Found malformed texture reference at line {assetPos.Line()}, file ""{modXmlFile}""", modXmlFile.Path);
                                BuildSettings.Fail();
                                return false;
                            }

                            SpriteBuildData sprite = mod.SpriteAtlas.GetSpriteWithFileName(spriteFileNameWithoutExtension);
                            if (sprite == null)
                            {
                                modLog.Error($@"Missing texture file ""{spriteFileNameWithoutExtension}"" at line {assetPos.Line()}, file ""{modXmlFile}""", modXmlFile.Path);
                                BuildSettings.Fail();
                                return false;
                            }

                            // Sprites are referenced in the animations file by their name, not by their id:
                            assetPos.SetAttributeValue("a", sprite.GlobalName);

                            // This moves the 'filename' attribute to the end:
                            string filenameAttribute = assetPos.Attribute("filename")?.Value;
                            assetPos.RemoveAttribute("filename");
                            assetPos.SetAttributeValue("filename", filenameAttribute);
                        }

                        // Write mod's animations files, for debugging:
                        if (!await modXmlFile.TrySaveToAsync(Path.Combine(mod.TexturesDir, modXmlFile.FileName), modLog, CT))
                            return false;
                    }
                }
                finally
                {
                    MapAnimationToTexture?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            }
            MapAnimationToTexture?.Complete();
            Log.Debug($"{MapAnimationToTexture} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildTexturesDir);



            // Done.
            Log.Success($"TEXTURES files ready", Paths.BuildTexturesDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to merge TEXTURES: {ex}", Paths.BuildTexturesDir);
            return false;
        }
    }

    private async Task<bool> TryMergeXML()
    {
        try
        {
            Log.Info($@"Merging XML files: {XmlMergeFileTypes.Select(t => $"{t.ToString().ToLowerInvariant()}").JoinToString(", ")}...", Paths.BuildMergeDir);
            Clock.Restart();

            HashSet<XmlFile> mergedXmlFiles = [];

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

                    foreach (EXmlFileType xmlFileType in XmlMergeFileTypes)
                    {
                        // Any such files in mod?
                        XmlFile[] modXmlFiles = mod.XmlFiles[xmlFileType].Values.ToArray();
                        if (modXmlFiles.Length <= 0)
                            continue;

                        XmlFile xmlFile = Build.XmlFile[xmlFileType];
                        bool modified = false;

                        foreach (NodeType nodeType in NodeType.RegisteredTypes.Values.Where(n => n.XmlFileType == xmlFileType))
                        {
                            // Merge with all mod library XML files:
                            foreach (XmlFile modXmlFile in modXmlFiles)
                            {
                                CT.ThrowIfCancellationRequested();

                                // Get parent node:
                                XElement parentNode = xmlFile.GetParentNode(nodeType);
                                if (parentNode == null)
                                {
                                    modLog.Error($"Unable to find target parent node with xpath '{nodeType.ParentXPath}' for registered node type '{nodeType}'", xmlFile.Path);
                                    return false;
                                }

                                // List all patch nodes:
                                List<XElement> nodes = modXmlFile.GetNodes(nodeType)?.ToList() ?? [];
                                if (nodes.Count <= 0)
                                    continue;

                                modLog.Debug($@"Merging {nodes.Count} node(s) of type '{nodeType}' from file ""{modXmlFile}""", modXmlFile.Path);

                                mergedXmlFiles.Add(xmlFile);
                                modified = true;

                                foreach (XElement node in nodes)
                                {
                                    // Strip XML comments out:
                                    node.DescendantNodesAndSelf().OfType<XComment>().Remove();

                                    // Read id and name:
                                    string id = nodeType.IdAttribute != null ? node.Attribute(nodeType.IdAttribute)?.Value : null;
                                    string name = nodeType.NameAttribute != null ? node.Attribute(nodeType.NameAttribute)?.Value : null;
                                    string src = $"{modXmlFile.FileName}, line {node.Line()}";

                                    // Replace existing nodes with same id OR same name:
                                    HashSet<XElement> existingNodes = [];
                                    if (!nodeType.IdAttribute.IsNullOrWhiteSpace())
                                        existingNodes.AddRange(parentNode.Elements(node.Name)?.Where(n => n.Attribute(nodeType.IdAttribute)?.Value == id) ?? []);
                                    if (!nodeType.NameAttribute.IsNullOrWhiteSpace())
                                        existingNodes.AddRange(parentNode.Elements(node.Name)?.Where(n => n.Attribute(nodeType.NameAttribute)?.Value == name) ?? []);

                                    string prettyPath = $"path='{nodeType.XPath}'";

                                    string prettyNewId = id.IsNullOrWhiteSpace() ? null : $" {nodeType.IdAttribute}={id}";
                                    string prettyNewName = name.IsNullOrWhiteSpace() ? null : $" {nodeType.NameAttribute}='{name}'";
                                    string prettyNewMod = $" mod='{mod}'";
                                    string prettyNewSrc = $" src='{src}'";

                                    string prettyNewNode = $"new node [{prettyPath}{prettyNewId}{prettyNewName}{prettyNewMod}{prettyNewSrc}]";

                                    // Remove existing node:
                                    if (existingNodes.Count <= 0)
                                    {
                                        modLog.Debug($@"Adding {prettyNewNode}", modXmlFile.Path);
                                    }
                                    else
                                    {
                                        foreach (XElement existingNode in existingNodes)
                                        {
                                            CT.ThrowIfCancellationRequested();

                                            string existingId = nodeType.IdAttribute.IsNullOrWhiteSpace() ? null : existingNode.Attribute(nodeType.IdAttribute)?.Value;
                                            string existingName = nodeType.NameAttribute.IsNullOrWhiteSpace() ? null : existingNode.Attribute(nodeType.NameAttribute)?.Value;
                                            string existingMod = existingNode.Attribute(NodeType.MergedByMod)?.Value;
                                            if (existingMod.IsNullOrWhiteSpace()) existingMod = null;
                                            string existingSrc = existingNode.Attribute(NodeType.MergeSource)?.Value;
                                            if (existingMod == null || existingSrc.IsNullOrWhiteSpace()) existingSrc = null;

                                            string prettyExistingId = existingId == null ? null : $" {nodeType.IdAttribute}={existingId}";
                                            string prettyExistingName = existingName == null ? null : $" {nodeType.NameAttribute}='{existingName}'";
                                            string prettyExistingMod = existingMod == null ? null : $" mod='{existingMod}'";
                                            string prettyExistingSrc = existingSrc == null ? null : $" src='{existingSrc}'";

                                            string prettyExistingNode = $"existing node [{prettyPath}{prettyExistingId}{prettyExistingName}{prettyExistingMod}{prettyExistingSrc}]";

                                            if (existingMod == null)
                                                modLog.Debug($@"Replacing {prettyExistingNode} with {prettyNewNode}", modXmlFile.Path);
                                            else if (existingMod == mod.Name)
                                                modLog.Warn($@"Replacing {prettyExistingNode} with {prettyNewNode}. The node was modified by the same mod => This could be an ERROR", modXmlFile.Path);
                                            else
                                                modLog.Warn($@"Replacing {prettyExistingNode} with {prettyNewNode} => This is a potential MOD INCOMPATIBILITY", modXmlFile.Path);
                                            existingNode.Remove();
                                        }
                                    }

                                    // Add new node:
                                    node.SetAttributeValue(NodeType.MergedByMod, mod.Name);
                                    node.SetAttributeValue(NodeType.MergeSource, src);
                                    parentNode.Add(new XElement(node));
                                }
                            }
                        }

                        // Write merged XML to mod merge dir:
                        if (modified && !await xmlFile.TrySaveToAsync(Path.Combine(mod.MergeDir, xmlFile.FileName), Log, CT))
                            return false;
                    }
                }
                finally
                {
                    // Done with this mod.
                    MergeXmlFiles?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            }
            foreach (XmlFile xmlFile in mergedXmlFiles)
            {
                // Write merged XML files to build stage directory:
                if (!await xmlFile.TrySaveAsync(Log, CT))
                    return false;

                // Also write merged XML files to build merge directory:
                if (!await xmlFile.TrySaveToAsync(Path.Combine(Paths.BuildMergeDir, xmlFile.FileName), Log, CT))
                    return false;
            }

            // Done.
            MergeXmlFiles?.Complete();
            Log.Debug($"{MergeXmlFiles} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildMergeDir);
            Log.Success($"XML merge completed", Paths.BuildMergeDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to merge XML files: {ex}", Paths.BuildMergeDir);
            return false;
        }
    }

    public async Task<bool> TryPatchXML()
    {
        try
        {
            Log.Info($"Patching XML files...", Paths.BuildPatchDir);
            Clock.Restart();

            // Select mods with library XML files:
            foreach (ModBuildData mod in Build.Mods)
            {
                HashSet<XmlFile> modifiedFiles = [];
                try
                {
                    ILogger modLog = mod.Log;

                    if (!mod.HasPatchXml)
                    {
                        modLog.Debug($@"This mod has no XML patch files", mod.Directory);
                        continue;
                    }

                    modLog.Debug($"Performing XML patch operations...", mod.PatchDir);

                    // Create mod patch dir:
                    if (!await IOUtils.TryCreateDirectoryAsync(mod.PatchDir, modLog, CT))
                    {
                        modLog.Error($@"Unable to create directory: ""{mod.PatchDir}""", Paths.BuildPatchDir);
                        return false;
                    }

                    foreach (XmlFile modXmlFile in mod.XmlFiles[EXmlFileType.Patch].Values)
                    {
                        // Save intermediary modXmlFile:
                        if (!await modXmlFile.TrySaveToAsync(Path.Combine(mod.PatchDir, "mod", modXmlFile.FileName), modLog, CT))
                            return false;

                        // Get the target XML file:
                        if (!XmlFile.TryGetPatchXmlFileType(modXmlFile, out EXmlFileType targetXmlType))
                        {
                            modLog.Error($@"Unable to detect target XML file of patches in file ""{modXmlFile}""", modXmlFile.Path);
                            return false;
                        }
                        if (!Build.XmlFile.TryGetValue(targetXmlType, out XmlFile xmlFile))
                        {
                            modLog.Error($@"Unable to get target XML file of type '{targetXmlType}'", Paths.BuildStageDir);
                            return false;
                        }
                        modifiedFiles.Add(xmlFile);

                        // Start patching:
                        modLog.Debug($@"Executing patch operations from file ""{modXmlFile}"" to the {xmlFile.FileName} file...", modXmlFile.Path);

                        // Remove all XML comments from the mod file:
                        modXmlFile.Root.DescendantNodesAndSelf().OfType<XComment>().Remove();

                        // Iterate over patch nodes:
                        List<XElement> nodes = modXmlFile.Root.Nodes().Where(n => n is XElement).Cast<XElement>().ToList();
                        modLog.Debug($@"Processing {nodes.Count} patch nodes from ""{modXmlFile}""...", modXmlFile.Path);
                        foreach (XElement patchNode in nodes)
                        {
                            // Parse patch operation:
                            if (!XmlPatchOperation.TryCreate(modXmlFile, mod.Variables, patchNode, out XmlPatchOperation patch, modLog))
                                return false;

                            // Skip if disabled by patch logic:
                            if (!patch.IsEnabled)
                            {
                                modLog.Info($"Skipping DISABLED patch node\n{patch}", modXmlFile.Path);
                                continue;
                            }

                            // Validate XPATH unevaluated variables:
                            if (patch.XPath.ContainsAny('{', '}'))
                                modLog.Warn($"The evaluated XPATH '{patch.XPath}' could still contain undefined variables. \n{patch}", modXmlFile.Path);

                            // Execute XPATH:
                            if (!xmlFile.TryRunXPath(patch.XPath, out List<XElement> targetNodes, modLog))
                            {
                                modLog.Error($"Failed to execute the evaluated xpath='{patch.XPath}'. \n{patch}", modXmlFile.Path);
                                return false;
                            }

                            // No target nodes?
                            if (targetNodes.Count <= 0)
                            {
                                modLog.Warn($"The evaluated XPATH returned ZERO RESULTS => This could be an ERROR \n{patch}", modXmlFile.Path);
                                continue; // Nothing else to do...
                            }

                            // Too many target nodes?
                            if (targetNodes.Count >= 25)
                                modLog.Warn($"The evaluated XPATH is targeting {targetNodes.Count} NODES => This could be an ERROR \n{patch}", modXmlFile.Path);

                            // Perform patch operation:
                            if (!patch.TryRun(targetNodes, modLog))
                            {
                                modLog.Error($"Patch operation has FAILED. \n{patch}", modXmlFile.Path);
                                return false;
                            }

                            // Done with this patch operation.
                            modLog.Debug($"Patch operation applied to {targetNodes.Count} target node(s). \n{patch}", modXmlFile.Path);
                        }
                    }

                    // Write target XML files modified by this mod, for debugging:
                    foreach (XmlFile xmlFile in modifiedFiles)
                        if (!await xmlFile.TrySaveToAsync(Path.Combine(mod.PatchDir, "result", xmlFile.FileName), Log, CT))
                            return false;
                }
                finally
                {
                    PatchXmlFiles?.IncrementNormalized(1.0 / Build.Mods.Count);
                }
            }

            // Done.
            PatchXmlFiles?.Complete();
            Log.Debug($"{PatchXmlFiles} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.BuildPatchDir);
            Log.Success($"XML patches completed", Paths.BuildPatchDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to patch XML files: {ex}", Paths.BuildPatchDir);
            return false;
        }
    }

    private async Task<bool> TryCreateSpaceHavenJarFile()
    {
        try
        {
            Log.Info($"Composing '{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' file...", Paths.CacheDir);
            Clock.Restart();

            // Clear target directory:
            if (!await IOUtils.TryDeleteDirectoryAsync(Paths.CacheDir, Log, CT))
                return false;
            if (!await IOUtils.TryCreateDirectoryAsync(Paths.CacheDir, Log, CT))
                return false;

            BuildJarFile.SetNormalized(0.10);

            // Make sure all XML files are saved:
            foreach (XmlFile xmlFile in Build.XmlFile.Values)
                if (!await xmlFile.TrySaveAsync(Log, CT))
                    return false;

            // Select files to add to template JAR:
            DirectoryInfo di = new(Paths.BuildStageDir);
            FileInfo[] files = di.GetFiles("*.*", SearchOption.AllDirectories);
            JarAppender jar = new();
            if (!await jar.AppendTo(Paths.TemplateJarPath, Paths.CacheJarPath, Paths.BuildStageDir, files, Log, ParallelOptions))
                return false;

            BuildJarFile.SetNormalized(0.85);

            // calculate hash:
            string hash = await XxHash64Calculator.ComputeFromFileAsync(Paths.CacheJarPath, Log, CT);
            if (!await IOUtils.TryWriteAllTextAsync(Paths.CacheModifiedJarHashPath, hash, Log, CT))
                return false;
            BuildJarFile.SetNormalized(0.90);

            // Copy original JAR hash file:
            if (!await IOUtils.TryCopyFileAsync(Paths.TemplateJarHashPath, Paths.BuildJarHashPath, true, Log, CT))
                return false;
            BuildJarFile.SetNormalized(0.95);

            if (!await IOUtils.TryCopyFileAsync(Paths.BuildJarHashPath, Paths.CacheJarHashPath, true, Log, CT))
                return false;
            BuildJarFile?.Complete();

            // Done.
            Log.Debug($"{BuildJarFile} ({(int)Clock.Elapsed.TotalMilliseconds} ms)", Paths.CacheDir);
            Log.Success($"'{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' is ready", Paths.CacheDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compose final '{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}' file: {ex}", Paths.CacheDir);
            return false;
        }
    }

    private async Task<bool> TryCreateConfigJson()
    {
        try
        {
            Log.Info($"Creating {SpaceHavenConstants.CONFIG_JSON}...", Paths.CacheDir);
            Clock.Restart();

            // Create modified config.json:
            ConfigJsonFile config = ConfigJsonFile.GetOriginal();
            if (Build.HasXmlMods)
            {
                config.ClassPath.Remove(SpaceHavenConstants.SPACEHAVEN_JAR);
                config.ClassPath.Add(ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
            }

            // Add JARs from mods:
            if (Build.HasJavaMods)
            {
                List<ModBuildData> mods = Build.Mods.Where(mod => mod.HasJava).ToList();
                PrepareJavaFiles.Max = mods.Count;

                // Adjust vmArgs:
                config.VMArgs.Insert(config.VMArgs.Count - 1, $"-javaagent:{Path.Combine(Paths.SpaceHavenJarDir, ModdingConstants.ASPECTJWEAVER).AsStandardPath()}");

                // Add JARs to classPath:
                config.ClassPath.Insert(0, ModdingConstants.ASPECTJWEAVER);
                config.ClassPath.Insert(1, ModdingConstants.ASPECTJ);
                foreach (ModBuildData mod in mods)
                {
                    CT.ThrowIfCancellationRequested();
                    try
                    {
                        ILogger modLog = mod.Log;
                        foreach (string path in mod.JavaFilePaths.Where(path => path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)).OrderBy(path => path))
                        {
                            modLog.Debug($"Adding JAR file to classPath: {path}", mod.Directory);
                            config.ClassPath.Insert(config.ClassPath.Count - 1, path.AsStandardPath());
                        }
                    }
                    finally
                    {
                        // Done with this mod.
                        PrepareJavaFiles.Increment();
                    }
                }
            }

            // Write config.json to cache directory:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.CacheConfigJsonPath, config.ToJsonString(), Log, CT))
                return false;

            // Write JAVA hash file:
            if (!await IOUtils.TryCopyFileAsync(Paths.BuildJavaHashPath, Paths.CacheJavaHashPath, true, Log, CT))
                return false;

            // Done.
            PrepareJavaFiles.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log?.Error($"Unable to create {SpaceHavenConstants.CONFIG_JSON}: {ex}", Paths.CacheDir);
            return false;
        }
    }

    private async Task<bool> TryCreatModsJson()
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
            Log?.Error($"Unable to create {SpaceHavenConstants.CONFIG_JSON}: {ex}", Paths.CacheDir);
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

    public override string ToString() => "Build";
}
