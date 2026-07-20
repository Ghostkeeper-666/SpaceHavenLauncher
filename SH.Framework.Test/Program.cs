using SH.Framework.Logging;
using System;
using System.Threading.Tasks;

namespace SH.Framework.Test;

internal static class Program
{
    private static void OnLogMessage(object sender, LogMessage e) =>
        Console.WriteLine($"[{e.Level.ToString().ToUpperInvariant()}]    {e?.Text}");

    private static async Task Main()
    {
        await Run();
    }

    private static async Task Run()
    {
        Logger logger = new();
        logger.OnMessage += OnLogMessage;



    }


}
