using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SH.Content;
using SH.Content.Art;
using SH.Content.Xml;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Launcher.Extensions;
using SH.Launcher.ViewModels.Enums;
using SH.Launcher.Views;
using SH.Modding;
using SH.Modding.Build;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class NavigationConsoleViewModel : ViewModelBase
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private readonly Bitmap NavigationConsoleBackgroundImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/NavigationConsole.jpg");

    [ObservableProperty]
    private NavigationConsoleLeftScreenViewModel _LeftScreen;

    [ObservableProperty]
    private NavigationConsoleCentralScreenViewModel _CentralScreen;

    [ObservableProperty]
    private NavigationConsoleRightScreenViewModel _RightScreen;

    [ObservableProperty]
    private StreamGeometry _ToggleLogViewButtonIcon = null;

    [ObservableProperty]
    private ObservableCollection<string> _LogHistory = [];

    private readonly MainWindowViewModel Parent;

    public NavigationConsoleViewModel(MainWindowViewModel parent)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        LeftScreen = new(this);
        CentralScreen = new(this);
        RightScreen = new(this);
    }



    public async Task OnLeftButtons()
    {
        if (State.IsInitializing)
            State.InitializationCTS?.Cancel();
        else await State.InitializeAsync(true);
    }

    public async Task OnLeftLever() =>
        await LaunchOriginalGame();

    public async Task OnRightLever() =>
        await LaunchModifiedGame();

    public async Task OnRightButtons() =>
        await ExtractLibraryFiles();



    private async Task LaunchOriginalGame()
    {
        if (State.IsProcessing && !State.IsLaunching)
            return;


        // Cancel?
        if (State.IsLaunching)
        {
            if (State.IsSpaceHavenRunning)
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard(
                        "Abort?",
                        $"Aborting the launch will close {SpaceHavenConstants.SpaceHavenName} if it is still running. \n\nDo you want to abort?",
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
        ProgressInfo runGame = new(SpaceHavenConstants.SpaceHavenName);

        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine3Async;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine2Async;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine1Async;
        runGame.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine0Async;

        runGame.ProgressChanged += CentralScreen.OnProgress_Title;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenProgressBarAsync;
        progress.Max = 8; // since we have 8 progress "bars", we don't need to be notified more than 8 times

        // Semaphore:
        await State.Semaphore.WaitAsync();
        try
        {
            progress.Start();
            runGame.Start();
            CentralScreen.ShowOriginal();

            using CancellationTokenSource cts = new();
            State.LaunchCTS = cts;

            Log.Info("Launching the ORIGINAL game...", Paths.SpaceHavenDir);
            CentralScreen.LeftLeverState = EControlState.Running;

            // Nothing to do?

            progress.Complete();
            CentralScreen.LeftLeverState = EControlState.Ready;

            // LAUNCH GAME
            if (AppSettings.StartSpaceHavenAutomatically)
            {
                runGame.Complete();

                StringBuilder sb = new($"Jumping to {SpaceHavenConstants.SpaceHavenName}\n");
                string dashedLine = $"{new('=', sb.Length - 1)}";
                sb.Insert(0, $"{dashedLine}\n");
                sb.Append(dashedLine);
                Log.Success(sb.ToString(), Paths.Data.SpaceHavenDir);

                // Run and await Space Haven:
                await Task.Yield();
                State.IsSpaceHavenRunning = true;
                if (await OS.TryStartApplication(Paths.Data.SpaceHavenPath, Log, State.LaunchCTS.Token))
                    Log.Success($"{SpaceHavenConstants.SpaceHavenName} has completed successfully", Paths.SpaceHavenDir);
                else Log.Error($"{SpaceHavenConstants.SpaceHavenName} has completed with errors", Paths.SpaceHavenDir);
                await Task.Yield();
            }
            else
            {
                Log.Warn("Space Haven was not started automatically, as defined by System Core settings", "app://SystemCore");
                await Task.Delay(500);
            }

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
            State?.IsSpaceHavenRunning = false;

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
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            try
            {
                progress?.Dispose();
                runGame?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            CentralScreen?.Reset();

            State?.LaunchCTS = null;
            State?.Semaphore?.Release();
        }
    }

    private async Task LaunchModifiedGame()
    {
        if (State.IsProcessing && !State.IsLaunching)
            return;


        // Cancel?
        if (State.IsLaunching)
        {
            if (State.IsSpaceHavenRunning)
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard(
                        "Abort?",
                        $"Aborting the launch will close {SpaceHavenConstants.SpaceHavenName} if it is still running. \n\nDo you want to abort?",
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
        ProgressInfo initializationProgress = new("Build Initialization");
        ProgressInfo javaBuildProgress = new("Build JAVA Mods");
        ProgressInfo xmlBuildProgress = new("Build XML Mods");
        ProgressInfo progress = new("Progress");
        progress.AddChild(initializationProgress, 1);
        progress.AddChild(javaBuildProgress, 1);
        progress.AddChild(xmlBuildProgress, 6);
        ProgressInfo runGame = new(SpaceHavenConstants.SpaceHavenName);

        runGame.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine0Async;
        xmlBuildProgress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine1Async;
        javaBuildProgress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine2Async;
        initializationProgress.ProgressChanged += CentralScreen.OnProgress_CentralScreenLine3Async;

        runGame.ProgressChanged += CentralScreen.OnProgress_Title;
        progress.ProgressChanged += CentralScreen.OnProgress_CentralScreenProgressBarAsync;
        progress.Max = 8; // since we have 8 progress "bars", we don't need to be notified more than 8 times

        // Semaphore:
        await State.Semaphore.WaitAsync();
        try
        {
            progress.Start();
            runGame.Start();
            CentralScreen.ShowModified();

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

            // GET MOD LIST:
            List<ModData> mods =
                State.Mods
                .Where(modViewModel => modViewModel.IsEnabled)
                .Select(modViewModel => modViewModel.Data)
                .ToList();

            if (mods.Count <= 0)
            {
                Log.Error(State.StatusBarText = "Please INSTALL and ENABLE at least one mod for launching a modified game!", "app://LearningComputer");
                CentralScreen.RightLeverState = EControlState.Error;
                return;
            }

            // BUILD:
            using BuildSettings settings = new(Paths.Data, ct)
            {
                AppVersion = SpaceHavenLauncher.Version,
                AppDir = Paths.AppDir,
                WorkDir = Paths.WorkDir,

                SpaceHavenVersion = State.SpaceHavenVersion,
                SpaceHavenDir = Paths.SpaceHavenDir,
                SpaceHavenJarDir = Paths.SpaceHavenJarDir,
                GamePlatform = State.GamePlatform,

                SkipRebuilding = AppSettings.SkipRebuilding,

                InitializationProgress = initializationProgress,
                JavaBuildProgress = javaBuildProgress,
                XmlBuildProgress = xmlBuildProgress,
            };

            settings.Mods.AddRange(mods);

            BuildService builderSvc = new(Log);
            if (!await builderSvc.TryBuildAsync(settings))
            {
                Log.Error(State.StatusBarText = "Unable to build selected mods", Paths.Data.BuildDir);
                CentralScreen.RightLeverState = EControlState.Error;
                return;
            }
            progress.Complete();
            CentralScreen.RightLeverState = EControlState.Ready;


            // LAUNCH GAME
            bool hasXmlMod = mods.Any(m => m.IsXmlMod);
            bool hasJavaMod = mods.Any(m => m.IsJavaMod);
            if (AppSettings.StartSpaceHavenAutomatically)
            {
                runGame.Complete();
                State.IsSpaceHavenRunning = true;

                GameLauncherService launcherSvc = new(Paths.Data, Log);

                // Run and await Space Haven:
                if (await launcherSvc.TryLaunchModifiedGameAsync(
                    State.GamePlatform,
                    State.AppSettings.JavaMainClass,
                    State.AppSettings.JavaVMArgs,
                    mods.Where(m => m.IsJavaMod).SelectMany(m => m.JarPaths),
                    Paths.Data.CacheJarPath,
                    ct))
                    Log.Success($"{SpaceHavenConstants.SpaceHavenName} has completed successfully", Paths.SpaceHavenDir);
                else
                    Log.Error($"{SpaceHavenConstants.SpaceHavenName} has completed with errors", Paths.SpaceHavenDir);
            }
            else
            {
                Log.Warn("Space Haven was not started automatically, as defined by System Core settings", "app://SystemCore");
                await Task.Delay(250);
            }

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
            State?.IsSpaceHavenRunning = false;

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
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            try
            {
                progress?.Dispose();
                runGame?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            CentralScreen?.Reset();

            State?.LaunchCTS = null;
            State?.Semaphore?.Release();
        }
    }

    public async Task ExtractLibraryFiles()
    {
        if (State.IsProcessing && !State.IsExporting)
            return;


        // Cancel?
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

        await State.Semaphore.WaitAsync();
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
                    Log.Warn($@"Skipping export of ORIGINAL files, accordingly to System Core settings", "app://SystemCore");
                }
                else if (!IOUtils.FileExists(originalJarPath) || !IOUtils.DirExists(originalFilesDir))
                {
                    Log.Warn($"Unable to locate ORIGINAL files => {SpaceHavenLauncher.Name} was not properly initialized", workDir);
                    exportOriginalSuccess = false;
                }
                else
                {
                    // Export ORIGINAL Library:
                    Log.Info($@"Extracting ORIGINAL library...", exportOriginalFilesDir);
                    JarRepositoryService repo = new(Log);
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
                        Log.Warn($@"Skipping export of ORIGINAL textures, accordingly to System Core settings", "app://SystemCore");
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
                    Log.Warn($@"Skipping export of MODIFIED files, accordingly to System Core settings", "app://SystemCore");
                }
                else if (!IOUtils.FileExists(modifiedJarPath) || !IOUtils.DirExists(modifiedFilesDir))
                {
                    Log.Warn($"Unable to locate MODIFIED files => The MODIFIED game must be built first", workDir);
                    exportModifiedSuccess = false;
                }
                else
                {
                    // Export MODIFIED Library:
                    Log.Info($@"Exporting MODIFIED library...", exportModifiedFilesDir);
                    JarRepositoryService repo = new(Log);
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
                        Log.Warn($@"Skipping export of MODIFIED textures, accordingly to System Core settings", "app://SystemCore");
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
            try
            {
                extractOriginalLibrary?.ProgressChanged -= RightScreen.OnExportOriginalLibraryProgressAsync;
                exportOriginalTextures?.ProgressChanged -= RightScreen.OnExportOriginalTexturesProgressAsync;
                extractModifiedLibrary?.ProgressChanged -= RightScreen.OnExportModifiedLibraryProgressAsync;
                exportModifiedTextures?.ProgressChanged -= RightScreen.OnExportModifiedTexturesProgressAsync;

                extractOriginalLibrary?.Dispose();
                exportOriginalTextures?.Dispose();
                extractModifiedLibrary?.Dispose();
                exportModifiedTextures?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            State?.ExportCTS = null;
            State?.Semaphore?.Release();
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
            string libraryDirectory = IOUtils.CombineAsOSPath(modifiedFilesDir, SpaceHavenConstants.LIBRARY);
            string texturesXmlPath = IOUtils.CombineAsOSPath(libraryDirectory, SpaceHavenConstants.TEXTURES);
            string animationsXmlPath = IOUtils.CombineAsOSPath(libraryDirectory, SpaceHavenConstants.ANIMATIONS);

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
            if (!await artRepository.TryExportSpriteSheetsToPngAsync(IOUtils.CombineAsOSPath(exportDir, "textures"), parallelOptions, progressExportSpriteSheets))
                throw new Exception("Unable to export all sprite sheets to PNG");
            Log.Debug($"{progressExportSpriteSheets} = {clock.Elapsed.TotalMilliseconds} ms");
            progressExportSpriteSheets.Complete();

            progressSpriteSheets?.Complete();

            // Export individual sprites to PNG:
            Log.Info($"Exporting sprites...");
            progressSprites.Start();
            clock.Restart();
            if (!await artRepository.TryExportSpritesToPngAsync(IOUtils.CombineAsOSPath(exportDir, "textures"), parallelOptions, progressSprites))
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
