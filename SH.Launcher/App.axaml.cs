using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SH.Launcher.ViewModels;
using SH.Launcher.Views;
using System;
using System.Linq;

namespace SH.Launcher;

public partial class App : Application
{
    public override void Initialize() =>
        AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        ToolTip.ShowDelayProperty.OverrideDefaultValue<TopLevel>(1);
        ToolTip.BetweenShowDelayProperty.OverrideDefaultValue<TopLevel>(1);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime?.Args?.Any(arg => arg.Equals("-console", StringComparison.OrdinalIgnoreCase)) ?? false)
            {
                // Console mode:
                lifetime.MainWindow = new ConsoleWindow()
                {
                    DataContext = new ConsoleWindowViewModel(),
#if DEBUG
                    WindowState = WindowState.Maximized,
#endif
                };
            }
            else
            {
                // Windowed mode:
                lifetime.MainWindow = new MainWindow()
                {
                    DataContext = new MainWindowViewModel(),
#if DEBUG
                    WindowState = WindowState.Maximized,
#endif
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}