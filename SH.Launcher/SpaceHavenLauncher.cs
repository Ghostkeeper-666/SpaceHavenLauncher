using SH.Framework.IO;
using System;
using System.IO;

namespace SH.Launcher;

internal static class SpaceHavenLauncher
{
    public static readonly string ASSEMBLY_NAME = "SpaceHavenLauncher";
    public static readonly string APP_NAME = "Space Haven Launcher";
    public static VersionInfo GetAppVersion() => new(typeof(Program)?.Assembly?.GetName()?.Version?.ToString() ?? "0");
    public static string GetAppDir() => Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
}
