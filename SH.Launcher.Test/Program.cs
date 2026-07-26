using SH.Framework.Logging;
using SH.Launcher.Console;
using System;

namespace SH.Launcher.Test;

internal class Program
{
    private static void OnLogMessage(object sender, LogMessage e) =>
        System.Console.WriteLine($"{$"[{e.Level.ToString().ToUpperInvariant()}]".PadRight(12)}{e?.Text}");

    private static int Main(string[] args)
    {
        try
        {
            SpaceHavenLauncherConsole console = new();
            console.Log.OnMessage += OnLogMessage;
            return console.Run(args);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.ToString());
            return 666;
        }
    }
}
