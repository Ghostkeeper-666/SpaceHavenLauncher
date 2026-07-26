using Avalonia;
using SH.Launcher.Console;
using System;
using System.Linq;

namespace SH.Launcher;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Any(arg => arg.Equals("-console", StringComparison.OrdinalIgnoreCase)))
            {
                #warning TODO: create an avalonia console window and show it instead of the MainWindow!
                return ConsoleMode.Run(args);
            }
            else
            {
                return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex);
            return 666;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
#if DEBUG
        .WithDeveloperTools()
#endif
        .WithInterFont()
        .LogToTrace();
}
