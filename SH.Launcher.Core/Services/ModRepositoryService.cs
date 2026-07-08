using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Modding;
using System;
using System.Collections.Generic;
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
            if (IOUtils.DirectoryExists(Paths.SteamModsDir))
                modRootDirectories.Add(Paths.SteamModsDir);
            if (IOUtils.DirectoryExists(Paths.ClassicModsDir))
                modRootDirectories.Add(Paths.ClassicModsDir);

            // Load:
            ModRepository modRepository = new(Log);
            OrderedDictionary<string, ModData> mods =
                await Task.Run(() => modRepository.TryLoadMods(modRootDirectories, ct, progress));

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




