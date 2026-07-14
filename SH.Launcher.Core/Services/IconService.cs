using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using ShellLink;
using System;

namespace SH.Launcher.Core.Services;

public sealed class IconService
{
    private readonly ILogger Log;

    public IconService(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    public void CreateSpaceHavenLauncherWindowsDesktopIcon()
    {
        try
        {
            if (!OS.IsWin)
                return;

            Shortcut shortcut = Shortcut.CreateShortcut(
                path: SpaceHavenLauncher.Directory.CombineAsEvaluatedOSPath($"{SpaceHavenLauncher.AssemblyName}.exe"),
                args: null,
                workdir: SpaceHavenLauncher.Directory,
                iconpath: SpaceHavenLauncher.Directory.CombineAsEvaluatedOSPath($"{SpaceHavenLauncher.AssemblyName}.exe"),
                iconindex: 0
            );

            shortcut.WriteToFile(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
                .CombineAsEvaluatedOSPath($"{SpaceHavenLauncher.Name}.lnk"));
        }
        catch (Exception ex)
        {
            Log?.Error($"Unable to create {SpaceHavenLauncher.Name} icon: {ex}");
        }
    }

}
