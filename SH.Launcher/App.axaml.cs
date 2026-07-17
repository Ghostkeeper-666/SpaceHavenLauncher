using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SH.Framework.Logging;
using SH.Launcher.ViewModels;
using SH.Launcher.Views;

namespace SH.Launcher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppViewModel.State.Log.SetLogLevel(ELogLevel.Info);

        ToolTip.ShowDelayProperty.OverrideDefaultValue<TopLevel>(1);
        ToolTip.BetweenShowDelayProperty.OverrideDefaultValue<TopLevel>(1);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
#if DEBUG
                WindowState = WindowState.Maximized,
#endif
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}