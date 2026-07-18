using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Launcher.Extensions;
using SH.Modding.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class SystemCoreViewModel : ViewModelBase
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private CancellationTokenSource DebugCTS;

    private IProgressInfo DebugProgress;

    private readonly Bitmap BackgroundImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/SystemCore.jpg");

    [ObservableProperty]
    private bool _IsWin = OS.IsWin;

    [ObservableProperty]
    private ELogVerbosity[] _LogVerbosityValues = Enum.GetValues<ELogVerbosity>().ToArray();

    [ObservableProperty]
    private ELanguage[] _ExportXmlAnnotationLanguages = Enum.GetValues<ELanguage>().OrderBy(v => v).ToArray();

    [ObservableProperty]
    private EExportOption[] _ExportOptions = Enum.GetValues<EExportOption>().ToArray();

    [ObservableProperty]
    private string _DebugButtonText = "COLLECT";

    [ObservableProperty]
    private string _DebugProgressText = string.Empty;

    [ObservableProperty]
    private IBrush _SpaceHavenDir_ForeColor = Brushes.OrangeRed;
    [ObservableProperty]
    private IBrush _SpaceHavenJarDir_ForeColor = Brushes.OrangeRed;
    [ObservableProperty]
    private IBrush _SteamDir_ForeColor = Brushes.OrangeRed;
    [ObservableProperty]
    private IBrush _SteamModsDir_ForeColor = Brushes.OrangeRed;
    [ObservableProperty]
    private IBrush _ClassicModsDir_ForeColor = Brushes.OrangeRed;
    [ObservableProperty]
    private IBrush _JREPath_ForeColor = Brushes.OrangeRed;
    [ObservableProperty]
    private IBrush _JavaVMArgs_ForeColor = Brushes.OrangeRed;
    [ObservableProperty]
    private IBrush _JavaMainClass_ForeColor = Brushes.OrangeRed;

    [ObservableProperty]
    private string _Help_CollectDebuggingInformation = $"This collects debugging information from {SpaceHavenLauncher.Name} and stores it {PathData.DebugFilename} for later analysis";

    private readonly MainWindowViewModel Parent;



    public SystemCoreViewModel(MainWindowViewModel parent)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }



    public void OnDeactivated()
    {
        Paths.PropertyChanged -= ExternalPropertyChanged;
        AppSettings.PropertyChanged -= ExternalPropertyChanged;
        State.PropertyChanged -= ExternalPropertyChanged;
    }

    public void OnActivated()
    {
        SetBackgroundImage();
        UpdateColors();
        Paths.PropertyChanged -= ExternalPropertyChanged;
        Paths.PropertyChanged += ExternalPropertyChanged;
        AppSettings.PropertyChanged -= ExternalPropertyChanged;
        AppSettings.PropertyChanged += ExternalPropertyChanged;
        State.PropertyChanged -= ExternalPropertyChanged;
        State.PropertyChanged += ExternalPropertyChanged;
    }

    private void AppSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        UpdateColors();

    private void ExternalPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        UpdateColors();

    public void SetBackgroundImage() =>
        State.ForcedBackground = BackgroundImage;

    public void UpdateColors()
    {
        SpaceHavenDir_ForeColor =
            IOUtils.DirExists(Paths.SpaceHavenDir) ? Brushes.LightCyan : Brushes.OrangeRed;

        SpaceHavenJarDir_ForeColor =
            IOUtils.DirExists(Paths.SpaceHavenJarDir) ? Brushes.LightCyan : Brushes.OrangeRed;

        SteamDir_ForeColor =
            IOUtils.DirExists(Paths.SteamDir) ? Brushes.LightCyan : Brushes.Gold;

        SteamModsDir_ForeColor =
            !IOUtils.DirExists(Paths.SteamDir) ? Brushes.Gold :
            !IOUtils.DirExists(Paths.SteamModsDir) ? Brushes.OrangeRed :
            Brushes.LightCyan;

        ClassicModsDir_ForeColor =
            IOUtils.DirExists(Paths.ClassicModsDir) ? Brushes.LightCyan :
            IOUtils.DirExists(Paths.SteamDir) && IOUtils.DirExists(Paths.SteamModsDir) ? Brushes.Gold :
            Brushes.OrangeRed;

        JREPath_ForeColor =
            !IOUtils.FileExists(Paths.JREPath) ? Brushes.OrangeRed :
            !Paths.JREPath.StartsWith(Paths.SpaceHavenJarDir, StringComparison.OrdinalIgnoreCase) ? Brushes.Gold :
            Brushes.LightCyan;

        JavaVMArgs_ForeColor =
            AppSettings.JavaVMArgs.IsNullOrWhiteSpace() ? Brushes.OrangeRed :
            AppSettings.JavaVMArgs.Equals(State.TemplateJavaVMArgs) ? Brushes.LightCyan :
            Brushes.Gold;

        JavaMainClass_ForeColor =
            AppSettings.JavaMainClass.IsNullOrWhiteSpace() ? Brushes.OrangeRed :
            AppSettings.JavaMainClass.Equals(State.TemplateJavaMainClass) ? Brushes.LightCyan :
            Brushes.Gold;
    }

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
            DebugProgress.ProgressChanged += (sender, e) =>
                Dispatcher.Run(() => DebugProgressText = $"Generating {PathData.DebugFilename}... ({e?.Progress?.NormalizedValue.ToString("0%")})");

            using CancellationTokenSource debugCTS = new();
            DebugCTS = debugCTS;

            DebugService svc = new(Paths.Data, Log);
            if (await Task.Run(async () => await svc.TryGenerateDebugFileAsync(DebugCTS.Token, DebugProgress)))
                DebugProgressText = $@"{PathData.DebugFilename} is ready!";
            else if (IOUtils.FileExists(Paths.Data.DebugFilePath))
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
