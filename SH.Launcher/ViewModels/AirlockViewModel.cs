using Avalonia.Media;
using SH.Framework.Logging;

namespace SH.Launcher.ViewModels;

public partial class AirlockViewModel : ViewModelBase
{
    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;

    private bool PreviousIsLeftPaneCollapsed;
    private double PreviousBackgroundDarkness;
    private bool PreviousIsBackgroundEnabled;

    public void Start()
    {
        PreviousBackgroundDarkness = AppSettings.BackgroundDarkness;
        PreviousIsLeftPaneCollapsed = AppSettings.IsLeftPaneCollapsed;
        PreviousIsBackgroundEnabled = AppSettings.IsBackgroundEnabled;
        State.LeftPaneBackgroundColor = Brushes.Black;
        AppSettings.BackgroundDarkness = 0.0;
        AppSettings.IsLeftPaneCollapsed = true;
        AppSettings.IsBackgroundEnabled = true;
        State.ForcedBackground = null;
    }

    public void Stop()
    {
        State.LeftPaneBackgroundColor = new SolidColorBrush(Color.Parse("#3F000000"));
        AppSettings.BackgroundDarkness = PreviousBackgroundDarkness;
        AppSettings.IsLeftPaneCollapsed = AppSettings.IsLeftPaneCollapsed && PreviousIsLeftPaneCollapsed;
        AppSettings.IsBackgroundEnabled = PreviousIsBackgroundEnabled;
    }
}
