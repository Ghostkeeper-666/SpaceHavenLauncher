using SH.Framework.IO;
using System;
using System.Reflection;

namespace SH.Modding.Models;

public static class SpaceHavenLauncher
{
    static SpaceHavenLauncher()
    {
        Version = new(Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "0");
        AppDir = AppContext.BaseDirectory.AsOSPath();
        WorkDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).CombineAsOSPath("SpaceHavenLauncher");
        AppLogPath = WorkDir.CombineAsOSPath("AppLog.txt");
        ConsoleLogPath = WorkDir.CombineAsOSPath("ConsoleLog.txt");
    }

    
    public static readonly string AssemblyName = "SpaceHavenLauncher";
    public static readonly string Name = "Space Haven Launcher";
    public static readonly VersionInfo Version;
    public static readonly string AppDir;
    public static readonly string WorkDir;
    public static readonly string AppLogPath;
    public static readonly string ConsoleLogPath;
}
