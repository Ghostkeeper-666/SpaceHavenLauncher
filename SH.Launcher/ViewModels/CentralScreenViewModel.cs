using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.ViewModels.Enums;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class CentralScreenViewModel : ObservableObject
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private const int LineCount = 4;
    private const int ProgressBarCount = 8;

    private readonly IBrush TextBrush_Offline = Brushes.Black;
    private readonly IBrush TextBrush_Inactive = new SolidColorBrush(Color.Parse("#3f4347"));
    private readonly IBrush TextBrush_Active = Brushes.LightCyan;

    private readonly IBrush BorderBrush_Inactive = Brushes.DarkGray;
    private readonly IBrush[] BorderBrush_Active =
    [
        Brushes.Red,
        Brushes.OrangeRed,
        Brushes.Orange,
        Brushes.Gold,
        Brushes.Yellow,
        Brushes.YellowGreen,
        Brushes.GreenYellow,
        Brushes.Lime,
    ];

    private readonly IBrush BackgroundBrush_Inactive = Brushes.Black;
    private readonly IBrush[] BackgroundBrush_Active =
    [
        Brushes.Red,
        Brushes.OrangeRed,
        Brushes.Orange,
        Brushes.Gold,
        Brushes.Yellow,
        Brushes.YellowGreen,
        Brushes.GreenYellow,
        Brushes.Lime,
    ];

    private readonly string Title_Empty = " NAVIGATION CONSOLE ";
    private readonly string Title_Running = "HYPERDRIVE ACTIVATED";

    private readonly string Title_Original = "LAUNCH ORIGINAL GAME";
    private readonly string[] Text_Original =
    [
        "LAUNCH APPLICATION",
        "POLISH HYPERDRIVES",
        "COPY CONFIGURATION",
        "INITIALIZE LAUNCH ",
    ];

    private readonly string Title_Modified = "LAUNCH MODIFIED GAME";
    private readonly string[] Text_Modified =
    [
        "LAUNCH APPLICATION",
        "BUILD ALL XML MODS",
        "JOIN ALL JAVA MODS",
        "INITIALIZE LAUNCH ",
    ];

    [ObservableProperty]
    private string _Title;

    [ObservableProperty]
    private ObservableCollection<string> _Text = [];
    [ObservableProperty]
    private ObservableCollection<IBrush> _TextColor = [];

    [ObservableProperty]
    private ObservableCollection<IBrush> _ProgressBarBorder = [];
    [ObservableProperty]
    private ObservableCollection<IBrush> _ProgressBarBackground = [];

    [ObservableProperty]
    private bool _LeftLeverHovered;
    [ObservableProperty]
    private bool _LeftLeverPressed;
    [ObservableProperty]
    private EControlState _LeftLeverState;

    [ObservableProperty]
    private bool _RightLeverHovered;
    [ObservableProperty]
    private bool _RightLeverPressed;
    [ObservableProperty]
    private EControlState _RightLeverState;


    public CentralScreenViewModel()
    {
        Title = Title_Empty;
        for (int line = 0; line < LineCount; ++line)
        {
            Text.Add(string.Empty);
            TextColor.Add(TextBrush_Offline);
        }
        for (int bar = 0; bar < ProgressBarCount; ++bar)
        {
            ProgressBarBorder.Add(BorderBrush_Inactive);
            ProgressBarBackground.Add(BackgroundBrush_Inactive);
        }
    }



    public async void OnProgress_CentralScreenProgressBarAsync(object sender, ProgressEventArgs e) =>
        Dispatcher.Run(async () => await SetProgressBarAsync(e.Progress.NormalizedValue));

    public async void OnProgress_CentralScreenLine0Async(object sender, ProgressEventArgs e) =>
        Dispatcher.Run(async () => await SetTextAsync(0, e.Progress.NormalizedValue, e.Progress.HasStarted));

    public async void OnProgress_CentralScreenLine1Async(object sender, ProgressEventArgs e) =>
        Dispatcher.Run(async () => await SetTextAsync(1, e.Progress.NormalizedValue, e.Progress.HasStarted));

    public async void OnProgress_CentralScreenLine2Async(object sender, ProgressEventArgs e) =>
        Dispatcher.Run(async () => await SetTextAsync(2, e.Progress.NormalizedValue, e.Progress.HasStarted));

    public async void OnProgress_CentralScreenLine3Async(object sender, ProgressEventArgs e) =>
        Dispatcher.Run(async () => await SetTextAsync(3, e.Progress.NormalizedValue, e.Progress.HasStarted));

    public async void OnProgress_Title(object sender, ProgressEventArgs e) =>
        Dispatcher.Run(async () => await SetTitleAsync(e.Progress.NormalizedValue, e.Progress.HasStarted));

    public void ShowEmptyOnMonitor()
    {
        if (State.IsProcessing)
            return;
        Title = Title_Empty;
        for (int line = 0; line < LineCount; ++line)
        {
            Text[line] = string.Empty;
            TextColor[line] = TextBrush_Offline;
        }
        for (int bar = 0; bar < ProgressBarCount; ++bar)
        {
            ProgressBarBorder[bar] = BorderBrush_Inactive;
            ProgressBarBackground[bar] = BackgroundBrush_Inactive;
        }
    }

    public void ShowLaunchOriginalTitleOnMonitor()
    {
        if (State.IsProcessing)
            return;
        Title = Title_Original;
    }

    public void ShowLaunchOriginalOnMonitor()
    {
        if (State.IsProcessing)
            return;
        Title = Title_Original;
        for (int line = 0; line < LineCount; ++line)
        {
            Text[line] = Text_Original[line];
            TextColor[line] = TextBrush_Inactive;
        }
        for (int bar = 0; bar < ProgressBarCount; ++bar)
        {
            ProgressBarBorder[bar] = BorderBrush_Inactive;
            ProgressBarBackground[bar] = BackgroundBrush_Inactive;
        }
    }

    public void ShowLaunchModifiedTitleOnMonitor()
    {
        if (State.IsProcessing)
            return;
        Title = Title_Modified;
    }

    public void ShowLaunchModifiedOnMonitor()
    {
        if (State.IsProcessing)
            return;
        Title = Title_Modified;
        for (int line = 0; line < LineCount; ++line)
        {
            Text[line] = Text_Modified[line];
            TextColor[line] = TextBrush_Inactive;
        }
        for (int bar = 0; bar < ProgressBarCount; ++bar)
        {
            ProgressBarBorder[bar] = BorderBrush_Inactive;
            ProgressBarBackground[bar] = BackgroundBrush_Inactive;
        }
    }

    private async Task SetProgressBarAsync(double progress) => AppViewModel.Dispatcher.Run(() =>
    {
        if (progress <= 0.0)
        {
            for (int bar = 0; bar < ProgressBarCount; ++bar)
            {
                ProgressBarBorder[bar] = BorderBrush_Inactive;
                ProgressBarBackground[bar] = BackgroundBrush_Inactive;
            }
            return;
        }

        if (progress >= 1.0)
        {
            for (int bar = 0; bar < ProgressBarCount; ++bar)
            {
                ProgressBarBorder[bar] = BorderBrush_Active[bar];
                ProgressBarBackground[bar] = BackgroundBrush_Active[bar];
            }
            return;
        }

        int threshold = (int)(progress * ProgressBarCount);
        for (int bar = 0; bar < ProgressBarCount; ++bar)
        {
            ProgressBarBorder[bar] = bar < threshold ? BorderBrush_Active[bar] : BorderBrush_Inactive;
            ProgressBarBackground[bar] = bar < threshold ? BackgroundBrush_Active[bar] : BackgroundBrush_Inactive;
        }
    });

    private async Task SetTextAsync(int line, double progress, bool hasStarted) => AppViewModel.Dispatcher.Run(() =>
    {
        TextColor[line] =
            !hasStarted ? TextBrush_Offline :
            progress <= 0.0 ? TextBrush_Inactive :
            TextBrush_Active;
    });

    private async Task SetTitleAsync(double progress, bool hasStarted) => AppViewModel.Dispatcher.Run(() =>
    {
        if (progress >= 1.0)
            Title = Title_Running;
    });

}
