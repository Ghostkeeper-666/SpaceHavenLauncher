using SH.Content;
using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Build;

internal sealed class BuildData : IAsyncDisposable
{
    public BuildData(BuildSettings buildSettings, BuildPathData paths, ILogger logger)
    {
        Settings = buildSettings ?? throw new ArgumentNullException(nameof(buildSettings));
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        FileLogger = new FileLogger(Paths.BuildLogPath);
        Log = new LoggerCollection(logger, FileLogger);
    }

    public ILogger Log { get; }
    public BuildPathData Paths { get; }

    private readonly BuildSettings Settings;
    private ParallelOptions ParallelOptions => Settings.ParallelOptions;
    private CancellationToken CT => Settings.CT;
    private FileLogger FileLogger { get; }
    public List<ModBuildData> Mods { get; } = [];
    public bool HasXmlMods => Mods.Any(mod => mod.IsXmlMod);
    public bool HasJavaMods => Mods.Any(mod => mod.IsJavaMod);


    public OrderedDictionary<EXmlFileType, XmlFile> XmlFile { get; } = [];
    public OrderedDictionary<EIdPool, SortedSet<string>> UsedIds { get; } = [];

    public string XmlHash { get; private set; }
    public IReadOnlyDictionary<string, string> XmlHashes { get; private set; } = new Dictionary<string, string>();

    public string JavaHash { get; private set; }
    public IReadOnlyDictionary<string, string> JavaHashes { get; private set; } = new Dictionary<string, string>();

    public ModsJsonFile ModsJsonFile { get; } = new();

    public void AddMods(IEnumerable<ModData> mods)
    {
        foreach (ModData mod in mods)
            Mods.Add(new ModBuildData(Settings, Mods.Count, mod, this, Log));
    }

    public async Task<bool> ComputeHash()
    {
        try
        {
            Log.Info("Computing build hash...");

            // Compute mod hashes:
            await Parallel.ForEachAsync(Mods, ParallelOptions, async (mod, ct) => await mod.ComputeHash());

            // From Space Haven Launcher:
            SortedDictionary<string, string> appData = new()
            {
                ["AppVersion"] = $@"""{Settings.AppVersion}""",
                ["AppDir"] = $@"""{Paths.AppDir}""",
                ["WorkDir"] = $@"""{Paths.WorkDir}""",

                ["SpaceHavenVersion"] = $@"""{Settings.SpaceHavenVersion}""",
                ["SpaceHavenDir"] = $@"""{Paths.SpaceHavenDir}""",
                ["SpaceHavenJarDir"] = $@"""{Paths.SpaceHavenJarDir}""",
            };
            string appHash = XxHash64Calculator.ComputeFromString(appData.JoinToString("\n"), Log) ?? string.Empty;

            // --- XML ---
            CT.ThrowIfCancellationRequested();
            {
                SortedDictionary<string, string> xmlHashes = new();
                XmlHashes = xmlHashes;

                // App:
                xmlHashes["App"] = appHash;

                // JAR:
                xmlHashes[SpaceHavenConstants.SPACEHAVEN_JAR] = IOUtils.TryReadAllText(Paths.TemplateJarHashPath, out string templateHash) ? templateHash : string.Empty;

                // Mods:
                string xmlModsHashData = Mods.Where(mod => mod.IsXmlMod).JoinToString(mod => $@"{mod.Name}={mod.XmlHash}", "\n") ?? string.Empty;
                xmlHashes["Mods"] = XxHash64Calculator.ComputeFromString(xmlModsHashData, Log) ?? string.Empty;

                // Overall XML Hash:
                string allXmlHashesStr = xmlHashes.JoinToString((kvp) => $"{kvp.Key}={kvp.Value}", "\n");
                XmlHash = XxHash64Calculator.ComputeFromString(allXmlHashesStr, Log);
            }

            // --- JAVA ---
            CT.ThrowIfCancellationRequested();
            {
                SortedDictionary<string, string> javaHashes = new();
                JavaHashes = javaHashes;

                // App:
                javaHashes["App"] = appHash;

                // Mods:
                string javaModsHashData = Mods.Where(mod => mod.IsJavaMod).JoinToString(mod => $@"{mod.Name}={mod.JavaHash}", "\n") ?? string.Empty;
                javaHashes["Mods"] = XxHash64Calculator.ComputeFromString(javaModsHashData, Log) ?? string.Empty;

                // Overall Java Hash:
                string allJavaHashesStr = javaHashes.JoinToString((kvp) => $"{kvp.Key}={kvp.Value}", "\n");
                JavaHash = XxHash64Calculator.ComputeFromString(allJavaHashesStr, Log);
            }

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to compute hash: {ex}");
            return false;
        }
    }

    public int AllocateNextNumericId(EIdPool poolId)
    {
        if (!UsedIds.TryGetValue(poolId, out SortedSet<string> pool))
            return 0;
        string idStr = pool.LastOrDefault() ?? string.Empty;
        if (!int.TryParse(idStr, out int id))
            return 0;
        if (!pool.Add((++id).ToString()))
            return 0;
        return id;
    }

    public static int NumericIdComparer(string sid1, string sid2)
    {
        if (ReferenceEquals(sid1, sid2))
            return 0;
        if (sid1 is null)
            return -1;
        if (sid2 is null)
            return 1;

        bool isNum1 = long.TryParse(sid1, out long id1);
        bool isNum2 = long.TryParse(sid2, out long id2);

        if (isNum1 && isNum2)
            return id1.CompareTo(id2);
        if (!isNum1 && !isNum2)
            return string.Compare(sid1, sid2, StringComparison.Ordinal);
        return isNum1 ? -1 : 1;
    }

    public async Task<bool> TryWriteVersion(ILogger logger, CancellationToken ct)
    {
        try
        {
            string[] lines = [Settings.SpaceHavenVersion.ToString(), "(modified)"];

            // Haven.xml:
            XmlFile[EXmlFileType.Haven].Xml.Root.SetAttributeValue("libVersion", lines.JoinToString(" "));

            // Version.txt:
            return await IOUtils.TryWriteAllTextAsync(Paths.BuildStageVersionPath, lines.JoinToString("\n"), logger, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
    }

    public async Task<bool> TryLoadXmlFiles(CancellationToken ct)
    {
        try
        {
            Log.Info($@"Loading XML files...", Paths.BuildStageDirectory);

            // Instantiate:
            XmlFile[EXmlFileType.Haven] = new(EXmlFileType.Haven, Paths.BuildStageDirectory, Paths.BuildStageHavenXmlPath);
            XmlFile[EXmlFileType.Texts] = new(EXmlFileType.Texts, Paths.BuildStageDirectory, Paths.BuildStageTextsXmlPath);
            XmlFile[EXmlFileType.Audio] = new(EXmlFileType.Audio, Paths.BuildStageDirectory, Paths.BuildStageAudioXmlPath);
            XmlFile[EXmlFileType.Textures] = new(EXmlFileType.Textures, Paths.BuildStageDirectory, Paths.BuildStageTexturesXmlPath);
            XmlFile[EXmlFileType.Animations] = new(EXmlFileType.Animations, Paths.BuildStageDirectory, Paths.BuildStageAnimationsXmlPath);
            XmlFile[EXmlFileType.SpaceHavenSettings] = new(EXmlFileType.SpaceHavenSettings, Paths.BuildStageDirectory, Paths.BuildStageSpaceHavenSettingsXmlPath);

            // Read:
            foreach (XmlFile xmlFile in XmlFile.Values)
            {
                if (await xmlFile.TryLoadAsync(Log, ct))
                    continue;
                Log.Error($@"This XML file contains syntax errors: ""{xmlFile.Path}""", xmlFile.Path);
                return false;
            }

            // Compute registered XML node IDs:
            if (!await TryComputeRegisteredNodeIDs(ct))
                return false;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            Log.Error($@"Unable to read XML files", Paths.BuildStageDirectory);
            return false;
        }
    }

    public async Task<bool> TryComputeRegisteredNodeIDs(CancellationToken ct)
    {
        try
        {
            Log.Debug($@"Computing registered XML node IDs...");

            UsedIds.Clear();

            // Got through all XML files and collect registered nodes' IDs: 
            foreach (NodeType nodeType in NodeType.RegisteredTypes.Values)
            {
                ct.ThrowIfCancellationRequested();

                if (nodeType.IdAttribute == null)
                    continue;

                string[] ids =
                    XmlFile[nodeType.XmlFileType]
                    .GetNodes(nodeType)?
                    .Select(n => n.Attribute(nodeType.IdAttribute)?.Value ?? string.Empty)?
                    .OrderBy(id => id)?
                    .ToArray() ?? [];

                ct.ThrowIfCancellationRequested();

                if (!UsedIds.TryGetValue(nodeType.IdPool, out SortedSet<string> idList))
                    UsedIds[nodeType.IdPool] = idList = new(new NumericIdComparer());

                foreach (string id in ids)
                    if (!idList.Add(id))
                        Log.Debug($@"Duplicate id=""{id}"" found in ID Pool '{nodeType.IdPool}'");
            }

            // Report currently last used ID for each pool:
            foreach (KeyValuePair<EIdPool, SortedSet<string>> kvp in UsedIds)
                Log.Debug($@"Last ID used from pool '{kvp.Key}' is {kvp.Value.LastOrDefault()}");

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Debug($@"Unable to computing registered XML node IDs: {ex}");
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
        foreach (ModBuildData mod in Mods)
            try { await mod.DisposeAsync(); } catch { }
        try { await FileLogger.DisposeAsync(); } catch { }
    }
    #endregion IAsyncDisposable
}
