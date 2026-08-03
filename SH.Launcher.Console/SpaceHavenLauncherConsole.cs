using SH.Content;
using SH.Framework.IO;
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

namespace SH.Launcher.Console;

public sealed class SpaceHavenLauncherConsole
{
    #region static
    public static readonly string LogPath;
    static SpaceHavenLauncherConsole()
    {
        LogPath = IOUtils.CombineAsOSPath(SpaceHavenLauncher.WorkDir, "consoleLog.txt");
    }
    #endregion

    public ILogger Log;

    public SpaceHavenLauncherConsole()
    {
        Log = new LoggerCollection(
            new BatchLogger(TimeSpan.FromMilliseconds(200)),
            new FileLogger(SpaceHavenLauncher.ConsoleLogPath)
        );
    }

    public int Run(string[] args) =>
        RunAsync(args, null).GetAwaiter().GetResult();

    public async Task<int> RunAsync(string[] args, CancellationTokenSource cts) =>
        await Task.Run(async () => await RunInternalAsync(args, cts));

    public async Task<int> RunInternalAsync(string[] args, CancellationTokenSource cts)
    {
        args ??= [];
        cts ??= new CancellationTokenSource();
        CancellationToken ct = cts.Token;

        // Log:
        Log.Success("===========================================");
        Log.Success("=== Space Haven Launcher (Console Mode) ===");
        Log.Success("===========================================");

        // Path settings:
        PathSettingsRepositoryService pathSvc = new(Log);
        PathData paths = pathSvc.TryLoad() ?? new();
        bool success = pathSvc.ResolveAll(paths);
        if (!success)
            Log.Error("Please set the DIRECTORIES on SYSTEM CORE tab and re-initialize the NAVIGATION CONSOLE afterwards. If you need some help, go to LEARNING COMPUTER tab", "tab://LearningComputer");
        if (!await pathSvc.TrySaveAsync(paths, ct))
            Log.Error($@"Unable to load/save path settings file, please make sure you have set write permissions for ""{SpaceHavenLauncher.WorkDir}""", SpaceHavenLauncher.WorkDir);


        // Obfuscates absolute directories in log entries:
        Log.Replacements = paths.GetLogReplacements();


        // App settings:
        AppSettingsRepositoryService appSvc = new(paths, Log);
        AppSettingsData appSettingsData = await appSvc.TryLoadOrCreateAsync(ct);
        if (appSettingsData == null)
        {
            Log.Error($@"Unable to load/save application settings file, please make sure you have set write permissions for ""{SpaceHavenLauncher.WorkDir}""");
            appSettingsData = AppSettingsData.GetDefault();
        }
        Log.SetLogLevel(appSettingsData.LogVerbosity.ToLogLevel());


        // Update to curent version:
        await appSvc.TrySaveAsync(appSettingsData, ct);


        // Initialize:
        InitializationService initSvc = new(paths, Log, null, null, null);
        InitializationData initializationData = await initSvc.InitializeAsync(false, ct);
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
#if DEBUG
            SkipRebuilding = false,
#else
            SkipRebuilding = appSettingsData.SkipRebuilding,
#endif
            GenerateAdditionalIntermediateBuildFiles = false,
        };
        settings.Mods.AddRange(mods.Values.Where(m => m.IsEnabled));
        BuildService builderSvc = new(Log);
        if (!await builderSvc.TryBuildAsync(settings))
            return -30;


        // Run Space Haven:
        GameLaunchService launcherSvc = new(paths, Log);

        // Run and await Space Haven:
        Task<bool> spaceHaven =
            launcherSvc.TryLaunchModifiedGameAsync(
            initializationData.GamePlatform,
            initializationData.JavaMainClass,
            initializationData.JavaVMArgs,
            mods.Values.Where(m => m.IsJavaMod).SelectMany(m => m.JarPaths),
            paths.CacheJarPath,
            ct);

        List<Task> tasks = [spaceHaven];
        if (appSettingsData.CloseAppAutomaticallyOnLaunch)
            tasks.Add(Task.Delay(3000));

        // Run:
        await Task.WhenAny(tasks);

        // Check result:
        if (!spaceHaven.IsCompleted)
            return 0; // happens ony when Task.Delay() finishes first!
        if (!spaceHaven.Result)
        {
            Log.Error($"{SpaceHavenConstants.SpaceHavenName} has completed with errors", paths.SpaceHavenDir);
            return -40;
        }
        Log.Success($"{SpaceHavenConstants.SpaceHavenName} has completed successfully", paths.SpaceHavenDir);

        // Done.
        Log.Success("Press any key to EXIT");
        return 0;
    }
}
