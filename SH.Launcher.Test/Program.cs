using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Modding;
using SH.Modding.Build;
using SH.Modding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Test;

internal class Program
{
    private static void OnLogMessage(object sender, LogMessage e) =>
        Console.WriteLine($"{$"[{e.Level.ToString().ToUpperInvariant()}]".PadRight(12)}{e?.Text}");

    private static Logger Log;
    private static PathData Paths;

    private static async Task Main(string[] args)
    {
        CancellationToken ct = default;


        // Log:
        Log = new();
        Log.SetLogLevel(ELogLevel.Info);
        Log.OnMessage += OnLogMessage;


        // Path settings:
        PathSettingsRepositoryService pathSvc = new(Log);
        Paths = pathSvc.TryLoad() ?? new();
        bool success = pathSvc.ResolveAll(Paths);
        if (!success)
            Log.Error("Unable to locate all required paths. Please set them on System Core. Afterwards, re-initialize the Navigation Console");
        if (!await pathSvc.TrySaveAsync(Paths, ct))
            Log.Error($@"Unable to save file, please check filesystem write permissions for ""{Paths.PathSettingsPath}""");


        // App settings:
        AppSettingsRepositoryService appSvc = new(Paths, Log);
        AppSettingsData data = await appSvc.TryLoadOrCreateAsync(ct);
        if (data == null)
        {
            Log.Error($@"Unable to load or create application settings => please check for write permissions on ""{Paths.WorkDir}""");
            data = AppSettingsData.GetDefault();
        }


        // Initialize:
        InitializationService initSvc = new(Paths, Log, null, null, null);
        InitializationData initializationData = await initSvc.InitializeAsync(true, ct);
        if (initializationData == null)
            return;


        // Load mods:
        ModRepositoryService modRepoSvc = new(Paths, Log);
        OrderedDictionary<string, ModData> mods = await modRepoSvc.TryLoadMods(ct, null);
        if (mods == null)
            return;


        // Load mod sequence and variable values:
        ModValuesRepositoryService valuesRepoSvc = new(Paths, Log);
        mods = await valuesRepoSvc.TryLoadModSortingAsync(mods, ct);
        foreach (ModData mod in mods.Values)
        {
            if (await valuesRepoSvc.TryLoadCurrentModValuesAsync(mod, ct))
            {
                // Try to also read previous version values:
                await valuesRepoSvc.TryLoadPreviousModValuesAsync(mod, false, ct);
            }
            else
            {
                // Try to read previous values for using them as current values:
                // (defaults to 'suggested value' if no previous value is defined)
                await valuesRepoSvc.TryLoadPreviousModValuesAsync(mod, true, ct);

                // Save current version values:
                await valuesRepoSvc.TrySaveModValuesAsync(mod, false, ct);
            }
        }


        // Build:
        using BuildSettings settings = new(Paths, ct)
        {
            AppVersion = SpaceHavenLauncher.Version,
            AppDir = Paths.AppDir,
            WorkDir = Paths.WorkDir,
            SpaceHavenVersion = initializationData.SpaceHavenVersion,
            SpaceHavenDir = Paths.SpaceHavenDir,
            SpaceHavenJarDir = Paths.SpaceHavenJarDir,
            GamePlatform = initializationData.GamePlatform,
            SkipRebuilding = false,
        };
        settings.Mods.AddRange(mods.Values); // all mods
        BuildService builderSvc = new(Log);
        if (!await builderSvc.TryBuildAsync(settings))
            return;


        // Run Space Haven:
        GameLaunchService launcherSvc = new(Paths, Log);
        if (!await launcherSvc.TryLaunchModifiedGameAsync(
            initializationData.GamePlatform,
            initializationData.JavaMainClass,
            initializationData.JavaVMArgs,
            mods.Values.Where(m => m.IsJavaMod).SelectMany(m => m.JarPaths),
            Paths.CacheJarPath,
            ct))
            return;


        // Done.
        Log.Success("Done.");
    }
}
