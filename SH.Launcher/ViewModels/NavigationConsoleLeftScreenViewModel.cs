using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.ViewModels.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SH.Launcher.ViewModels;

public partial class NavigationConsoleLeftScreenViewModel : ObservableObject
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private int LineCount => Texts.Count;
    private const int ProgressBarStepCount = 5;

    private readonly IBrush TextBrush_Standby = Brushes.Gray;
    private readonly IBrush TextBrush_Running = Brushes.Yellow;
    private readonly IBrush TextBrush_Error = Brushes.LightCyan;
    private readonly IBrush TextBrush_Success = Brushes.LightCyan;

    private readonly IBrush BorderBrush_Standby = Brushes.Gray;
    private readonly IBrush[] BorderBrush_Running =
    [
        Brushes.Red,
        Brushes.Orange,
        Brushes.Gold,
        Brushes.YellowGreen,
        Brushes.Lime,
    ];
    private readonly IBrush BorderBrush_Error = Brushes.Gray;
    private readonly IBrush BorderBrush_Success = Brushes.Lime;

    private readonly IBrush BackgroundBrush_Standby = Brushes.Black;
    private readonly IBrush[] BackgroundBrush_Running =
    [
        Brushes.Red,
        Brushes.Orange,
        Brushes.Gold,
        Brushes.YellowGreen,
        Brushes.Lime,
    ];
    private readonly IBrush BackgroundBrush_Error = Brushes.OrangeRed;
    private readonly IBrush BackgroundBrush_Success = Brushes.Lime;

    [ObservableProperty]
    private string _Title = "INITIALIZATION";

    [ObservableProperty]
    private ObservableCollection<string> _Texts =
    [
        "CLONE ORIGINAL",
        "CRAFT TEMPLATE",
        "VALIDATE CACHE",
        "LOAD ALL MODS ",
    ];

    [ObservableProperty]
    private ObservableCollection<bool> _Error = [];

    [ObservableProperty]
    private ObservableCollection<IBrush> _TextColor = [];

    [ObservableProperty]
    private ObservableCollection<ObservableCollection<IBrush>> _ProgressBarBorder = [];

    [ObservableProperty]
    private ObservableCollection<ObservableCollection<IBrush>> _ProgressBarBackground = [];

    [ObservableProperty]
    private bool _LeftButtonsHovered;
    [ObservableProperty]
    private bool _LeftButtonsPressed;
    [ObservableProperty]
    private EControlState _LeftButtonsState;

    private readonly int Steps = EnumX.MaxValue<ELeftScreenStep>() + 1;

    private readonly NavigationConsoleViewModel Parent;

    public NavigationConsoleLeftScreenViewModel(NavigationConsoleViewModel parent)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));

        for (int line = 0; line < LineCount; ++line)
        {
            Error.Add(false);
            TextColor.Add(TextBrush_Standby);
            ProgressBarBorder.Add(new());
            ProgressBarBackground.Add(new());
            for (int bar = 0; bar < ProgressBarStepCount; ++bar)
            {
                ProgressBarBorder[line].Add(BorderBrush_Standby);
                ProgressBarBackground[line].Add(BackgroundBrush_Standby);
            }
        }

        for (int step = 0; step < Steps; ++step)
            Set((ELeftScreenStep)step, false, 0.0, false);

        State.PropertyChanged -= State_PropertyChanged;
        State.PropertyChanged += State_PropertyChanged;
    }

    private void State_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppViewModel.InitializationState))
        {
            switch (State.InitializationState)
            {
                case EControlState.Standby:
                    for (int step = 0; step < Steps; ++step)
                        Set((ELeftScreenStep)step, false, 0.0, false);
                    break;

                case EControlState.Error:
                    if (State.LoadModsProgress.HasStarted)
                        Set(ELeftScreenStep.LoadMods, true, 1.0, true);
                    else if (State.CacheProgress.HasStarted)
                        Set(ELeftScreenStep.Cache, true, 1.0, true);
                    else if (State.TemplateProgress.HasStarted)
                        Set(ELeftScreenStep.Template, true, 1.0, true);
                    else if (State.BackupProgress.HasStarted)
                        Set(ELeftScreenStep.Backup, true, 1.0, true);
                    break;

                case EControlState.Hovered:
                    break;

                case EControlState.Running:
                    break;

                case EControlState.Ready:
                    for (int step = 0; step < Steps; ++step)
                        Set((ELeftScreenStep)step, false, 1.0, true);
                    break;

                default:
                    throw new NotImplementedException($"{nameof(EControlState)} = {LeftButtonsState}");
            }

            LeftButtonsState = State.InitializationState;
        }
    }

    public async void OnBackupProgressAsync(object _, ProgressEventArgs e) =>
        Set(ELeftScreenStep.Backup, false, e.Progress.NormalizedValue, e.Progress.HasStarted);

    public async void OnTemplateProgressAsync(object _, ProgressEventArgs e) =>
        Set(ELeftScreenStep.Template, false, e.Progress.NormalizedValue, e.Progress.HasStarted);

    public async void OnCacheProgressAsync(object _, ProgressEventArgs e) =>
        Set(ELeftScreenStep.Cache, false, e.Progress.NormalizedValue, e.Progress.HasStarted);

    public async void OnModsProgressAsync(object _, ProgressEventArgs e) =>
        Set(ELeftScreenStep.LoadMods, false, e.Progress.NormalizedValue, e.Progress.HasStarted);

    private void Set(ELeftScreenStep step, bool error, double progress, bool hasStarted) => Dispatcher.Run(() =>
    {
        int line = (int)step;

        if (Error[line] = error)
        {
            TextColor[line] = TextBrush_Error;
            for (int bar = 0; bar < ProgressBarStepCount; ++bar)
            {
                ProgressBarBorder[line][bar] = BorderBrush_Error;
                ProgressBarBackground[line][bar] = BackgroundBrush_Error;
            }
            return;
        }

        if (progress <= 0.0)
        {
            TextColor[line] = hasStarted ? TextBrush_Running : TextBrush_Standby;
            for (int bar = 0; bar < ProgressBarStepCount; ++bar)
            {
                ProgressBarBorder[line][bar] = BorderBrush_Standby;
                ProgressBarBackground[line][bar] = BackgroundBrush_Standby;
            }
            return;
        }

        if (progress < 1.0)
        {
            TextColor[line] = TextBrush_Running;
            int threshold = (int)(progress * ProgressBarStepCount);
            for (int bar = 0; bar < ProgressBarStepCount; ++bar)
            {
                ProgressBarBorder[line][bar] = bar < threshold ? BorderBrush_Running[bar] : BorderBrush_Standby;
                ProgressBarBackground[line][bar] = bar < threshold ? BackgroundBrush_Running[bar] : BackgroundBrush_Standby;
            }
            return;
        }

        if (progress >= 1.0)
        {
            TextColor[line] = TextBrush_Success;
            for (int bar = 0; bar < ProgressBarStepCount; ++bar)
            {
                ProgressBarBorder[line][bar] = BorderBrush_Success;
                ProgressBarBackground[line][bar] = BackgroundBrush_Success;
            }
            return;
        }
    });

}
