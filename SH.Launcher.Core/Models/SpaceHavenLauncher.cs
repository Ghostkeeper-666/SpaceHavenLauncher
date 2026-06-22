using SH.Framework.IO;
using System;
using System.IO;
using System.Reflection;

namespace SH.Launcher.Core.Models;

public static class SpaceHavenLauncher
{
    static SpaceHavenLauncher()
    {
        Version = new(Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "0");
        Directory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
    }

    public static readonly string AssemblyName = "SpaceHavenLauncher";
    public static readonly string Name = "Space Haven Launcher";
    public static readonly VersionInfo Version;
    public static readonly string Directory;
}
