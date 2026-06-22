using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using ShellLink;
using System;
using System.IO;

namespace SH.Launcher.Core.Services;

public sealed class IconService
{
    private readonly ILogger Log;

    public IconService(ILogger logger) =>
        Log = logger ?? new VoidLogger();

    public void CreateSpaceHavenLauncherWindowsDesktopIcon()
    {
        try
        {
            if (!OS.IsWin)
                return;
            
            Shortcut shortcut = Shortcut.CreateShortcut(
                path: Path.Combine(SpaceHavenLauncher.Directory, $"{SpaceHavenLauncher.AssemblyName}.exe"),
                args: null,
                workdir: SpaceHavenLauncher.Directory,
                iconpath: Path.Combine(SpaceHavenLauncher.Directory, $"{SpaceHavenLauncher.AssemblyName}.exe"),
                iconindex: 0
            );

            shortcut.WriteToFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{SpaceHavenLauncher.Name}.lnk"));
        }
        catch (Exception ex)
        {
            Log?.Error($"Unable to create {SpaceHavenLauncher.Name} icon: {ex}");
        }
    }

}
