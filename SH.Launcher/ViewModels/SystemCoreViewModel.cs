using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Content.Enums;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Launcher.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class SystemCoreViewModel : ViewModelBase
{
    public SystemCoreViewModel() { }

    private readonly Bitmap BackgroundImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/SystemCore.jpg");

    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;

    public void SetBackgroundImage() =>
        State.ForcedBackground = BackgroundImage;

    [ObservableProperty]
    private bool _IsWin = OS.IsWin;

    [ObservableProperty]
    private ELogVerbosity[] _LogVerbosityValues = Enum.GetValues<ELogVerbosity>().ToArray();

    [ObservableProperty]
    private ELanguage[] _ExportXmlAnnotationLanguages = Enum.GetValues<ELanguage>().OrderByDescending(v => v).ToArray();

    [ObservableProperty]
    private EExportOption[] _ExportOptions = Enum.GetValues<EExportOption>().ToArray();

    [ObservableProperty]
    private string _DebugButtonText = "COLLECT";

    [ObservableProperty]
    private string _DebugToolTipText = $"This collects debugging information from {SpaceHavenLauncher.Name} and stores it {PathData.DebugFilename} for later analysis";

    [ObservableProperty]
    private string _DebugProgressText = string.Empty;


    private CancellationTokenSource DebugCTS;
    
    private IProgressInfo DebugProgress;

    public async Task CollectDebuggingInformation()
    {
        try
        {
            // Cancel if already running
            if (DebugCTS != null && !DebugCTS.IsCancellationRequested)
            {
                DebugCTS?.Cancel();
                return;
            }

            DebugButtonText = "STOP";

            using ProgressInfo debugProgress = new() { Max = 100 };
            DebugProgress = debugProgress;
            DebugProgress.ProgressChanged += (object sender, ProgressEventArgs e) =>
                State.DispatchQueue.TryEnqueue(() => DebugProgressText = $"Generating {PathData.DebugFilename}... ({e?.Progress?.NormalizedValue.ToString("0%")})");

            using CancellationTokenSource debugCTS = new();
            DebugCTS = debugCTS;

            DebugService svc = new(Paths.Data, Log);
            if (await Task.Run(async () => await svc.TryGenerateDebugFileAsync(DebugCTS.Token, DebugProgress)))
                DebugProgressText = $@"{PathData.DebugFilename} is ready!";
            else if(IOUtils.FileExists(Paths.Data.DebugFilePath))
                DebugProgressText = $@"{PathData.DebugFilename} was generated with some errors, check the log on Navigation Console";
            else DebugProgressText = $@"Unable to generate {PathData.DebugFilename}, check the log on Navigation Console";
        }
        catch (OperationCanceledException)
        {
            Log.Warn($"Operation was cancelled");
            DebugProgressText = "Cancelled";
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            try
            {
                DebugButtonText = "COLLECT";
                DebugProgress = null;
                DebugCTS = null;
            }
            catch { }
        }
    }
}
