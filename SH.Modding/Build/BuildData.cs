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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Build;

internal sealed class BuildData : IAsyncDisposable
{
    public BuildData(BuildSettings settings, ILogger log)
    {
        BuildSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        FileLogger = new FileLogger(Paths.BuildLogPath);
        Log = new LoggerCollection(log, FileLogger);
    }

    public ILogger Log { get; }
    public BuildPathData Paths => BuildSettings.Paths;

    private readonly BuildSettings BuildSettings;
    private ParallelOptions ParallelOptions => BuildSettings.ParallelOptions;
    private CancellationToken CT => BuildSettings.CT;
    private FileLogger FileLogger { get; }
    
    public List<ModBuildData> Mods { get; } = [];

    public bool HasXmlMods => Mods.Any(mod => mod.IsXmlMod);
    public bool HasJavaMods => Mods.Any(mod => mod.IsJavaMod);


    public SortedDictionary<EXmlFileType, XmlFile> XmlFile { get; } = [];
    public SortedDictionary<EKeyPool, SortedSet<string>> UsedIds { get; } = [];
    public int LastOriginalSpriteId { get; private set; }

    public string XmlHash { get; private set; }
    public IReadOnlyDictionary<string, string> XmlHashes { get; private set; } = new Dictionary<string, string>();

    public string JavaHash { get; private set; }
    public IReadOnlyDictionary<string, string> JavaHashes { get; private set; } = new Dictionary<string, string>();

    public ModsJsonFile ModsJsonFile { get; } = new();

    public void AddMods(IEnumerable<ModData> mods)
    {
        foreach (ModData mod in mods)
            Mods.Add(new ModBuildData(BuildSettings, Mods.Count, mod, this, Log));
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
                ["AppVersion"] = $@"""{BuildSettings.AppVersion}""",
                ["AppDir"] = $@"""{Paths.AppDir}""",
                ["WorkDir"] = $@"""{Paths.WorkDir}""",

                ["SpaceHavenVersion"] = $@"""{BuildSettings.SpaceHavenVersion}""",
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

    public int AllocateNextNumericId(EKeyPool poolId)
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

    public int GetLastUsedKey(EKeyPool poolId)
    {
        if (!UsedIds.TryGetValue(poolId, out SortedSet<string> pool))
            return 0;
        string idStr = pool.LastOrDefault() ?? string.Empty;
        if (!int.TryParse(idStr, out int id))
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



    public async Task<bool> TryLoadSpaceHavenXmlFilesAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            Log.Info($@"Loading XML files...", Paths.BuildStageDirectory);
            progress?.Start();

            // Instantiate:
            XmlFile[EXmlFileType.Haven] = new(EXmlFileType.Haven, Paths.BuildStageDirectory, Paths.BuildStageHavenXmlPath);
            if (!await XmlFile[EXmlFileType.Haven].TryLoadAsync(Log, ct))
                return false;
            progress.SetNormalized(0.30);

            XmlFile[EXmlFileType.Texts] = new(EXmlFileType.Texts, Paths.BuildStageDirectory, Paths.BuildStageTextsXmlPath);
            if (!await XmlFile[EXmlFileType.Texts].TryLoadAsync(Log, ct))
                return false;
            progress.SetNormalized(0.60);

            XmlFile[EXmlFileType.Audio] = new(EXmlFileType.Audio, Paths.BuildStageDirectory, Paths.BuildStageAudioXmlPath);
            if (!await XmlFile[EXmlFileType.Audio].TryLoadAsync(Log, ct))
                return false;
            progress.SetNormalized(0.61);

            XmlFile[EXmlFileType.Textures] = new(EXmlFileType.Textures, Paths.BuildStageDirectory, Paths.BuildStageTexturesXmlPath);
            if (!await XmlFile[EXmlFileType.Textures].TryLoadAsync(Log, ct))
                return false;
            progress.SetNormalized(0.64);

            XmlFile[EXmlFileType.Animations] = new(EXmlFileType.Animations, Paths.BuildStageDirectory, Paths.BuildStageAnimationsXmlPath);
            if (!await XmlFile[EXmlFileType.Animations].TryLoadAsync(Log, ct))
                return false;
            progress.SetNormalized(0.94);

            XmlFile[EXmlFileType.SpaceHavenSettings] = new(EXmlFileType.SpaceHavenSettings, Paths.BuildStageDirectory, Paths.BuildStageSpaceHavenSettingsXmlPath);
            if (!await XmlFile[EXmlFileType.SpaceHavenSettings].TryLoadAsync(Log, ct))
                return false;
            progress.SetNormalized(0.95);

            // Compute registered XML node IDs:
            if (!await TryComputeRegisteredNodeIDs(ct))
                return false;
            progress.Complete();

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

                if (nodeType.KeyAttribute == null)
                    continue;

                string[] ids =
                    XmlFile[nodeType.XmlFileType]
                    .GetNodes(nodeType)?
                    .Select(n => n.Attribute(nodeType.KeyAttribute)?.Value ?? string.Empty)?
                    .OrderBy(id => id)?
                    .ToArray() ?? [];

                ct.ThrowIfCancellationRequested();

                if (!UsedIds.TryGetValue(nodeType.KeyPool, out SortedSet<string> idList))
                    UsedIds[nodeType.KeyPool] = idList = new(new NumericIdComparer());

                foreach (string id in ids)
                    if (!idList.Add(id))
                        Log.Debug($@"Duplicate id=""{id}"" found in ID Pool '{nodeType.KeyPool}'");
            }

            // Scan Space Haven's texture file for last used sprite id:
            LastOriginalSpriteId =
                XmlFile[EXmlFileType.Textures]
                .GetNodes(NodeType.TexturesRegion)
                .Select(node => node.Attribute("id")?.Value ?? string.Empty)
                .Max(strId => int.TryParse(strId, out int id) ? id : 0);

            // Report currently last used ID for each pool:
            foreach (KeyValuePair<EKeyPool, SortedSet<string>> kvp in UsedIds)
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
