using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SH.Framework.Test;

internal class Program
{
    private static void Logger_OnMessage(object sender, LogMessage e) =>
        Console.WriteLine(e.Text);

    private static async Task Main()
    {
        //await Run();
    }

    private static async Task Run()
    {
        Logger logger = new();
        logger.OnMessage += Logger_OnMessage;

        Stopwatch sw = Stopwatch.StartNew();

        string localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string laucherDir = Path.Combine(localAppDataDir, @"SpaceHavenLauncher");
        string templateSpaceHavenJar = Path.Combine(laucherDir, @"template\spacehaven.jar");
        string modifiedSpaceHavenJar = Path.Combine(laucherDir, @"modified\spacehaven.jar");
        string buildStageDir = Path.Combine(laucherDir, @"build\output");
        DirectoryInfo di = new(buildStageDir);
        FileInfo[] fis = di.GetFiles("*.*", SearchOption.AllDirectories);
        JarAppender j = new();
        await j.AppendTo(templateSpaceHavenJar, modifiedSpaceHavenJar, buildStageDir, fis, logger, null);

        sw.Stop();

        Console.WriteLine($"Done in {sw.Elapsed.TotalMilliseconds} ms");
    }


}
