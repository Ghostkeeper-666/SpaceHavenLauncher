using SH.Content.Enums;

namespace SH.Launcher.Models;

/// <summary>
/// A class for persisting application settings
/// </summary>
public sealed class AppSettingsData
{
    public static AppSettingsData GetDefault() => new()
    {
        MonitorIndex = 0,
        IsLeftPaneCollapsed = false,
        LogVerbosity = ELogVerbosity.Normal,
        ModPageSplitterHeight = 380,
        IsBackgroundEnabled = true,
        BackgroundDarkness = 0.80,
        ForceSpritesheetSize2048 = false,
        ExportXmlAnnotationLanguage = ELanguage.EN,
        ExportTextures = true,
        ExportOption = EExportOption.Both,
    };

    public AppSettingsData() { }

    public int MonitorIndex { get; set; }
    public bool IsLeftPaneCollapsed { get; set; }
    public ELogVerbosity LogVerbosity { get; set; }
    public int ModPageSplitterHeight { get; set; }
    public bool IsBackgroundEnabled { get; set; }
    public double BackgroundDarkness { get; set; }
    public bool ForceSpritesheetSize2048 { get; set; }
    public ELanguage ExportXmlAnnotationLanguage { get; set; }
    public bool ExportTextures { get; set; }
    public EExportOption ExportOption { get; set; }
}
