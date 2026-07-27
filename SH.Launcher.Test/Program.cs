using SH.Framework.Logging;
using SH.Launcher.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SH.Launcher.Test;

internal static class Program
{
    private static void OnLogMessage(object sender, LogMessage e) =>
        System.Console.WriteLine($"{$"[{e.Level.ToString().ToUpperInvariant()}]".PadRight(12)}{e?.Text}");

    private static void BatchLogger_OnMessages(object sender, IReadOnlyList<LogMessage> e)
    {
        StringBuilder sb = new();
        foreach (LogMessage m in e ?? [])
            sb.AppendLine(m.Text);
        System.Console.Write(sb.ToString());
    }

    private static int Main(string[] args)
    {
        try
        {
            SpaceHavenLauncherConsole console = new();
            BatchLogger batchLogger = console.Log as BatchLogger ?? console.Log.Children.FirstOrDefault(log => log is BatchLogger) as BatchLogger;
            if (batchLogger == null)
                console.Log.OnMessage += OnLogMessage;
            else batchLogger.OnMessages += BatchLogger_OnMessages;
            return console.Run(args);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.ToString());
            return 666;
        }
    }

}
