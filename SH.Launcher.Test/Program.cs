using SH.Launcher.Console;
using System;

namespace SH.Launcher.Test;

internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            return ConsoleMode.Run(args);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.ToString());
            return 666;
        }
    }
}
