using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SH.Content;
using SH.Content.Art;
using SH.Content.Modding;
using SH.Content.Modding.Build;
using SH.Content.Xml;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Extensions;
using SH.Launcher.Models;
using SH.Launcher.Repositories;
using SH.Launcher.Services;
using SH.Launcher.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class NavigationConsoleViewModel : ViewModelBase
{
    public NavigationConsoleViewModel() { }

    private readonly Bitmap NavigationConsoleBackgroundImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/NavigationConsole.jpg");

    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;

    private readonly SemaphoreSlim Semaphore = new(1, 1);



    [ObservableProperty]
    private LeftScreen _LeftScreen = new();

    [ObservableProperty]
    private CentralScreen _CentralScreen = new();

    [ObservableProperty]
    private RightScreen _RightScreen = new();


    [ObservableProperty]
    private StreamGeometry _ToggleLogViewButtonIcon = null;

    [ObservableProperty]
    private ObservableCollection<string> _LogHistory = [];



    public async Task OnLeftButtons() =>
        State.DispatchQueue.TryEnqueue(() => InitializeBuildSystemAsync(true));

    public async Task OnLeftLever() =>
        State.DispatchQueue.TryEnqueue(() => LaunchOriginalGame());

    public async Task OnRightLever() =>
        State.DispatchQueue.TryEnqueue(() => LaunchModifiedGame());

    public async Task OnRightButtons() =>
        await ExtractLibraryFiles();



    public async Task InitializeBuildSystemAsync(bool forceReset)
    {
        if (State.IsProcessing && !State.IsInitializing)
            return;

        if (State.IsInitializing)
        {
            Log.Warn("Cancelling initialization...");
            try { State.InitializeCTS?.Cancel(); } catch { }
            return;
        }

        ProgressInfo backup = new("Perform backup");
        ProgressInfo template = new("Prepare template");
        ProgressInfo cache = new("Validate cache");
        ProgressInfo loadMods = new("Load mods");
        ProgressInfo launch = new("Initialization",
        [
            (backup, 10),
            (template, 70),
            (cache, 10),
            (loadMods, 10),
        ]);

        backup?.ProgressChanged += LeftScreen.OnBackupOriginalProgressAsync;
        template?.ProgressChanged += LeftScreen.OnCreateTemplateProgressAsync;
        cache?.ProgressChanged += LeftScreen.OnValidateCacheProgressAsync;
        loadMods?.ProgressChanged += LeftScreen.OnLoadModsProgressAsync;

        launch.Reset(); // resets all children

        try
        {
            using CancellationTokenSource cts = new();
            State.InitializeCTS = cts;
            CancellationToken ct = cts.Token;

            LeftScreen.Reset();
            LeftScreen.LeftButtonsState = EControlState.Running;

            if (forceReset)
            {
                Log.Info($"Resetting {Paths.AppName} files...");
                await IOUtils.TryDeleteDirectoryAsync(Paths.Data.TemplateDir, Log, ct);
                await IOUtils.TryDeleteDirectoryAsync(Paths.Data.BuildDir, Log, ct);
                await IOUtils.TryDeleteDirectoryAsync(Paths.Data.CacheDir, Log, ct);
            }

            DeploymentService svc = new(Paths.Data, Log);

            // There must be an original JAR file in order to proceed:
            Log.Info($@"Performing backup of original files...", Paths.Data.BackupDir);
            if (!await svc.TryBackupOriginal(State.InitializeCTS.Token, backup))
            {
                Log.Error("Unable to backup original JAR file", Paths.Data.BackupDir);
                LeftScreen.SetError(ELeftScreenStep.BackupOriginal);
                return;
            }
            backup.Complete();
            Log.Success($"Original files backup is complete");

            await Task.Yield();

            // There must be a template jar file in order to proceed:
            Log.Info($@"Preparing template files...", Paths.Data.TemplateDir);
            if (!await Task.Run(() => svc.TryPrepareTemplateAsync(State.InitializeCTS.Token, template)))
            {
                Log.Error("Unable to prepare template JAR file", Paths.Data.TemplateDir);
                LeftScreen.SetError(ELeftScreenStep.CreateTemplate);
                return;
            }
            template.Complete();
            Log.Success($"Template files are ready");

            await Task.Yield();

            // Read version info:
            Log.Info($@"Reading version...", Paths.Data.TemplateDir);
            VersionParserService versionParser = new();
            if (!await Paths.TryReadSpaceHavenVersion(Log, State.InitializeCTS.Token))
            {
                Log.Error($"Unable to read {Paths.SpaceHavenName} version", Paths.Data.TemplateDir);
                LeftScreen.SetError(ELeftScreenStep.CreateTemplate);
                return;

            }
            Log.Success($"Detected {Paths.SpaceHavenName} version {Paths.SpaceHavenVersion}");

            await Task.Yield();

            // The cached mod jar must match the current original jar:
            Log.Info($@"Validating mod cache...", Paths.Data.CacheDir);
            if (!await Task.Run(() => svc.TryValidateModifiedCache(State.InitializeCTS.Token, cache)))
            {
                Log.Error("Validation of mod cache has failed", Paths.Data.CacheDir);
                LeftScreen.SetError(ELeftScreenStep.ValidateCache);
                return;
            }
            cache.Complete();
            Log.Success($"Cached files are validated");

            await Task.Yield();

            // Load mods:
            Log.Info($@"Loading mods...");
            if (!await TryReloadModsAsync(ct, loadMods))
            {
                Log.Error("Unable to all load mods");
                LeftScreen.SetError(ELeftScreenStep.LoadMods);
                return;
            }
            loadMods.Complete();

            // Done.
            Log.Success($"{Paths.AppName} initialization is complete", Paths.WorkDir);
            LeftScreen.LeftButtonsState = EControlState.Ready;
        }
        catch (OperationCanceledException)
        {
            Log.Error($"{Paths.AppName} initialization was cancelled");
            LeftScreen.LeftButtonsState = EControlState.Error;
        }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            LeftScreen.LeftButtonsState = EControlState.Error;
        }
        finally
        {
            State.InitializeCTS = null;
            launch?.Dispose();
        }
    }



    private async Task<bool> TryReloadModsAsync(CancellationToken ct, IProgressInfo loadModsProgress)
    {
        try
        {
            // Clear mod items:
            State.Mods.Clear();
            State.ModPages.Clear();

            // Prepare the list of items to remove, then remove, otherwise we get an exception:
            List<LeftPaneItem> leftPaneItemsToRemove = State.LeftPaneItems.Where(item => item.Type == EPageType.Mod).ToList();
            foreach (LeftPaneItem item in leftPaneItemsToRemove)
            {
                State.LeftPaneItems.Remove(item);
                State.FilteredLeftPaneItems.Remove(item);
            }

            // Load mods, then sort them:
            ModRepositoryService modRepoSvc = new(Paths.Data, Log);
            ModValuesRepositoryService valuesRepoSvc = new(Paths.Data, Log);
            OrderedDictionary<string, ModData> mods = await modRepoSvc.TryLoadMods(ct, loadModsProgress);
            if (mods == null)
                return false;
            mods = await valuesRepoSvc.TryLoadModSorting(mods, ct);

            // Load mod values:
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

            // Now add the loaded mods:
            foreach (ModData modData in mods.Values)
            {
                ct.ThrowIfCancellationRequested();

                ModViewModel mod = new(modData, mods.Values);
                State.Mods.Add(mod);
                State.ModPages.Add(mod.Name, new ModPageViewModel(mod));
                State.LeftPaneItems.Add(new LeftPaneItem(EPageType.Mod, mod));
                await Task.Yield();
            }
            State.FilteredLeftPaneItems = new(State.LeftPaneItems);

            // Update mod conflicts:
            State.UpdateModIds();
            State.UpdateModConflicts();
            State.UpdateModDependencies();

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Info(ex);
            return false;
        }
    }



    private async Task LaunchOriginalGame()
    {
        if (State.IsProcessing && !State.IsLaunching)
            return;

        if (State.IsLaunching)
        {
            if (State.IsSpaceHavenRunning)
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard(
                        "Abort?",
                        $"Aborting the launch will close {Paths.SpaceHavenName} if it is still running. \n\nDo you want to abort?",
                        ButtonEnum.YesNo,
                        Icon.Setting,
                        windowStartupLocation: Avalonia.Controls.WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(MainWindow.Window);

                // Cancel the cancellation:
                if (result != ButtonResult.Yes)
                    return;
            }

            // Cancel:
            Log.Warn("Cancelling launch...");
            try { State.LaunchCTS?.Cancel(); } catch { }
            return;
        }


        // Progress:
        ProgressInfo progress = new("Progress");
        ProgressInfo runGame = new("Launch");

        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenProgressBarAsync;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine3Async;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine2Async;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine1Async;
        runGame.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine0Async;

        // Use this trick to show inactive text on central screen monitor:
        runGame.SetNormalized(0.001);
        progress.SetNormalized(0.001);
        CentralScreen.ShowLaunchOriginalOnMonitor();

        await Semaphore.WaitAsync();
        try
        {
            using CancellationTokenSource cts = new();
            State.LaunchCTS = cts;

            Log.Info("Launching the ORIGINAL game...", Paths.SpaceHavenDir);
            CentralScreen.LeftLeverState = EControlState.Running;

            // DEPLOY
            DeploymentService svc = new(Paths.Data, Log);
            if (!await svc.DeployOriginalGameAsync(State.LaunchCTS.Token, progress))
                return;

            // Everything is ready!
            progress.Complete();

            // LAUNCH GAME
            CentralScreen.LeftLeverState = EControlState.Ready;
            runGame.Complete();

            StringBuilder sb = new($"Jumping to {Paths.SpaceHavenName}\n");
            string dashedLine = $"{new('=', sb.Length - 1)}";
            sb.Insert(0, $"{dashedLine}\n");
            sb.Append(dashedLine);
            Log.Success(sb.ToString(), Paths.Data.SpaceHavenDir);

            // Run and await Space Haven:
            await Task.Yield();
            State.IsSpaceHavenRunning = true;
            if (await OS.TryStartApplication(Paths.Data.SpaceHavenPath, Log, State.LaunchCTS.Token))
                Log.Success($"{Paths.SpaceHavenName} has completed successfully");
            else Log.Error($"{Paths.SpaceHavenName} has completed with errors");
            await Task.Yield();

            // Done.
            CentralScreen.LeftLeverState = EControlState.Standby;
        }
        catch (OperationCanceledException)
        {
            Log.Error($"Launch was cancelled");
            CentralScreen.LeftLeverState = EControlState.Error;
        }
        catch (Exception ex)
        {
            Log.Info($"Unable to launch the ORIGINAL game: {ex}", Paths.Data.SpaceHavenDir);
            CentralScreen.LeftLeverState = EControlState.Error;
        }
        finally
        {
            State.IsSpaceHavenRunning = false;

            try
            {
                // COSMETIC SHUTDOWN EFFECT:
                const int shutdownSteps = 10;
                double shutdownProgress = 1.0;
                for (int i = 0; i < shutdownSteps && progress?.NormalizedValue > 0.0; ++i) await Task.Run(async () =>
                {
                    progress?.SetNormalized(shutdownProgress -= 1.0 / shutdownSteps);
                    await Task.Delay(50);
                });
            }
            catch { }

            State.LaunchCTS = null;
            Semaphore.Release();

            CentralScreen.ShowEmptyOnMonitor();
            try
            {
                progress?.Dispose();
                runGame?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }
        }
    }



    private async Task LaunchModifiedGame()
    {
        if (State.IsProcessing && !State.IsLaunching)
            return;

        if (State.IsLaunching)
        {
            if (State.IsSpaceHavenRunning)
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard(
                        "Abort?",
                        $"Aborting the launch will close {Paths.SpaceHavenName} if it is still running. \n\nDo you want to abort?",
                        ButtonEnum.YesNo,
                        Icon.Setting,
                        windowStartupLocation: Avalonia.Controls.WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(MainWindow.Window);

                // Cancel the cancellation:
                if (result != ButtonResult.Yes)
                    return;
            }

            // Cancel:
            Log.Warn("Cancelling launch...");
            try { State.LaunchCTS?.Cancel(); } catch { }
            return;
        }

        // Progress:
        ProgressInfo initialization = new("Warm-up");
        ProgressInfo javaBuild = new("JAVA Mods");
        ProgressInfo xmlBuild = new("XML Mods");
        ProgressInfo deploy = new("Deploy");
        ProgressInfo progress = new("Progress");
        progress.Add(initialization, 10);
        progress.Add(javaBuild, 5);
        progress.Add(xmlBuild, 80);
        progress.Add(deploy, 5);
        ProgressInfo runGame = new(Paths.SpaceHavenName);

        runGame.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine0Async;
        xmlBuild.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine1Async;
        javaBuild.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine2Async;
        initialization.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine3Async;

        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenProgressBarAsync;

        // Use this trick to show inactive text on central screen monitor:
        CentralScreen.ShowLaunchModifiedOnMonitor();
        runGame.SetNormalized(0.001);
        progress.SetNormalized(0.001);

        await Semaphore.WaitAsync();
        try
        {
            using CancellationTokenSource cts = new();
            State.LaunchCTS = cts;
            CancellationToken ct = cts.Token;
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct,
            };

            Log.Info("Launching the MODIFIED game...", Paths.Data.CacheDir);
            CentralScreen.RightLeverState = EControlState.Running;

            // LIST MODS:
            List<ModData> mods =
                State.Mods
                .Where(modViewModel => modViewModel.IsEnabled)
                .Select(modViewModel => modViewModel.Data)
                .ToList();

            if (mods.Count <= 0)
            {
                Log.Error(State.StatusBarText = "Please include at least one mod");
                CentralScreen.RightLeverState = EControlState.Error;
                return;
            }

            await Task.Yield();

            // BUILD:
            BuildPathData paths = new()
            {
                AppDir = Paths.AppDir,
                WorkDir = Paths.WorkDir,
                SpaceHavenDir = Paths.SpaceHavenDir,
                SpaceHavenJarDir = Paths.SpaceHavenJarDir,
            };

            BuildSettings settings = new(ct)
            {
                AppVersion = SpaceHavenLauncher.GetAppVersion(),
                SpaceHavenVersion = Paths.SpaceHavenVersion,
                ForceSpritesheetSize2048 = AppSettings.ForceSpritesheetSize2048,
                Initialization = initialization,
                JavaBuild = javaBuild,
                XmlBuild = xmlBuild,
            };
            settings.Mods.AddRange(mods);

            BuildService builderSvc = new(Paths.Data, Log);
            if (!await builderSvc.TryBuildAsync(paths, settings))
            {
                Log.Error(State.StatusBarText = "Unable to build all the required mods", Paths.Data.BuildDir);
                CentralScreen.RightLeverState = EControlState.Error;
                return;
            }

            await Task.Yield();

            // DEPLOY
            DeploymentService svc = new(Paths.Data, Log);
            if (!await svc.DeployModifiedGameAsync(mods.Any(mod => mod.IsXmlMod), mods.Any(mod => mod.IsJavaMod), ct, deploy))
                return;
            deploy.Complete();

            await Task.Yield();

            // Everything is ready!
            progress.Complete();

            // LAUNCH GAME
            CentralScreen.RightLeverState = EControlState.Ready;
            runGame.Complete();

            StringBuilder sb = new($"Jumping to {Paths.SpaceHavenName}\n");
            string dashedLine = $"{new('=', sb.Length - 1)}";
            sb.Insert(0, $"{dashedLine}\n");
            sb.Append(dashedLine);
            Log.Success(sb.ToString(), Paths.Data.SpaceHavenDir);

            // Run and await Space Haven:
            await Task.Yield();
            State.IsSpaceHavenRunning = true;
            if (await OS.TryStartApplication(Paths.Data.SpaceHavenPath, Log, State.LaunchCTS.Token))
                Log.Success($"{Paths.SpaceHavenName} has completed successfully");
            else Log.Error($"{Paths.SpaceHavenName} has completed with errors");
            await Task.Yield();

            // Done.
            CentralScreen.RightLeverState = EControlState.Standby;
        }
        catch (OperationCanceledException)
        {
            Log.Error($"Launch was cancelled");
            CentralScreen.RightLeverState = EControlState.Error;
        }
        catch (Exception ex)
        {
            Log.Info($"Unable to launch the MODIFIED game: {ex}", Paths.Data.CacheDir);
            CentralScreen.RightLeverState = EControlState.Error;
        }
        finally
        {
            State.IsSpaceHavenRunning = false;

            try
            {
                // COSMETIC SHUTDOWN EFFECT:
                const int shutdownSteps = 10;
                double shutdownProgress = 1.0;
                for (int i = 0; i < shutdownSteps && progress?.NormalizedValue > 0.0; ++i) await Task.Run(async () =>
                {
                    progress?.SetNormalized(shutdownProgress -= 1.0 / shutdownSteps);
                    await Task.Delay(50);
                });
            }
            catch { }

            State.LaunchCTS = null;
            Semaphore.Release();

            CentralScreen.ShowEmptyOnMonitor();
            try
            {
                progress?.Dispose();
                runGame?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }
        }
    }



    public async Task ExtractLibraryFiles()
    {
        if (State.IsProcessing && !State.IsExporting)
            return;

        if (State.IsExporting)
        {
            Log.Warn("Cancelling export...");
            try { State.ExportCTS?.Cancel(); } catch { }
            return;
        }

        // Progress:
        ProgressInfo extractOriginalLibrary = new("Extract original library");
        ProgressInfo exportOriginalTextures = new("Export original textures");
        ProgressInfo extractModifiedLibrary = new("Extract modified library");
        ProgressInfo exportModifiedTextures = new("Export modified textures");

        extractOriginalLibrary.ProgressChanged += RightScreen.OnExportOriginalLibraryProgressAsync;
        exportOriginalTextures.ProgressChanged += RightScreen.OnExportOriginalTexturesProgressAsync;
        extractModifiedLibrary.ProgressChanged += RightScreen.OnExportModifiedLibraryProgressAsync;
        exportModifiedTextures.ProgressChanged += RightScreen.OnExportModifiedTexturesProgressAsync;

        extractOriginalLibrary.Reset();
        exportOriginalTextures.Reset();
        extractModifiedLibrary.Reset();
        exportModifiedTextures.Reset();

        string workDir = Paths.Data.WorkDir;
        string exportDir = Paths.Data.ExportDir;
        string exportOriginalDir = Paths.Data.ExportOriginalDir;
        string exportModifiedDir = Paths.Data.ExportModifiedDir;

        await Semaphore.WaitAsync();
        try
        {
            using CancellationTokenSource cts = new();
            State.ExportCTS = cts;
            CancellationToken ct = cts.Token;
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct,
            };

            RightScreen.Reset();
            RightScreen.RightButtonsState = EControlState.Running;

            // Export ORIGINAL Library:
            bool exportOriginalSuccess = true;
            {
                string originalJarPath = Paths.Data.BackupJarPath;
                string originalFilesDir = Paths.Data.TemplateStageDir;
                if (!File.Exists(originalJarPath) || !Directory.Exists(originalFilesDir))
                {
                    Log.Warn($"Unable to locate ORIGINAL files => {Paths.AppName} was not properly initialized", workDir);
                    exportOriginalSuccess = false;
                }
                else
                {
                    // Reset target directory:
                    if (!await IOUtils.TryDeleteDirectoryAsync(exportOriginalDir, Log, ct) || !await IOUtils.TryDeleteDirectoryAsync(exportOriginalDir, Log, ct))
                    {
                        Log.Error($@"Unable to reset export directory", exportDir);
                        RightScreen.SetError(ERightScreenStep.ExportOriginalLibrary);
                        RightScreen.SetError(ERightScreenStep.ExportOriginalTextures);
                        exportOriginalSuccess = false;
                    }
                    else
                    {
                        // Export ORIGINAL Library:
                        Log.Info($@"Extracting ORIGINAL library...", exportOriginalDir);
                        JarRepository repo = new(Paths.Data, Log);
                        if (!await repo.TryExportLibraryAsync(originalJarPath, exportOriginalDir, ct, extractOriginalLibrary))
                        {
                            Log.Error($@"Unable to extract ORIGINAL library", exportDir);
                            RightScreen.SetError(ERightScreenStep.ExportOriginalLibrary);
                            exportOriginalSuccess = false;
                        }
                        else extractOriginalLibrary.Complete();

                        // Annotate ORIGINAL Libraries:
                        XmlAnnotationService xmlAnnotationService = new(Log);
                        if (!await xmlAnnotationService.TryRunAsync(Paths.Data.ExportOriginalDir, AppSettings.XmlAnnotationLanguage, default))
                        {
                            Log.Error($@"Unable to annotate ORIGINAL library", exportDir);
                            RightScreen.SetError(ERightScreenStep.ExportOriginalLibrary);
                            exportOriginalSuccess = false;
                        }

                        // Export ORIGINAL Textures:
                        Log.Info($@"Exporting ORIGINAL textures...", exportOriginalDir);
                        if (!AppSettings.ExportTextures)
                        {
                            Log.Warn($@"Skipping export of ORIGINAL textures because it is disabled in System Core settings.", "tab://SystemCore");
                        }
                        else if (!await TryExportTextures(originalFilesDir, exportOriginalDir, parallelOptions, exportOriginalTextures))
                        {
                            Log.Error($@"Unable to export ORIGINAL library", exportDir);
                            RightScreen.SetError(ERightScreenStep.ExportOriginalTextures);
                            exportOriginalSuccess = false;
                        }
                        else exportOriginalTextures.Complete();
                    }
                }
            }

            // Export MODIFIED Library:
            bool exportModifiedSuccess = true;
            {
                string modifiedJarPath = Paths.Data.CacheJarPath;
                string modifiedFilesDir = Paths.Data.BuildStageDir;
                if (!File.Exists(modifiedJarPath) || !Directory.Exists(modifiedFilesDir))
                {
                    Log.Warn($"Unable to locate MODIFIED files => try to successfully run a MODIFIED game first", workDir);
                    exportModifiedSuccess = false;
                }
                else
                {
                    // Reset target directory:
                    if (!await IOUtils.TryDeleteDirectoryAsync(exportModifiedDir, Log, ct) || !await IOUtils.TryDeleteDirectoryAsync(exportModifiedDir, Log, ct))
                    {
                        Log.Error($@"Unable to reset export directory", exportDir);
                        RightScreen.SetError(ERightScreenStep.ExportModifiedLibrary);
                        RightScreen.SetError(ERightScreenStep.ExportModifiedTextures);
                        exportOriginalSuccess = false;
                    }
                    else
                    {
                        // Export MODIFIED Library:
                        Log.Info($@"Extracting MODIFIED library...", Paths.Data.ExportModifiedDir);
                        JarRepository repo = new(Paths.Data, Log);
                        if (!await repo.TryExportLibraryAsync(modifiedJarPath, Paths.Data.ExportModifiedDir, ct, extractModifiedLibrary))
                        {
                            Log.Error($@"Unable to extract MODIFIED library", exportDir);
                            RightScreen.SetError(ERightScreenStep.ExportModifiedLibrary);
                            exportModifiedSuccess = false;
                        }
                        else extractModifiedLibrary.Complete();

                        // Annotate MODIFIED Libraries:
                        XmlAnnotationService xmlAnnotationService = new(Log);
                        if (!await xmlAnnotationService.TryRunAsync(Paths.Data.ExportModifiedDir, AppSettings.XmlAnnotationLanguage, default))
                        {
                            Log.Error($@"Unable to annotate MODIFIED library", exportDir);
                            RightScreen.SetError(ERightScreenStep.ExportOriginalLibrary);
                            exportModifiedSuccess = false;
                        }

                        // Export MODIFIED Textures:
                        Log.Info($@"Exporting MODIFIED textures...", Paths.Data.ExportModifiedDir);
                        if (!AppSettings.ExportTextures)
                        {
                            Log.Warn($@"Skipping export of MODIFIED textures because it is disabled in System Core settings.", "tab://SystemCore");
                        }
                        else if (!await TryExportTextures(modifiedFilesDir, Paths.Data.ExportModifiedDir, parallelOptions, exportModifiedTextures))
                        {
                            Log.Error($@"Unable to export MODIFIED library", exportDir);
                            RightScreen.SetError(ERightScreenStep.ExportModifiedTextures);
                            exportModifiedSuccess = false;
                        }
                        else exportModifiedTextures.Complete();
                    }
                }
            }

            // Done.
            if (exportOriginalSuccess)
                Log.Success("ORIGINAL files were successfully exported", exportOriginalDir);

            if (exportModifiedSuccess)
                Log.Success("MODIFIED files were successfully exported", exportModifiedDir);

            if (exportOriginalSuccess || exportModifiedSuccess)
                State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(exportDir, Log));

            RightScreen.RightButtonsState =
                exportOriginalSuccess && exportModifiedSuccess ?
                EControlState.Ready : EControlState.Error;
        }
        catch (OperationCanceledException)
        {
            Log.Error($"Export was cancelled");
            RightScreen.RightButtonsState = EControlState.Error;
        }
        catch (Exception ex)
        {
            Log.Error(ex, workDir);
            RightScreen.RightButtonsState = EControlState.Error;
        }
        finally
        {
            State.ExportCTS = null;
            Semaphore.Release();
            extractOriginalLibrary?.Dispose();
            exportOriginalTextures?.Dispose();
            extractModifiedLibrary?.Dispose();
            exportModifiedTextures?.Dispose();
        }
    }



    /// <summary>
    /// TODO: Create class 'SpaceHavenContentRepository' and pack this method there...
    /// </summary>
    private async Task<bool> TryExportTextures(string modifiedFilesDir, string exportDir, ParallelOptions parallelOptions, IProgressInfo progress)
    {
        // Progress:
        ProgressInfo loadATexturesXml = new("Load textures XML");
        progress?.Add(loadATexturesXml, 59);
        ProgressInfo loadAnimationsXml = new("Load animations XML");
        progress?.Add(loadAnimationsXml, 393);
        ProgressInfo loadGameArt = new("Load Game Art");
        progress?.Add(loadGameArt, 192);
        ProgressInfo exportSpriteSheets = new("Export Sprite Sheets");
        progress?.Add(exportSpriteSheets, 8545);
        ProgressInfo exportSprites = new("Export Sprites");
        progress?.Add(exportSprites, 16925);

        try
        {
            Stopwatch clock = new();

            // Paths
            string libraryDirectory = Path.Combine(modifiedFilesDir, SpaceHavenConstants.LIBRARY);
            string texturesXmlPath = Path.Combine(libraryDirectory, SpaceHavenConstants.TEXTURES);
            string animationsXmlPath = Path.Combine(libraryDirectory, SpaceHavenConstants.ANIMATIONS);

            // Textures
            TexturesXmlRepository texturesXmlRepository = new(Log);
            Log.Debug($"Reading texture XML information from {texturesXmlPath}");
            clock.Restart();
            if (!await texturesXmlRepository.TryReadAsync(texturesXmlPath, parallelOptions.CancellationToken, loadATexturesXml))
                throw new Exception("[textures.xml] Unable to read all textures XML information");
            Log.Success($"{loadATexturesXml} = {clock.Elapsed.TotalMilliseconds} ms");
            loadATexturesXml.Complete();

            // Animations
            AnimationsXmlRepository animationsXmlRepository = new(Log);
            Log.Debug($@"[animations.xml] Reading animation XML information from ""{animationsXmlPath}""...");
            clock.Restart();
            if (!await animationsXmlRepository.TryReadAsync(animationsXmlPath, parallelOptions.CancellationToken, loadAnimationsXml))
                throw new Exception("[animations.xml] Unable to read all animations XML information");
            Log.Success($"{loadAnimationsXml} = {clock.Elapsed.TotalMilliseconds} ms");
            loadAnimationsXml.Complete();

            // Load Art
            Log.Debug($"Loading game art...");
            ArtRepository artRepository = new(texturesXmlRepository, animationsXmlRepository, Log);
            clock.Restart();
            if (!await artRepository.TryLoadAsync(libraryDirectory, parallelOptions.CancellationToken, loadGameArt))
                throw new Exception("Unable to load all game art");
            Log.Success($"{loadGameArt} = {clock.Elapsed.TotalMilliseconds} ms");
            loadGameArt.Complete();

            // Export CIM to PNG:
            Log.Debug($"Exporting sprite sheets...");
            clock.Restart();
            if (!await artRepository.TryExportSpriteSheetsToPngAsync(Path.Combine(exportDir, "textures"), parallelOptions, exportSpriteSheets))
                throw new Exception("Unable to export all sprite sheets to PNG");
            Log.Success($"{exportSpriteSheets} = {clock.Elapsed.TotalMilliseconds} ms");
            exportSpriteSheets.Complete();

            // Export individual sprites to PNG:
            Log.Debug($"Exporting sprites...");
            clock.Restart();
            if (!await artRepository.TryExportSpritesToPngAsync(Path.Combine(exportDir, "textures"), parallelOptions, exportSprites))
                throw new Exception("Unable to export all sprites to PNG");
            Log.Success($"{exportSprites} = {clock.Elapsed.TotalMilliseconds} ms");
            exportSprites.Complete();

            // Done.
            progress?.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
        finally
        {
            progress?.RemoveAll();
            loadATexturesXml?.Dispose();
            loadAnimationsXml?.Dispose();
            loadGameArt?.Dispose();
            exportSpriteSheets?.Dispose();
            exportSprites?.Dispose();
        }
    }



    public void SetBackgroundImage() =>
        State.ForcedBackground = NavigationConsoleBackgroundImage;
}
