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
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Launcher.Extensions;
using SH.Launcher.ViewModels.Enums;
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

    private readonly Bitmap NavigationConsoleBackgroundImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/NavigationConsole.jpg");

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

        // Since we have 5 progress "bars", we don't need to be notified more than 5 times
        backup.Max = 5;
        template.Max = 5;
        cache.Max = 5;
        loadMods.Max = 5;

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
                Log.Info($"Resetting {SpaceHavenLauncher.Name} files...");
                await IOUtils.TryDeleteDirectoryAsync(Paths.Data.TemplateDir, Log, ct);
                await IOUtils.TryDeleteDirectoryAsync(Paths.Data.BuildDir, Log, ct);
                await IOUtils.TryDeleteDirectoryAsync(Paths.Data.CacheDir, Log, ct);
            }

            DeploymentService svc = new(Paths.Data, Log);

            // There must be an original JAR file in order to proceed:
            Log.Debug($@"Performing backup of original files...", Paths.Data.BackupDir);
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
            Log.Debug($@"Preparing template files...", Paths.Data.TemplateDir);
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
            Log.Debug($@"Reading version...", Paths.Data.TemplateDir);
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
            Log.Debug($@"Validating mod cache...", Paths.Data.CacheDir);
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
            Log.Debug($@"Loading mods...");
            if (!await TryReloadModsAsync(ct, loadMods))
            {
                Log.Error("Unable to all load mods");
                LeftScreen.SetError(ELeftScreenStep.LoadMods);
                return;
            }
            loadMods.Complete();

            // Done.
            Log.Success($"{SpaceHavenLauncher.Name} initialization is complete", Paths.WorkDir);
            LeftScreen.LeftButtonsState = EControlState.Ready;
        }
        catch (OperationCanceledException)
        {
            Log.Error($"{SpaceHavenLauncher.Name} initialization was cancelled");
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



    private async Task<bool> TryReloadModsAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress?.Start();

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
            OrderedDictionary<string, ModData> mods = await modRepoSvc.TryLoadMods(ct, progress);
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
            progress?.Complete();
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

        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine3Async;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine2Async;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine1Async;
        runGame.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine0Async;
        
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenProgressBarAsync;

        CentralScreen.ShowLaunchOriginalOnMonitor();

        progress.Max = 8; // since we have 8 progress "bars", we don't need to be notified more than 8 times
        progress.Start();
        runGame.Start();

        await Semaphore.WaitAsync();
        try
        {
            using CancellationTokenSource cts = new();
            State.LaunchCTS = cts;

            Log.Info("Launching the ORIGINAL game...", Paths.SpaceHavenDir);
            CentralScreen.LeftLeverState = EControlState.Running;

            // DEPLOY
            DeploymentService svc = new(Paths.Data, Log);
            if (!await svc.RestoreOriginalGameAsync(State.LaunchCTS.Token, progress))
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
        progress.AddChild(initialization, 10);
        progress.AddChild(javaBuild, 5);
        progress.AddChild(xmlBuild, 80);
        progress.AddChild(deploy, 5);
        ProgressInfo runGame = new(Paths.SpaceHavenName);

        runGame.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine0Async;
        xmlBuild.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine1Async;
        javaBuild.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine2Async;
        initialization.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine3Async;

        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenProgressBarAsync;

        CentralScreen.ShowLaunchModifiedOnMonitor();

        progress.Max = 8; // since we have 8 progress "bars", we don't need to be notified more than 8 times
        progress.Start();
        runGame.Start();

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
                AppVersion = SpaceHavenLauncher.Version,
                SpaceHavenVersion = Paths.SpaceHavenVersion,
                ForceSpriteSheetSize2048 = AppSettings.ForceSpriteSheetSize2048,
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
        IProgressInfo extractOriginalLibrary = new ProgressInfo(nameof(extractOriginalLibrary));
        IProgressInfo extractOriginalFiles = extractOriginalLibrary.CreateChild(nameof(extractOriginalFiles), 40);
        IProgressInfo annotateOriginalXML = extractOriginalLibrary.CreateChild(nameof(annotateOriginalXML), 60);

        IProgressInfo exportOriginalTextures = new ProgressInfo(nameof(exportOriginalTextures));
        IProgressInfo exportOriginalSpriteSheets = exportOriginalTextures.CreateChild(nameof(exportOriginalSpriteSheets), 80);
        IProgressInfo exportOriginalSprites = exportOriginalTextures.CreateChild(nameof(exportOriginalSprites), 20);

        IProgressInfo extractModifiedLibrary = new ProgressInfo(nameof(extractModifiedLibrary));
        IProgressInfo extractModifiedFiles = extractModifiedLibrary.CreateChild(nameof(extractModifiedFiles), 40);
        IProgressInfo annotateModifiedXML = extractModifiedLibrary.CreateChild(nameof(annotateModifiedXML), 60);

        IProgressInfo exportModifiedTextures = new ProgressInfo(nameof(exportModifiedTextures));
        IProgressInfo exportModifiedSpriteSheets = exportModifiedTextures.CreateChild(nameof(exportModifiedSpriteSheets), 80);
        IProgressInfo exportModifiedSprites = exportModifiedTextures.CreateChild(nameof(exportModifiedSprites), 20);

        extractOriginalLibrary.ProgressChanged += RightScreen.OnExportOriginalLibraryProgressAsync;
        exportOriginalTextures.ProgressChanged += RightScreen.OnExportOriginalTexturesProgressAsync;
        extractModifiedLibrary.ProgressChanged += RightScreen.OnExportModifiedLibraryProgressAsync;
        exportModifiedTextures.ProgressChanged += RightScreen.OnExportModifiedTexturesProgressAsync;

        // Since we have 5 progress "bars", we don't need to be notified more than 5 times
        extractOriginalLibrary.Max = 5;
        exportOriginalTextures.Max = 5;
        extractModifiedLibrary.Max = 5;
        exportModifiedTextures.Max = 5;

        extractOriginalLibrary.Reset();
        exportOriginalTextures.Reset();

        extractModifiedLibrary.Reset();
        exportModifiedTextures.Reset();

        string workDir = Paths.Data.WorkDir;
        string exportDir = Paths.Data.ExportDir;

        string exportOriginalFilesDir = Paths.Data.ExportOriginalFilesDir;
        string exportOriginalTexturesDir = Paths.Data.ExportOriginalTexturesDir;

        string exportModifiedFilesDir = Paths.Data.ExportModifiedFilesDir;
        string exportModifiedTexturesDir = Paths.Data.ExportModifiedTexturesDir;

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

                if ((AppSettings.ExportOption & EExportOption.Original) != EExportOption.Original)
                {
                    Log.Warn($@"Skipping export of ORIGINAL files, accordingly to System Core settings", "tab://SystemCore");
                }
                else if (!File.Exists(originalJarPath) || !Directory.Exists(originalFilesDir))
                {
                    Log.Warn($"Unable to locate ORIGINAL files => {SpaceHavenLauncher.Name} was not properly initialized", workDir);
                    exportOriginalSuccess = false;
                }
                else
                {
                    // Export ORIGINAL Library:
                    Log.Info($@"Extracting ORIGINAL library...", exportOriginalFilesDir);
                    JarRepositoryService repo = new(Paths.Data, Log);
                    if (!await repo.TryExportLibraryAsync(originalJarPath, exportOriginalFilesDir, ct, extractOriginalFiles))
                    {
                        Log.Error($@"Unable to extract ORIGINAL library", exportDir);
                        RightScreen.SetError(ERightScreenStep.ExportOriginalLibrary);
                        exportOriginalSuccess = false;
                    }
                    else Log.Success($@"ORIGINAL files were successfully exported to: ""{exportOriginalFilesDir}""", exportOriginalFilesDir);

                    // Annotate ORIGINAL Libraries:
                    Log.Info($"Writing XML annotation...");
                    XmlAnnotationService xmlAnnotationService = new(Log);
                    if (!await xmlAnnotationService.TryRunAsync(exportOriginalFilesDir, AppSettings.ExportXmlAnnotationLanguage, ct, annotateOriginalXML))
                    {
                        Log.Error($@"Unable to annotate ORIGINAL library", exportDir);
                        RightScreen.SetError(ERightScreenStep.ExportOriginalLibrary);
                        exportOriginalSuccess = false;
                    }

                    // Export ORIGINAL Textures:
                    Log.Info($@"Exporting ORIGINAL textures...", exportOriginalTexturesDir);
                    if (!AppSettings.ExportTextures)
                    {
                        Log.Warn($@"Skipping export of ORIGINAL textures, accordingly to System Core settings", "tab://SystemCore");
                    }
                    else if (!await TryExportTextures(originalFilesDir, exportOriginalTexturesDir, parallelOptions, exportOriginalSpriteSheets, exportOriginalSprites))
                    {
                        Log.Error($@"Unable to export ORIGINAL library", exportDir);
                        RightScreen.SetError(ERightScreenStep.ExportOriginalTextures);
                        exportOriginalSuccess = false;
                    }
                    else Log.Success($@"ORIGINAL textures were successfully exported to: ""{exportOriginalTexturesDir}""", exportOriginalTexturesDir);
                }
            }

            // Export MODIFIED Library:
            bool exportModifiedSuccess = true;
            {
                string modifiedJarPath = Paths.Data.CacheJarPath;
                string modifiedFilesDir = Paths.Data.BuildStageDir;

                if ((AppSettings.ExportOption & EExportOption.Modified) != EExportOption.Modified)
                {
                    Log.Warn($@"Skipping export of MODIFIED files, accordingly to System Core settings", "tab://SystemCore");
                }
                else if (!File.Exists(modifiedJarPath) || !Directory.Exists(modifiedFilesDir))
                {
                    Log.Warn($"Unable to locate MODIFIED files => The MODIFIED game must be built first", workDir);
                    exportModifiedSuccess = false;
                }
                else
                {
                    // Export MODIFIED Library:
                    Log.Info($@"Exporting MODIFIED library...", exportModifiedFilesDir);
                    JarRepositoryService repo = new(Paths.Data, Log);
                    if (!await repo.TryExportLibraryAsync(modifiedJarPath, exportModifiedFilesDir, ct, extractModifiedFiles))
                    {
                        Log.Error($@"Unable to export MODIFIED library", exportDir);
                        RightScreen.SetError(ERightScreenStep.ExportModifiedLibrary);
                        exportModifiedSuccess = false;
                    }
                    else Log.Success($@"MODIFIED files were successfully exported to: ""{exportModifiedFilesDir}""", exportModifiedFilesDir);

                    // Annotate MODIFIED Libraries:
                    Log.Info($"Writing XML annotation...");
                    XmlAnnotationService xmlAnnotationService = new(Log);
                    if (!await xmlAnnotationService.TryRunAsync(exportModifiedFilesDir, AppSettings.ExportXmlAnnotationLanguage, ct, annotateModifiedXML))
                    {
                        Log.Error($@"Unable to annotate MODIFIED library", exportDir);
                        RightScreen.SetError(ERightScreenStep.ExportOriginalLibrary);
                        exportModifiedSuccess = false;
                    }

                    // Export MODIFIED Textures:
                    Log.Info($@"Exporting MODIFIED textures...", exportModifiedTexturesDir);
                    if (!AppSettings.ExportTextures)
                    {
                        Log.Warn($@"Skipping export of MODIFIED textures, accordingly to System Core settings", "tab://SystemCore");
                    }
                    else if (!await TryExportTextures(modifiedFilesDir, exportModifiedTexturesDir, parallelOptions, exportModifiedSpriteSheets, exportModifiedSprites))
                    {
                        Log.Error($@"Unable to export MODIFIED library", exportDir);
                        RightScreen.SetError(ERightScreenStep.ExportModifiedTextures);
                        exportModifiedSuccess = false;
                    }
                    else Log.Success($@"MODIFIED textures were successfully exported to: ""{exportModifiedTexturesDir}""", exportModifiedTexturesDir);
                }
            }

            // Done.
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

            extractOriginalLibrary?.ProgressChanged -= RightScreen.OnExportOriginalLibraryProgressAsync;
            exportOriginalTextures?.ProgressChanged -= RightScreen.OnExportOriginalTexturesProgressAsync;
            extractModifiedLibrary?.ProgressChanged -= RightScreen.OnExportModifiedLibraryProgressAsync;
            exportModifiedTextures?.ProgressChanged -= RightScreen.OnExportModifiedTexturesProgressAsync;

            extractOriginalLibrary?.Dispose();
            exportOriginalTextures?.Dispose();
            extractModifiedLibrary?.Dispose();
            exportModifiedTextures?.Dispose();
        }
    }



    /// <summary>
    /// TODO: Create class 'SpaceHavenContentRepository' and pack this method there...
    /// </summary>
    private async Task<bool> TryExportTextures(string modifiedFilesDir, string exportDir, ParallelOptions parallelOptions, IProgressInfo progressSpriteSheets, IProgressInfo progressSprites)
    {
        // Progress:
        IProgressInfo progressloadTexturesXml = progressSpriteSheets?.CreateChild(nameof(progressloadTexturesXml), 200);
        IProgressInfo progressLoadAnimationsXml = progressSpriteSheets?.CreateChild(nameof(progressLoadAnimationsXml), 400);
        IProgressInfo progressLoadGameArt = progressSpriteSheets?.CreateChild(nameof(progressLoadGameArt), 400);
        IProgressInfo progressExportSpriteSheets = progressSpriteSheets?.CreateChild(nameof(progressExportSpriteSheets), 7000);

        try
        {
            Stopwatch clock = Stopwatch.StartNew();

            // Paths
            string libraryDirectory = Path.Combine(modifiedFilesDir, SpaceHavenConstants.LIBRARY);
            string texturesXmlPath = Path.Combine(libraryDirectory, SpaceHavenConstants.TEXTURES);
            string animationsXmlPath = Path.Combine(libraryDirectory, SpaceHavenConstants.ANIMATIONS);

            progressSpriteSheets.Start();

            // Textures
            TexturesXmlRepository texturesXmlRepository = new(Log);
            Log.Info($"Reading textures...");
            progressloadTexturesXml.Start();
            clock.Restart();
            if (!await texturesXmlRepository.TryReadAsync(texturesXmlPath, parallelOptions.CancellationToken, progressloadTexturesXml))
                throw new Exception("Unable to read all textures XML information");
            Log.Debug($"{progressloadTexturesXml} = {clock.Elapsed.TotalMilliseconds} ms");
            progressloadTexturesXml.Complete();

            // Animations
            AnimationsXmlRepository animationsXmlRepository = new(Log);
            Log.Info($@"Reading animations...");
            progressLoadAnimationsXml.Start();
            clock.Restart();
            if (!await animationsXmlRepository.TryReadAsync(animationsXmlPath, parallelOptions.CancellationToken, progressLoadAnimationsXml))
                throw new Exception("Unable to read all animations XML information");
            Log.Debug($"{progressLoadAnimationsXml} = {clock.Elapsed.TotalMilliseconds} ms");
            progressLoadAnimationsXml.Complete();

            // Load Art
            Log.Info($"Loading game art...");
            progressLoadGameArt.Start();
            ArtRepository artRepository = new(texturesXmlRepository, animationsXmlRepository, Log);
            clock.Restart();
            if (!await artRepository.TryLoadAsync(libraryDirectory, parallelOptions.CancellationToken, progressLoadGameArt))
                throw new Exception("Unable to load all game art");
            Log.Debug($"{progressLoadGameArt} = {clock.Elapsed.TotalMilliseconds} ms");
            progressLoadGameArt.Complete();

            // Export CIM to PNG:
            Log.Info($"Exporting sprite sheets...");
            progressExportSpriteSheets.Start();
            clock.Restart();
            if (!await artRepository.TryExportSpriteSheetsToPngAsync(Path.Combine(exportDir, "textures"), parallelOptions, progressExportSpriteSheets))
                throw new Exception("Unable to export all sprite sheets to PNG");
            Log.Debug($"{progressExportSpriteSheets} = {clock.Elapsed.TotalMilliseconds} ms");
            progressExportSpriteSheets.Complete();

            progressSpriteSheets?.Complete();

            // Export individual sprites to PNG:
            Log.Info($"Exporting sprites...");
            progressSprites.Start();
            clock.Restart();
            if (!await artRepository.TryExportSpritesToPngAsync(Path.Combine(exportDir, "textures"), parallelOptions, progressSprites))
                throw new Exception("Unable to export all sprites to PNG");
            Log.Debug($"{progressSprites} = {clock.Elapsed.TotalMilliseconds} ms");
            progressSprites.Complete();

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }



    public void SetBackgroundImage() =>
        State.ForcedBackground = NavigationConsoleBackgroundImage;
}
