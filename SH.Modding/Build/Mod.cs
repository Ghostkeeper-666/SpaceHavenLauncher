using SH.Content.Xml;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Memory;
using SH.Framework.Tasks;
using SH.Modding.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Build;

internal sealed class Mod : IAsyncDisposable
{
    public Mod(BuildSettings buildSettings, int buildSeqNum, ModData mod, ILogger log)
    {
        BuildSettings = buildSettings ?? throw new ArgumentNullException(nameof(buildSettings));
        Data = mod ?? throw new ArgumentNullException(nameof(mod));
        BuildSeqNum = buildSeqNum;
        FullFileLogger = new FileLogger(FullLogPath);
        ErrorFileLogger = new FileLogger(ErrorLogPath);
        ErrorFileLogger.SetLogLevel(ELogLevel.Warn);
        Log = new LoggerCollection(log, FullFileLogger, ErrorFileLogger) { Prefix = $"[{UniqueName}] " };
        Log.Replacements = log?.Replacements?.ToArray(); // clone
    }

    private BuildSettings BuildSettings;
    private PathData Paths => BuildSettings.Paths;
    private CancellationToken CT => BuildSettings.CT;

    public string UniqueName => Data.UniqueName;
    public VersionInfo Version => Data.Version;
    public string Author => Data.Author;
    public string Dir => Data.Dir;
    public string InfoXmlPath => Data.InfoXmlPath;
    public int ID => Data.ID;

    public string AudioDir => Data.AudioDir;
    public string SpritesDir => Data.SpritesDir;
    public string SpriteSheetsDir => Data.SpriteSheetsDir;
    public string XmlLibraryDir => Data.XmlLibraryDir;
    public string XmlPatchesDir => Data.XmlPatchesDir;

    // Mod Absolute Paths:
    public IReadOnlyList<string> AudioPaths => Data.AudioPaths;
    public IReadOnlyList<string> SpritePaths => Data.SpritePaths;
    public IReadOnlyList<string> SpriteSheetPaths => Data.SpriteSheetPaths;
    public IReadOnlyList<string> XmlLibraryPaths => Data.XmlLibraryPaths;
    public IReadOnlyList<string> XmlPatchPaths => Data.XmlPatchPaths;
    public IReadOnlyList<string> JarFilePaths => Data.JarPaths;
    public IReadOnlyList<string> OtherFilesPaths => Data.OtherFilePaths;

    // Mod Relative Paths:
    public IReadOnlyList<string> AudioRelativePaths => Data.AudioRelativePaths;
    public IReadOnlyList<string> SpriteRelativePaths => Data.SpriteRelativePaths;
    public IReadOnlyList<string> SpriteSheetRelativePaths => Data.SpriteSheetRelativePaths;
    public IReadOnlyList<string> XmlLibraryRelativePaths => Data.XmlLibraryRelativePaths;
    public IReadOnlyList<string> XmlPatchRelativePaths => Data.XmlPatchRelativePaths;
    public IReadOnlyList<string> JarRelativePaths => Data.JarRelativePaths;
    public IReadOnlyList<string> OtherFilesRelativePaths => Data.OtherFilesRelativePaths;


    public string BuildName => $"[{BuildSeqNum}] {Data.UniqueName}";
    public int BuildSeqNum { get; }

    private ModData Data;
    public ILogger Log { get; private set; }
    private FileLogger FullFileLogger;
    private FileLogger ErrorFileLogger;
    public SortedDictionary<string, Var> Variables { get; private set; } = [];
    public SortedDictionary<string, Audio> Audio { get; private set; } = [];

    public SortedDictionary<EXmlFileType, SortedDictionary<string, XmlFile>> LibraryXmlFiles { get; } = new()
    {
        [EXmlFileType.Haven] = new(),
        [EXmlFileType.Texts] = new(),
        [EXmlFileType.Audio] = new(),
        [EXmlFileType.Textures] = new(),
        [EXmlFileType.Animations] = new(),
        [EXmlFileType.SpaceHavenSettings] = new(),
    };

    public SortedDictionary<EXmlFileType, SortedDictionary<string, XmlFile>> PatchXmlFiles { get; } = new()
    {
        [EXmlFileType.Haven] = new(),
        [EXmlFileType.Texts] = new(),
        [EXmlFileType.Audio] = new(),
        [EXmlFileType.Textures] = new(),
        [EXmlFileType.Animations] = new(),
        [EXmlFileType.SpaceHavenSettings] = new(),
    };

    public bool IsXmlMod => HasAudio || HasSprites || HasSpriteSheets || HasLibraryXml || HasPatchXml;
    public bool IsJavaMod => HasJar;

    public bool HasAudio => Data.HasAudio;
    public bool HasSprites => Data.HasSprites;
    public bool HasSpriteSheets => Data.HasSpriteSheets;
    public bool HasLibraryXml => Data.HasLibraryXml;
    public bool HasPatchXml => Data.HasPatchXml;
    public bool HasJar => Data.HasJar;

    // Mod Build Paths:
    public string FullLogPath => IOUtils.CombineAsOSPath(Paths.BuildLogsDir, $"{BuildName} (full log).txt");
    public string ErrorLogPath => IOUtils.CombineAsOSPath(Paths.BuildLogsDir, $"{BuildName} (error log).txt");
    public string BuildAudioDir => IOUtils.CombineAsOSPath(Paths.BuildAudioDir, BuildName);
    public string BuildTexturesDir => IOUtils.CombineAsOSPath(Paths.BuildTexturesDir, BuildName);
    public string BuildMergeDir => IOUtils.CombineAsOSPath(Paths.BuildMergeDir, BuildName);
    public string BuildPatchDir => IOUtils.CombineAsOSPath(Paths.BuildPatchDir, BuildName);

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

                // XML files: compute hash of full file content
                List<string> xmlFilesPath = [];
                xmlFilesPath.AddRange(XmlLibraryPaths);
                xmlFilesPath.AddRange(XmlPatchPaths);
                xmlFilesPath.Sort();
                foreach (string path in xmlFilesPath)
                {
                    if (!IOUtils.FileExists(path))
                        continue;
                    string relativePath = path.Substring(Dir.Length + 1);
                    xmlHashes[$@"XmlFile:{relativePath}"""] = await XxHash64Calculator.ComputeFromFileAsync(path, Log, CT);
                }

                CT.ThrowIfCancellationRequested();

                // Remaining files: Calculate approximate hash from:
                // - file size
                // - last modified time
                // - a few bytes from content
                List<string> resourceFilePaths = [];
                resourceFilePaths.AddRange(AudioPaths);
                resourceFilePaths.AddRange(SpritePaths);
                resourceFilePaths.Sort();

                using (ArrayPool<byte> arrayPool = new(1024, 32))
                {
                    await Parallel.ForEachAsync(resourceFilePaths, BuildSettings.ParallelOptions, async (path, ct) =>
                    {
                        if (!IOUtils.FileExists(path))
                            return;
                        byte[] buffer = arrayPool.Get();

                        if (!path.TryGetFileInfo(out long size, out DateTime lastWriteTime))
                            return; // file not exists

                        Array.Clear(buffer, 0, buffer.Length);
                        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(0, 8), size);
                        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8, 8), lastWriteTime.Ticks);
                        string relativePath = path.Substring(Dir.Length + 1);
                        await IOUtils.TryReadFirstBytesAsync(path, 16, buffer, Log, ct);
                        lock (xmlHashes)
                            xmlHashes[$@"ResourceFile:{relativePath}"""] = XxHash64Calculator.ComputeFromBytes(buffer, Log);
                        arrayPool.Return(buffer);
                    });
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
                foreach (string path in JarFilePaths)
                {
                    string relativePath = path.Substring(Dir.Length + 1);
                    javaHashes[$"JavaFile[{relativePath}]"] = await XxHash64Calculator.ComputeFromFileAsync(path, Log, CT) ?? string.Empty;
                }

                // Other files:
                foreach (string path in OtherFilesPaths)
                {
                    string relativePath = path.Substring(Dir.Length + 1);
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

    private readonly IReadOnlyDictionary<EXmlFileType, SemaphoreSlim> LibraryXmlFileSemaphores = new Dictionary<EXmlFileType, SemaphoreSlim>()
    {
        [EXmlFileType.Haven] = new(1, 1),
        [EXmlFileType.Texts] = new(1, 1),
        [EXmlFileType.Audio] = new(1, 1),
        [EXmlFileType.Textures] = new(1, 1),
        [EXmlFileType.Animations] = new(1, 1),
        [EXmlFileType.SpaceHavenSettings] = new(1, 1),
    };

    public async Task<bool> TryLoadLibraryXmlFilesAsync(EXmlFileType targetXmlFileType)
    {
        // Should never happen if targetXmlFileType is a valid value:
        if (!LibraryXmlFileSemaphores.TryGetValue(targetXmlFileType, out SemaphoreSlim semaphore))
            throw new NotImplementedException($"{nameof(EXmlFileType)} = {targetXmlFileType}");

        await semaphore.WaitAsync(BuildSettings.CT);
        try
        {
            Log.Debug($"Loading LIBRARY XML files of type '{targetXmlFileType}'...", Dir);

            if (!LibraryXmlFiles.TryGetValue(targetXmlFileType, out SortedDictionary<string, XmlFile> dict))
                throw new NotImplementedException($"{nameof(EXmlFileType)} = {targetXmlFileType}");

            string[] paths =
                Data.XmlLibraryPaths
                .Where(path => XmlFile.GetLibraryXmlFileType(path) == targetXmlFileType)
                .OrderBy(path => path.ToLowerInvariant())
                .ToArray();

            await Parallel.ForEachAsync(paths, BuildSettings.ParallelOptions, async (path, ct) =>
            {
                XmlFile xmlFile = new(targetXmlFileType, Data.XmlLibraryDir, path);

                if (!await TryLoadWithEvaluatedVariablesAsync(xmlFile, Data.ModId, Data.AutoId, Data.CustomId, Variables, Log, CT))
                    throw new StopException($@"The LIBRARY XML file contains a SYNTAX ERROR and could not be parsed ""{path}""", path, BuildSettings.InternalCTS);

                string evaluatedPath = IOUtils.CombineAsOSPath(BuildMergeDir, "mod", xmlFile.RelativePath);
                if (!await xmlFile.TrySaveToAsync(evaluatedPath, Log, CT))
                    throw new StopException($@"Unable to write evaluated LIBRARY XML file ""{evaluatedPath}""", BuildPatchDir, BuildSettings.InternalCTS);

                if (!LibraryXmlFiles.TryGetValue(targetXmlFileType, out SortedDictionary<string, XmlFile> dict))
                    throw new NotImplementedException($"{nameof(EXmlFileType)} = {targetXmlFileType}");

                lock (dict)
                    dict[path] = xmlFile;
            });

            // Done.
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
            Log.Error(ex, Dir);
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private readonly IReadOnlyDictionary<EXmlFileType, SemaphoreSlim> PatchXmlFileSemaphores = new Dictionary<EXmlFileType, SemaphoreSlim>()
    {
        [EXmlFileType.Haven] = new(1, 1),
        [EXmlFileType.Texts] = new(1, 1),
        [EXmlFileType.Audio] = new(1, 1),
        [EXmlFileType.Textures] = new(1, 1),
        [EXmlFileType.Animations] = new(1, 1),
        [EXmlFileType.SpaceHavenSettings] = new(1, 1),
    };

    public async Task<bool> TryLoadPatchXmlFilesAsync(EXmlFileType targetXmlFileType)
    {
        // Should never happen if targetXmlFileType is a valid value:
        if (!PatchXmlFileSemaphores.TryGetValue(targetXmlFileType, out SemaphoreSlim semaphore))
            throw new NotImplementedException($"{nameof(EXmlFileType)} = {targetXmlFileType}");

        await semaphore.WaitAsync(BuildSettings.CT);
        try
        {
            Log.Debug($"Loading PATCH XML files of type '{targetXmlFileType}'...", Dir);

            if (!PatchXmlFiles.TryGetValue(targetXmlFileType, out SortedDictionary<string, XmlFile> dict))
                throw new NotImplementedException($"{nameof(EXmlFileType)} = {targetXmlFileType}");

            string[] paths =
                Data.XmlPatchPaths
                .Where(path => XmlFile.GetPatchXmlFileType(path) == targetXmlFileType)
                .OrderBy(path => path.ToLowerInvariant())
                .ToArray();

            await Parallel.ForEachAsync(paths, BuildSettings.ParallelOptions, async (path, ct) =>
            {
                CT.ThrowIfCancellationRequested();

                XmlFile xmlFile = new(EXmlFileType.Patch, Data.XmlPatchesDir, path)
                {
                    PatchType = targetXmlFileType,
                };
                if (!await TryLoadWithEvaluatedVariablesAsync(xmlFile, Data.ModId, Data.AutoId, Data.CustomId, Variables, Log, CT))
                    throw new StopException($@"The PATCH XML file contains a SYNTAX ERROR and could not be parsed ""{path}""", path, BuildSettings.InternalCTS);

                string evaluatedPath = IOUtils.CombineAsOSPath(BuildPatchDir, "mod", xmlFile.RelativePath);
                if (!await xmlFile.TrySaveToAsync(evaluatedPath, Log, CT))
                    throw new StopException($@"Unable to write evaluated PATCH XML file ""{evaluatedPath}""", BuildPatchDir, BuildSettings.InternalCTS);

                lock(dict)
                    dict[path] = xmlFile;
            });

            // Done.
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
            Log.Error(ex, Dir);
            return false;
        }
        finally
        {
            semaphore.Release();
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

                Var variable = new(v);
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

    public async Task<bool> TryLoadWithEvaluatedVariablesAsync(XmlFile xmlFile, int modID, int autoID, int customID, IReadOnlyDictionary<string, Var> variables, ILogger log, CancellationToken ct)
    {
        try
        {
            log?.Debug($@"Loading evaluated XML document: ""{xmlFile.Path}""", xmlFile.Path);

            if (xmlFile.Path.IsNullOrWhiteSpace() || !IOUtils.FileExists(xmlFile.Path))
            {
                log?.Error($@"XML document could not be found at ""{xmlFile.Path}""", xmlFile.Path);
                return false;
            }
            string content = await IOUtils.TryReadAllTextAsync(xmlFile.Path, log, ct);
            OrderedDictionary<string, string> replacements = new(StringComparer.OrdinalIgnoreCase);

            // Replacements using the reserved variable '{id}'
            if (modID == 0)
            {
                if (customID == autoID || customID == 0)
                {
                    // Replace {id} with generated autoID
                    log?.Info($@"Replacing '{ModdingConstants.BracedIdVariable}' with AUTO-ID='{autoID}' in XML file ""{xmlFile.FileName}"", Path");
                    replacements[ModdingConstants.BracedIdVariable] = autoID.ToString(); // i.e. replace {id} with autoID
                }
                else
                {
                    // Replace {id} with adjusted customID
                    log?.Info($@"Replacing '{ModdingConstants.BracedIdVariable}' with CUSTOM-ID='{customID}' in XML file ""{xmlFile.FileName}""", xmlFile.Path);
                    replacements[ModdingConstants.BracedIdVariable] = customID.ToString(); // i.e. replace {id} with customID
                }
            }
            else // modID == 0
            {
                if (customID == modID || customID == 0)
                {
                    // Replace {id} with declared modID
                    log?.Info($@"Replacing '{ModdingConstants.BracedIdVariable}' with MOD-ID='{modID}' in XML file ""{xmlFile.FileName}""", xmlFile.Path);
                    replacements[ModdingConstants.BracedIdVariable] = $@"{modID}"; // i.e. replace {id} with modID
                }
                else
                {
                    // Replace {id} with adjusted customID
                    log?.Info($@"Replacing '{ModdingConstants.BracedIdVariable}' with CUSTOM-ID='{customID}' in XML file ""{xmlFile.FileName}""", xmlFile.Path);
                    replacements[ModdingConstants.BracedIdVariable] = customID.ToString(); // i.e. replace {id} with customID
                }
            }

            // Replace numeric "modID with numeric "customID
            if (modID != 0 && customID != modID && customID != 0)
            {
                log?.Info($@"Replacing MOD-ID='{modID}' with CUSTOM-ID='{customID}' in XML file ""{xmlFile.FileName}""", xmlFile.Path);
                content = content.Replace('"' + modID.ToString(), '"' + customID.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            // Replace mod variables with their respective values:
            foreach (Var v in variables.Values)
                replacements[v.BracedName] = v.StrValue;

            // Execute replacements in one pass using REGEX:
            Regex regex = new(@"\{[^{}]+\}", RegexOptions.IgnoreCase);
            content = regex.Replace(content, match => replacements.TryGetValue(match.Value, out string value) ? value : match.Value);

            ct.ThrowIfCancellationRequested();

            // Reload XML document after all replacements:
            if (!xmlFile.TrySetXmlContent(content, Log))
                return false;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex, xmlFile.Path);
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
        Data = null;
        BuildSettings = null;

        Variables?.Clear();
        Variables = null;

        Audio?.Clear();
        Audio = null;

        try { await FullFileLogger.DisposeAsync(); } catch { }
        FullFileLogger = null;

        try { await ErrorFileLogger.DisposeAsync(); } catch { }
        ErrorFileLogger = null;

        try { await Log.DisposeAsync(); } catch { }
        Log = null;
    }
    #endregion

    public override string ToString() => Data.UniqueName;
}
