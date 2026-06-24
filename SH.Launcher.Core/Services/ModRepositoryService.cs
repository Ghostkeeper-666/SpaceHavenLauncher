using SH.Content.Modding;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class ModRepositoryService
{
    private readonly PathData Paths;
    private readonly ILogger Log;

    public ModRepositoryService(PathData paths, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = logger ?? new VoidLogger();
    }

    /// <summary>
    /// Loads all mods
    /// </summary>
    public async Task<OrderedDictionary<string, ModData>> TryLoadMods(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            // Mod Root Paths:
            List<string> modRootDirectories = [];
            if (!Paths.SteamModsDir.IsNullOrWhiteSpace() && Directory.Exists(Paths.SteamModsDir))
                modRootDirectories.Add(Paths.SteamModsDir);
            if (!Paths.ClassicModsDir.IsNullOrWhiteSpace() && Directory.Exists(Paths.ClassicModsDir))
                modRootDirectories.Add(Paths.ClassicModsDir);

            // Load:
            ModRepository modRepository = new(Log);
            OrderedDictionary<string, ModData> mods = await modRepository.TryLoadMods(modRootDirectories, ct, progress);

            // Done.
            return mods;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }
}




