using Avalonia.Media;
using SH.Framework.Logging;
using System;

namespace SH.Launcher.ViewModels;

public partial class AirlockViewModel : ViewModelBase
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private bool PreviousIsLeftPaneCollapsed;
    private double PreviousBackgroundDarkness;
    private bool PreviousIsBackgroundEnabled;

    private readonly MainWindowViewModel Parent;



    public AirlockViewModel(MainWindowViewModel parent)
    {
        Parent = parent?? throw new ArgumentNullException(nameof(parent));
    }



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
