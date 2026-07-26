using SH.Content.Enums;
using SH.Framework.IO;
using SH.Modding.Models;

namespace SH.Launcher.Core.Models;

/// <summary>
/// A class for persisting application settings
/// </summary>
public sealed class AppSettingsData
{
    public static AppSettingsData GetDefault() => new()
    {
        PreviousAppVersion = SpaceHavenLauncher.Version,
        MonitorIndex = 0,
        IsLeftPaneCollapsed = false,
        LogVerbosity = ELogVerbosity.Normal,
        ModPageSplitterHeight = 380,
        IsBackgroundEnabled = true,
        BackgroundDarkness = 0.80,
        SkipRebuilding = true,
        StartSpaceHavenAutomatically = true,
        CloseAppAutomaticallyOnLaunch = false,
        ExportXmlAnnotationLanguage = ELanguage.EN,
        ExportTextures = true,
        ExportOption = EExportOption.Both,
        JavaVMArgs = string.Empty,
        JavaMainClass = string.Empty,
    };

    public AppSettingsData() { }

    public VersionInfo PreviousAppVersion { get; internal set; }
    public int MonitorIndex { get; set; }
    public bool IsLeftPaneCollapsed { get; set; }
    public ELogVerbosity LogVerbosity { get; set; }
    public int ModPageSplitterHeight { get; set; }
    public bool IsBackgroundEnabled { get; set; }
    public double BackgroundDarkness { get; set; }
    public bool StartSpaceHavenAutomatically { get; set; }
    public bool CloseAppAutomaticallyOnLaunch { get; set; }
    public bool SkipRebuilding { get; set; }
    public ELanguage ExportXmlAnnotationLanguage { get; set; }
    public bool ExportTextures { get; set; }
    public EExportOption ExportOption { get; set; }
    public string JavaVMArgs { get; set; }
    public string JavaMainClass { get; set; }
}
