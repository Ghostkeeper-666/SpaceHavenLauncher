using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.ViewModels.Enums;
using System.Collections.ObjectModel;

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



    public NavigationConsoleLeftScreenViewModel()
    {
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
        Reset();
    }



    public void Reset()
    {
        for (int step = 0; step < Steps; ++step)
            Set((ELeftScreenStep)step, false, 0.0, false);
    }

    public async void OnBackupOriginalProgressAsync(object _, ProgressEventArgs e) =>
        Dispatcher.Run(() => Set(ELeftScreenStep.Backup, false, e.Progress.NormalizedValue, e.Progress.HasStarted));

    public async void OnCreateTemplateProgressAsync(object _, ProgressEventArgs e) =>
        Dispatcher.Run(() => Set(ELeftScreenStep.Template, false, e.Progress.NormalizedValue, e.Progress.HasStarted));

    public async void OnValidateCacheProgressAsync(object _, ProgressEventArgs e) =>
        Dispatcher.Run(() => Set(ELeftScreenStep.Cache, false, e.Progress.NormalizedValue, e.Progress.HasStarted));

    public async void OnLoadModsProgressAsync(object _, ProgressEventArgs e) =>
        Dispatcher.Run(() => Set(ELeftScreenStep.LoadMods, false, e.Progress.NormalizedValue, e.Progress.HasStarted));

    public void SetError(ELeftScreenStep step)
    {
        Set(step, true, 0.0, false);
        LeftButtonsState = EControlState.Error;
    }

    private void Set(ELeftScreenStep step, bool error, double progress, bool hasStarted) => AppViewModel.Dispatcher.Run(() =>
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
