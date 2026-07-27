using CommunityToolkit.Mvvm.ComponentModel;
using SH.Content.Enums;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class AppSettingsViewModel : ObservableObject
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private volatile bool IsUpdating;

    private AppSettingsData Data;

    // PREVIOUS APP VERSION
    public VersionInfo PreviousAppVersion => Data?.PreviousAppVersion ?? new VersionInfo("6.6.6");

    // MONITOR:
    [ObservableProperty]
    private int _MonitorIndex;

    // LEFT PANE:
    [ObservableProperty]
    private bool _IsLeftPaneCollapsed;

    // LOG:
    [ObservableProperty]
    private ELogVerbosity _LogVerbosity;

    // BACKGROUND:
    [ObservableProperty]
    private bool _IsBackgroundEnabled;

    [ObservableProperty]
    private double _BackgroundDarkness;

    [ObservableProperty]
    private string _BackgroundTransparencyText;

    // MOD PAGE:
    [ObservableProperty]
    private int _ModPageSplitterHeight;

    // BUILD / LAUNCH:
    [ObservableProperty]
    private bool _SkipRebuilding;

    [ObservableProperty]
    private bool _StartSpaceHavenAutomatically;

    [ObservableProperty]
    private bool _CloseAppAutomaticallyOnLaunch;

    // EXPORT:
    [ObservableProperty]
    private ELanguage _ExportXmlAnnotationLanguage;

    [ObservableProperty]
    private bool _ExportTextures;

    [ObservableProperty]
    private EExportOption _ExportOption;

    // ADVANCED:
    [ObservableProperty]
    private string _JavaVMArgs;

    [ObservableProperty]
    private string _JavaMainClass;



    public AppSettingsViewModel()
    {
        Data = AppSettingsData.GetDefault();
        SetData(Data);
    }



    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        await UpdateDataAsync(e.PropertyName);
        base.OnPropertyChanged(e);
    }

    public AppSettingsData GetData() =>
        Data;

    public AppSettingsViewModel SetData(AppSettingsData data)
    {
        try
        {
            IsUpdating = true;

            Data = data ?? throw new ArgumentNullException(nameof(data));
            MonitorIndex = data.MonitorIndex;
            IsLeftPaneCollapsed = data.IsLeftPaneCollapsed;
            LogVerbosity = data.LogVerbosity;
            ModPageSplitterHeight = data.ModPageSplitterHeight;
            IsBackgroundEnabled = data.IsBackgroundEnabled;
            BackgroundDarkness = Math.Min(1.00, Math.Max(0.0, data.BackgroundDarkness));
            SkipRebuilding = data.SkipRebuilding;
            StartSpaceHavenAutomatically = data.StartSpaceHavenAutomatically;
            CloseAppAutomaticallyOnLaunch = data.CloseAppAutomaticallyOnLaunch;
            ExportXmlAnnotationLanguage = data.ExportXmlAnnotationLanguage;
            ExportTextures = data.ExportTextures;
            ExportOption = data.ExportOption;
            JavaVMArgs = data.JavaVMArgs;
            JavaMainClass = data.JavaMainClass;

            return this;
        }
        finally
        {
            IsUpdating = false;
        }
    }

    private async Task UpdateDataAsync(string propertyName)
    {
        if (IsUpdating)
            return;
        try
        {
            IsUpdating = true;

            switch (propertyName)
            {
                case nameof(MonitorIndex):
                    Data?.MonitorIndex = MonitorIndex;
                    break;
                case nameof(IsLeftPaneCollapsed):
                    Data?.IsLeftPaneCollapsed = IsLeftPaneCollapsed;
                    break;
                case nameof(LogVerbosity):
                    Data?.LogVerbosity = LogVerbosity;
                    State.Log.SetLogLevel(LogVerbosity.ToLogLevel());
                    break;
                case nameof(ModPageSplitterHeight):
                    Data?.ModPageSplitterHeight = ModPageSplitterHeight;
                    break;
                case nameof(IsBackgroundEnabled):
                    Data?.IsBackgroundEnabled = IsBackgroundEnabled;
                    break;
                case nameof(BackgroundDarkness):
                    Data?.BackgroundDarkness = BackgroundDarkness;
                    BackgroundTransparencyText = BackgroundDarkness >= 1.0 ? $"Background: OFF" : $"Background: {(1.0 - BackgroundDarkness):0%}";
                    break;
                case nameof(SkipRebuilding):
                    Data?.SkipRebuilding = SkipRebuilding;
                    break;
                case nameof(StartSpaceHavenAutomatically):
                    Data?.StartSpaceHavenAutomatically = StartSpaceHavenAutomatically;
                    break;
                case nameof(CloseAppAutomaticallyOnLaunch):
                    Data?.CloseAppAutomaticallyOnLaunch = CloseAppAutomaticallyOnLaunch;
                    break;
                case nameof(ExportXmlAnnotationLanguage):
                    Data?.ExportXmlAnnotationLanguage = ExportXmlAnnotationLanguage;
                    break;
                case nameof(ExportTextures):
                    Data?.ExportTextures = ExportTextures;
                    break;
                case nameof(ExportOption):
                    Data?.ExportOption = ExportOption;
                    break;
                case nameof(JavaVMArgs):
                    Data?.JavaVMArgs = JavaVMArgs;
                    break;
                case nameof(JavaMainClass):
                    Data?.JavaMainClass = JavaMainClass;
                    break;

                default:
                    return;
            }
            if (Paths != null)
            {
                AppSettingsRepositoryService repo = new(Paths.Data, Log);
                await repo.TrySaveAsync(Data, default);
            }
        }
        finally
        {
            IsUpdating = false;
        }
    }


}
