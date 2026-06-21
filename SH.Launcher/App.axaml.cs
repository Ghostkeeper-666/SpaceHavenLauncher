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
        SharedState.State.Log.SetLogLevel(ELogLevel.Info);

        ToolTip.ShowDelayProperty.OverrideDefaultValue(typeof(TopLevel), 10);
        ToolTip.BetweenShowDelayProperty.OverrideDefaultValue(typeof(TopLevel), 10);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                WindowState = WindowState.Maximized,
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}