using SH.Framework.Logging;
using System;
using System.Threading.Tasks;

namespace SH.Content.Test;

public sealed class Program
{
    private static void Logger_OnMessage(object sender, LogMessage e) =>
        Console.WriteLine(e.Text);

    public static async Task Main()
    {
        //await RunAsync();
    }

    public static  async Task RunAsync()
    {
        bool success = true;

        Logger logger = new();
        logger.OnMessage += Logger_OnMessage;

        LibraryTest lib = new(logger);
        success &= await lib.TryRunAsync();

        if (success)
            Console.WriteLine(">>>>>>>>>> Program ended SUCCESSFULLY <<<<<<<<<<");
        else
            Console.WriteLine(">>>>>>>>>> Program execution FAILED! <<<<<<<<<<");
    }

}
