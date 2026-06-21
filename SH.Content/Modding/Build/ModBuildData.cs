using SH.Content.Xml;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Content.Modding.Build;

internal sealed class ModBuildData : IAsyncDisposable
{
    public ModBuildData(BuildSettings buildSettings, int buildSeqNum, ModData mod, BuildData build, ILogger logger)
    {
        Settings = buildSettings ?? throw new ArgumentNullException(nameof(buildSettings));
        Data = mod ?? throw new ArgumentNullException(nameof(mod));
        Build = build ?? throw new ArgumentNullException(nameof(build));
        BuildSeqNum = buildSeqNum;
        FullFileLogger = new FileLogger(FullLogPath);
        ErrorFileLogger = new FileLogger(ErrorLogPath);
        ErrorFileLogger.SetLogLevel(ELogLevel.Warn);
        Log = new LoggerCollection(logger, FullFileLogger, ErrorFileLogger) { Prefix = $"[{Name}] " };
        SpriteAtlas = new(mod.Name);
    }

    private readonly BuildSettings Settings;
    private ParallelOptions ParallelOptions => Settings.ParallelOptions;
    private CancellationToken CT => Settings.CT;

    public string Name => Data.Name;
    public string Version => Data.Name;
    public string Directory => Data.Directory;
    public int ID => Data.ID;

    public string AudioDirectory => Data.AudioDirectory;
    public string TexturesDirectory => Data.TexturesDirectory;
    public string XmlLibraryDirectory => Data.XmlLibraryDirectory;
    public string XmlPatchesDirectory => Data.XmlPatchesDirectory;

    public IReadOnlyList<string> AudioFilePaths => Data.AudioFilePaths;
    public IReadOnlyList<string> TextureFilePaths => Data.TextureFilePaths;
    public IReadOnlyList<string> XmlLibraryFilePaths => Data.XmlLibraryFilePaths;
    public IReadOnlyList<string> XmlPatchFilePaths => Data.XmlPatchFilePaths;
    public IReadOnlyList<string> JavaFilePaths => Data.JavaFilePaths;
    public IReadOnlyList<string> OtherFilePaths => Data.OtherFilePaths;

    public string BuildName => $"[{BuildSeqNum}] {Data.Name}";
    public int BuildSeqNum { get; }

    private ModData Data { get; }
    public ILogger Log { get; }
    private FileLogger FullFileLogger { get; }
    private FileLogger ErrorFileLogger { get; }
    public BuildPathData Paths => Build.Paths;
    public BuildData Build { get; }
    public SpriteAtlasBuildData SpriteAtlas { get; }
    public SortedDictionary<string, AudioBuildData> Audio { get; } = [];
    public SortedDictionary<string, VarBuildData> Variables { get; } = [];

    public bool IsXmlMod => HasAudio || HasTextures || HasLibraryXml || HasPatchXml;
    public bool IsJavaMod => HasJava;

    public bool HasAudio => Data.AudioFilePaths.Count > 0;
    public bool HasTextures => Data.TextureFilePaths.Count > 0;
    public bool HasLibraryXml => Data.XmlLibraryFilePaths.Count > 0;
    public bool HasPatchXml => Data.XmlPatchFilePaths.Count > 0;
    public bool HasJava => Data.JavaFilePaths.Count > 0;

    public string FullLogPath => Path.Combine(Paths.BuildLogsDir, $"{BuildName} (full log).txt");
    public string ErrorLogPath => Path.Combine(Paths.BuildLogsDir, $"{BuildName} (error log).txt");
    public string AudioDir => Path.Combine(Paths.BuildAudioDir, BuildName);
    public string TexturesDir => Path.Combine(Paths.BuildTexturesDir, BuildName);
    public string MergeDir => Path.Combine(Paths.BuildMergeDir, BuildName);
    public string PatchDir => Path.Combine(Paths.BuildPatchDir, BuildName);

    public SortedDictionary<EXmlFileType, SortedDictionary<string, XmlFile>> XmlFiles { get; } = new()
    {
        [EXmlFileType.Patch] = new(),
        [EXmlFileType.Haven] = new(),
        [EXmlFileType.Texts] = new(),
        [EXmlFileType.Audio] = new(),
        [EXmlFileType.Textures] = new(),
        [EXmlFileType.Animations] = new(),
        [EXmlFileType.SpaceHavenSettings] = new(),
    };

    // Merge:
    public string MergedHavenXmlPath => Path.Combine(MergeDir, $"{SpaceHavenConstants.HAVEN}.xml");
    public string MergedTextsXmlPath => Path.Combine(MergeDir, $"{SpaceHavenConstants.TEXTS}.xml");
    public string MergedAudioXmlPath => Path.Combine(MergeDir, $"{SpaceHavenConstants.AUDIO}.xml");
    public string MergedTexturesXmlPath => Path.Combine(MergeDir, $"{SpaceHavenConstants.TEXTURES}.xml");
    public string MergedAnimationsXmlPath => Path.Combine(MergeDir, $"{SpaceHavenConstants.ANIMATIONS}.xml");
    public string MergedSpaceHavenSettingsXmlPath => Path.Combine(MergeDir, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);

    // Patch:
    public string PatchedHavenXmlPath => Path.Combine(PatchDir, $"{SpaceHavenConstants.HAVEN}.xml");
    public string PatchedTextsXmlPath => Path.Combine(PatchDir, $"{SpaceHavenConstants.TEXTS}.xml");
    public string PatchedAudioXmlPath => Path.Combine(PatchDir, $"{SpaceHavenConstants.AUDIO}.xml");
    public string PatchedTexturesXmlPath => Path.Combine(PatchDir, $"{SpaceHavenConstants.TEXTURES}.xml");
    public string PatchedAnimationsXmlPath => Path.Combine(PatchDir, $"{SpaceHavenConstants.ANIMATIONS}.xml");
    public string PatchedSpaceHavenSettingsXmlPath => Path.Combine(PatchDir, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);

    public string XmlHash { get; private set; } = string.Empty;
    public IReadOnlyDictionary<string, string> XmlHashes { get; private set; } = new SortedDictionary<string, string>();

    public string JavaHash { get; private set; } = string.Empty;
    public IReadOnlyDictionary<string, string> JavaHashes { get; private set; } = new SortedDictionary<string, string>();


    public async Task<bool> ComputeHash()
    {
        try
        {
            Log.Debug("Computing HASH...");

            // info.xml:
            // - full file content hash
            string infoXmlHash = await XxHash64Calculator.ComputeFromFileAsync(Data.InfoXmlPath, Log, CT);

            // --- XML MOD ONLY ---
            if (IsXmlMod)
            {
                SortedDictionary<string, string> xmlHashes = new();
                XmlHashes = xmlHashes;

                // MOD ID:
                xmlHashes[$"ID[{nameof(ModData.CustomId)}]"] = Data.CustomId.ToString();

                // info.xml:
                xmlHashes[$"InfoFile[{ModdingConstants.INFO_XML}]"] = infoXmlHash;

                // Hash of all variable values:
                string variables = Variables.Values?.OrderBy(v => v.Name).JoinToString(v => $@"{v.Name}={v.StrValue}", "\n") ?? string.Empty;
                xmlHashes["Vars"] = XxHash64Calculator.ComputeFromString(variables, Log) ?? string.Empty;

                CT.ThrowIfCancellationRequested();

                // XML files:
                // - full file content hash
                List<string> xmlFilesPath = [];
                xmlFilesPath.AddRange(XmlLibraryFilePaths);
                xmlFilesPath.AddRange(XmlPatchFilePaths);
                xmlFilesPath.Sort();
                foreach (string path in xmlFilesPath)
                {
                    string relativePath = path.Substring(Directory.Length + 1);
                    xmlHashes[$@"XmlFile:{relativePath}"""] = await XxHash64Calculator.ComputeFromFileAsync(path, Log, CT);
                }

                CT.ThrowIfCancellationRequested();

                // Remaining files: Calculate approximate hash from:
                // - file size
                // - last modified time
                // - a few bytes from file content
                byte[] buffer = new byte[4096];
                List<string> resourceFilePaths = [];
                resourceFilePaths.AddRange(AudioFilePaths);
                resourceFilePaths.AddRange(TextureFilePaths);
                resourceFilePaths.Sort();
                foreach (string path in resourceFilePaths)
                {
                    FileInfo fi = new(path);
                    Array.Clear(buffer, 0, buffer.Length);
                    BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(0, 8), fi.Length);
                    BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8, 8), fi.LastWriteTimeUtc.Ticks);
                    string relativePath = path.Substring(Directory.Length + 1);
                    await IOUtils.TryReadFirstBytesAsync(path, 16, buffer, Log);
                    xmlHashes[$@"ResourceFile:{relativePath}"""] = XxHash64Calculator.ComputeFromBytes(buffer, Log);
                }

                CT.ThrowIfCancellationRequested();

                // Overall XML Hash:
                string allXmlHashesStr = xmlHashes.JoinToString((kvp) => $"{kvp.Key}={kvp.Value}", "\n");
                XmlHash = XxHash64Calculator.ComputeFromString(allXmlHashesStr, Log) ?? string.Empty;
            }


            // --- JAVA MOD ONLY ---
            if (IsJavaMod)
            {
                SortedDictionary<string, string> javaHashes = new();
                JavaHashes = javaHashes;

                // MOD ID:
                javaHashes[$"ID[{nameof(ModData.CustomId)}]"] = Data.CustomId.ToString();

                // info.xml:
                javaHashes[$"InfoFile[{ModdingConstants.INFO_XML}]"] = infoXmlHash;

                // JAVA files:
                foreach (string path in JavaFilePaths)
                {
                    string relativePath = path.Substring(Directory.Length + 1);
                    javaHashes[$"JavaFile[{relativePath}]"] = await XxHash64Calculator.ComputeFromFileAsync(path, Log, CT) ?? string.Empty;
                }

                // Other files:
                foreach (string path in OtherFilePaths)
                {
                    string relativePath = path.Substring(Directory.Length + 1);
                    javaHashes[$"OtherFile[{relativePath}]"] = await XxHash64Calculator.ComputeFromFileAsync(path, Log, CT) ?? string.Empty;
                }

                // Overall JAVA Hash:
                string allJavaHashesStr = javaHashes.JoinToString((kvp) => $"{kvp.Key}={kvp.Value}", "\n");
                JavaHash = XxHash64Calculator.ComputeFromString(allJavaHashesStr, Log) ?? string.Empty;
            }


            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compute hash: {ex}");

            // In case of exception, always set a new unique hash value:
            if (XmlHash.IsNullOrEmpty())
                XmlHash = XxHash64Calculator.ComputeFromString(DateTime.Now.Ticks.ToString(), Log);
            if (JavaHash.IsNullOrEmpty())
                JavaHash = XxHash64Calculator.ComputeFromString(DateTime.Now.Ticks.ToString(), Log);

            return false;
        }
    }
    public async Task<bool> TryLoadXmlFiles()
    {
        try
        {
            Log.Debug($"Loading XML files with evaluated variable values...", Directory);

            // Library:
            foreach (string path in Data.XmlLibraryFilePaths.OrderBy(path => path))
            {
                CT.ThrowIfCancellationRequested();

                if (!XmlFile.TryGetLibraryXmlFileType(path, out EXmlFileType xmlFileType))
                {
                    Log.Error($@"Unknown target XML file for ""{path}"". Hint: Check for case-sensitive XML tags", path);
                    return false;
                }

                if (!XmlFiles.TryGetValue(xmlFileType, out SortedDictionary<string, XmlFile> dict))
                    XmlFiles[xmlFileType] = dict = new();

                CT.ThrowIfCancellationRequested();

                XmlFile file = dict[path] = new XmlFile(xmlFileType, path);
                if (!await TryLoadWithEvaluatedVariablesAsync(file, Data.ModId, Data.AutoId, Data.CustomId, Variables, Log, CT))
                {
                    Log.Error($@"This XML file contains a SYNTAX ERROR and could not be parsed ""{path}""", path);
                    return false;
                }
            }

            // Patch:
            SortedDictionary<string, XmlFile> patchDict = new();
            XmlFiles[EXmlFileType.Patch] = patchDict;
            foreach (string path in Data.XmlPatchFilePaths.OrderBy(path => path))
            {
                CT.ThrowIfCancellationRequested();

                XmlFile file = patchDict[path] = new XmlFile(EXmlFileType.Patch, path);
                if (!await TryLoadWithEvaluatedVariablesAsync(file, Data.ModId, Data.AutoId, Data.CustomId, Variables, Log, CT))
                {
                    Log.Error($@"This XML file contains a SYNTAX ERROR and could not be parsed ""{path}""", path);
                    return false;
                }
            }

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"{ex}");
            return false;
        }
    }
    public async Task<bool> TryMapVariables()
    {
        try
        {
            Log.Debug($"Mapping variable names to values...", Data.InfoXmlPath);
            foreach (VarData v in Data.Variables.Where(v => !v.IsSeparator))
            {
                CT.ThrowIfCancellationRequested();

                VarBuildData variable = new(v);
                if (Variables.ContainsKey(v.Name))
                {
                    Log.Warn($"Skipping duplicate variable '{v.Name}' => This could be an ERROR", Data.InfoXmlPath);
                    continue;
                }
                Variables[variable.Name] = variable;
            }
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to map mod variables: {ex}");
            return false;
        }
    }



    public async Task<bool> TryLoadWithEvaluatedVariablesAsync(XmlFile xmlFile, int modID, int autoID, int customID, IReadOnlyDictionary<string, VarBuildData> variables, ILogger logger, CancellationToken ct)
    {
        try
        {
            logger?.Debug($@"Loading evaluated XML document: ""{xmlFile.Path}""", xmlFile.Path);

            if (xmlFile.Path.IsNullOrWhiteSpace() || !File.Exists(xmlFile.Path))
            {
                logger?.Error($@"XML document could not be found at ""{xmlFile.Path}""", xmlFile.Path);
                return false;
            }
            string content = await File.ReadAllTextAsync(xmlFile.Path, ct);
            OrderedDictionary<string, string> replacements = new(StringComparer.OrdinalIgnoreCase);

            // Replacements using the reserved variable '{id}'
            if (modID == 0)
            {
                if (customID == autoID || customID == 0)
                {
                    // Replace {id} with generated autoID
                    logger?.Info($@"Replacing '{ModdingConstants.BracedIdVariable}' with AUTO-ID='{autoID}' in XML file ""{xmlFile.FileName}"", Path");
                    replacements[ModdingConstants.BracedIdVariable] = autoID.ToString(); // i.e. replace {id} with autoID
                }
                else
                {
                    // Replace {id} with adjusted customID
                    logger?.Info($@"Replacing '{ModdingConstants.BracedIdVariable}' with CUSTOM-ID='{customID}' in XML file ""{xmlFile.FileName}""", xmlFile.Path);
                    replacements[ModdingConstants.BracedIdVariable] = customID.ToString(); // i.e. replace {id} with customID
                }
            }
            else // modID == 0
            {
                if (customID == modID || customID == 0)
                {
                    // Replace {id} with declared modID
                    logger?.Info($@"Replacing '{ModdingConstants.BracedIdVariable}' with MOD-ID='{modID}' in XML file ""{xmlFile.FileName}""", xmlFile.Path);
                    replacements[ModdingConstants.BracedIdVariable] = $@"{modID}"; // i.e. replace {id} with modID
                }
                else
                {
                    // Replace {id} with adjusted customID
                    logger?.Info($@"Replacing '{ModdingConstants.BracedIdVariable}' with CUSTOM-ID='{customID}' in XML file ""{xmlFile.FileName}""", xmlFile.Path);
                    replacements[ModdingConstants.BracedIdVariable] = customID.ToString(); // i.e. replace {id} with customID
                }
            }

            // Replace numeric "modID with numeric "customID
            if (modID != 0 && customID != modID && customID != 0)
            {
                logger?.Info($@"Replacing MOD-ID='{modID}' with CUSTOM-ID='{customID}' in XML file ""{xmlFile.FileName}""", xmlFile.Path);
                content = content.Replace('"' + modID.ToString(), '"' + customID.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            // Replace mod variables with their respective values:
            foreach (VarBuildData v in variables.Values)
                replacements[v.BracedName] = v.StrValue;

            // Execute replacements in one pass using REGEX:
            Regex regex = new(@"\{[^{}]+\}", RegexOptions.IgnoreCase);
            content = regex.Replace(content, match => replacements.TryGetValue(match.Value, out string value) ? value : match.Value);

            ct.ThrowIfCancellationRequested();

            // Reload XML document after all replacements:
            xmlFile.Xml = XDocument.Parse(content, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
            return xmlFile.Xml != null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error(ex, xmlFile.Path);
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
        foreach (SpriteBuildData sprite in SpriteAtlas.Sprites ?? [])
            try { await sprite.DisposeAsync(); } catch { }
        try { await FullFileLogger.DisposeAsync(); } catch { }
        try { await ErrorFileLogger.DisposeAsync(); } catch { }
    }
    #endregion

    public override string ToString() => Data.Name;
}
