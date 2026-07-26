using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Modding;
using SH.Modding.Build;
using SH.Modding.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Console;

public static class ConsoleMode
{
    private static void OnLogMessage(object sender, LogMessage e) =>
        System.Console.WriteLine($"{$"[{e.Level.ToString().ToUpperInvariant()}]".PadRight(12)}{e?.Text}");

    public static int Run(string[] args) =>
        RunAsync(args).GetAwaiter().GetResult();

    public static async Task<int> RunAsync(string[] args)
    {
        CancellationToken ct = default;

        // Log:
        Logger Log = new();
        Log.SetLogLevel(ELogLevel.Info);
        Log.OnMessage += OnLogMessage;


        // Path settings:
        PathSettingsRepositoryService pathSvc = new(Log);
        PathData paths = pathSvc.TryLoad() ?? new();
        bool success = pathSvc.ResolveAll(paths);
        if (!success)
            Log.Error("Unable to locate all required paths. Please set them on System Core. Afterwards, re-initialize the Navigation Console");
        if (!await pathSvc.TrySaveAsync(paths, ct))
            Log.Error($@"Unable to save file, please check filesystem write permissions for ""{paths.PathSettingsPath}""");


        // Obfuscates absolute directories in log entries:
        Log.Replacements = paths.GetLogReplacements();


        // App settings:
        AppSettingsRepositoryService appSvc = new(paths, Log);
        AppSettingsData data = await appSvc.TryLoadOrCreateAsync(ct);
        if (data == null)
        {
            Log.Error($@"Unable to load or create application settings => please check for write permissions on ""{paths.WorkDir}""");
            data = AppSettingsData.GetDefault();
        }


        // Initialize:
        InitializationService initSvc = new(paths, Log, null, null, null);
        InitializationData initializationData = await initSvc.InitializeAsync(true, ct);
        if (initializationData == null)
            return -10;


        // Load mods:
        ModRepositoryService modRepoSvc = new(paths, Log);
        OrderedDictionary<string, ModData> mods = await modRepoSvc.TryLoadMods(ct, null);
        if (mods == null)
            return -20;


        // Load mod sequence and variable values:
        ModValuesRepositoryService valuesRepoSvc = new(paths, Log);
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
        using BuildSettings settings = new(paths, ct)
        {
            AppVersion = SpaceHavenLauncher.Version,
            AppDir = paths.AppDir,
            WorkDir = paths.WorkDir,
            SpaceHavenVersion = initializationData.SpaceHavenVersion,
            SpaceHavenDir = paths.SpaceHavenDir,
            SpaceHavenJarDir = paths.SpaceHavenJarDir,
            GamePlatform = initializationData.GamePlatform,
            SkipRebuilding = false,
        };
        settings.Mods.AddRange(mods.Values); // all mods
        BuildService builderSvc = new(Log);
        if (!await builderSvc.TryBuildAsync(settings))
            return -30;


        // Run Space Haven:
        GameLaunchService launcherSvc = new(paths, Log);
        if (!await launcherSvc.TryLaunchModifiedGameAsync(
            initializationData.GamePlatform,
            initializationData.JavaMainClass,
            initializationData.JavaVMArgs,
            mods.Values.Where(m => m.IsJavaMod).SelectMany(m => m.JarPaths),
            paths.CacheJarPath,
            ct))
            return -40;


        // Done.
        Log.Success("Press any key to EXIT");
        return 0;
    }
}
