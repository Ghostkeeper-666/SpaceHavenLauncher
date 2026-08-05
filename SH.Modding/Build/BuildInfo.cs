using SH.Content;
using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Framework.Tasks;
using SH.Modding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Build;

internal sealed class BuildInfo : IAsyncDisposable
{
    public BuildInfo(BuildSettings settings, ILogger log)
    {
        BuildSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        FileLogger = new FileLogger(Paths.BuildLogPath);
        Log = new LoggerCollection(log, FileLogger);
        Log.Replacements = log?.Replacements?.ToArray(); // clone
    }

    private ILogger Log;
    private BuildSettings BuildSettings;
    private PathData Paths => BuildSettings.Paths;
    private ParallelOptions ParallelOptions => BuildSettings.ParallelOptions;
    private CancellationToken CT => BuildSettings.CT;
    private FileLogger FileLogger;

    public IReadOnlyList<Mod> Mods => ModList;
    private List<Mod> ModList = [];

    public bool HasXmlMods => ModList.Any(mod => mod.IsXmlMod);
    public bool HasJavaMods => ModList.Any(mod => mod.IsJavaMod);

    public SortedDictionary<EXmlFileType, XmlFile> XmlFile { get; private set; } = [];
    public SortedDictionary<EKeyPool, SortedSet<string>> UsedIds { get; private set; } = [];
    public int LastOriginalSpriteId { get; private set; }

    public string BuildHash { get; private set; }
    public IReadOnlyDictionary<string, string> BuildHashSources { get; private set; } = new Dictionary<string, string>();

    public void AddMods(IEnumerable<ModData> mods)
    {
        foreach (ModData mod in mods)
            ModList.Add(new Mod(BuildSettings, ModList.Count, mod, Log));
    }

    /// <summary>
    /// Computes hash from inputs! 
    /// </summary>
    public async Task<bool> ComputeHash()
    {
        try
        {
            Log.Debug("Computing global build hash...");

            SortedDictionary<string, string> buildHashSources = new();
            BuildHashSources = buildHashSources;

            // Compute mod hashes:
            await Parallel.ForEachAsync(ModList, ParallelOptions, async (mod, ct) => await mod.ComputeHash());
            CT.ThrowIfCancellationRequested();

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
            buildHashSources["App"] = XxHash64Calculator.ComputeFromString(appData.JoinToString("\n"), Log) ?? string.Empty;
            CT.ThrowIfCancellationRequested();

            // JAR:
            buildHashSources[SpaceHavenConstants.SPACEHAVEN_JAR] = IOUtils.TryReadAllText(Paths.TemplateJarHashPath, out string templateHash) ? templateHash : string.Empty;
            CT.ThrowIfCancellationRequested();

            // Mods:
            string xmlModsHashData = ModList.JoinToString(mod => $@"{mod.UniqueName}={mod.BuildHash}", "\n") ?? string.Empty;
            buildHashSources["Mods"] = XxHash64Calculator.ComputeFromString(xmlModsHashData, Log) ?? string.Empty;
            CT.ThrowIfCancellationRequested();

            // Final hash:
            string buildHash = buildHashSources.JoinToString((kvp) => $"{kvp.Key}={kvp.Value}", "\n");
            BuildHash = XxHash64Calculator.ComputeFromString(buildHash, Log);
            CT.ThrowIfCancellationRequested();

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            BuildHash = string.Empty;
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
            Log.Info($@"Loading XML files...", Paths.BuildStageDir);
            progress?.Start();

            await Parallel.ForEachAsync(Paths.BuildStageXmlPaths, BuildSettings.ParallelOptions, async (kvp, ct) =>
            {
                // Instantiate:
                XmlFile xmlFile;
                lock (XmlFile)
                    xmlFile = XmlFile[kvp.Key] = new(kvp.Key, Paths.BuildStageDir, kvp.Value);
                if (!await xmlFile.TryLoadAsync(Log, ct))
                    throw new StopException($@"Unable to load original '{kvp.Key}' file", Paths.BuildStageDir, BuildSettings.InternalCTS);
                progress.IncrementNormalized(1.0 / Paths.BuildStageXmlPaths.Count);
            });

            // Compute registered XML node IDs:
            if (!await TryComputeRegisteredNodeIDs(ct))
                return false;

            // Done.
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            Log.Error($@"Unable to read XML files", Paths.BuildStageDir);
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
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        BuildSettings = null;

        foreach (Mod mod in ModList ?? [])
            try { await mod.DisposeAsync(); } catch { }
        ModList.Clear();
        ModList = null;

        UsedIds.Clear();
        UsedIds = null;

        XmlFile.Clear();
        XmlFile = null;

        BuildHashSources = null;

        try { await FileLogger.DisposeAsync(); }
        catch { }
        FileLogger = null;

        try { await Log.DisposeAsync(); } catch { }
        Log = null;
    }
    #endregion IAsyncDisposable
}
