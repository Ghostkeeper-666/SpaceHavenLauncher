using SH.Content;
using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using System.Collections.Generic;
using System.Linq;

namespace SH.Modding.Models;

/// <summary>
/// A class for persisting path information
/// </summary>
public sealed class PathData
{
    public PathData() { }
    public PathData(PathData data) // Clone
    {
        AppDir = data.AppDir;
        WorkDir = data.WorkDir;
        SteamDir = data.SteamDir;
        SpaceHavenDir = data.SpaceHavenDir;
        SpaceHavenJarDir = data.SpaceHavenJarDir;
        SteamModsDir = data.SteamModsDir;
        ClassicModsDir = data.ClassicModsDir;
        ModValuesDir = data.ModValuesDir;
        JREPath = data.JREPath;
    }

    #region Mutable Properties

    public string AppDir { get; set; }
    public string WorkDir { get; set; }
    public string SteamDir { get; set; }
    public string SteamModsDir { get; set; }
    public string ClassicModsDir { get; set; }
    public string ModValuesDir { get; set; }
    public string SpaceHavenDir { get; set; }
    public string SpaceHavenJarDir { get; set; }
    public string JREPath { get; set; }

    #endregion Mutable Properties

    #region Derived Properties

    public string AppAspectJPath => AppDir.CombineAsOSPath(ModdingConstants.ASPECTJ);
    public string AppAspectJWeaverPath => AppDir.CombineAsOSPath(ModdingConstants.ASPECTJWEAVER);

    public string LearningDir => AppDir.CombineAsOSPath("Learning");

    public string AppLogPath => SpaceHavenLauncher.AppLogPath;
    public string ConsoleLogPath => SpaceHavenLauncher.ConsoleLogPath;
    public string LauncherAgentLogPath => CacheDir.CombineAsOSPath("LauncherAgent.log");

    public string ModListPath => WorkDir.CombineAsOSPath("mods.xml");
    public string PathSettingsPath => WorkDir.CombineAsOSPath("path.xml");
    public string ApplicationSettingsPath => WorkDir.CombineAsOSPath("app.xml");
    public string SystemInformationFilePath => WorkDir.CombineAsOSPath("system.txt");


    public static readonly string DebugFilename = "DEBUG.ZIP";
    public string DebugFilePath => WorkDir.CombineAsOSPath(DebugFilename);


    public string SpaceHavenPath
    {
        get
        {
            if (SpaceHavenDir.IsNullOrWhiteSpace())
                return null;

            switch (OS.Type)
            {
                case EOSType.Windows:
                    return SpaceHavenDir.GetFiles(ESearchOption.TopDir, equalsAny: ["spacehaven.exe"]).FirstOrDefault();
                case EOSType.Linux:
                    return SpaceHavenDir.GetFiles(ESearchOption.TopDir, equalsAny: ["spacehaven"]).FirstOrDefault();
                case EOSType.OSX:
                    return SpaceHavenDir;
                default:
                    throw new OSException();
            }
        }
    }
    public string SpaceHavenJarPath => SpaceHavenJarDir.CombineAsOSPath(SpaceHavenConstants.SPACEHAVEN_JAR);
    public string SpaceHavenModsJsonPath => SpaceHavenJarDir.CombineAsOSPath(ModdingConstants.MODS_JSON);
    public string SpaceHavenAspectJPath => SpaceHavenJarDir.CombineAsOSPath(ModdingConstants.ASPECTJ);
    public string SpaceHavenAspectJWeaverPath => SpaceHavenJarDir.CombineAsOSPath(ModdingConstants.ASPECTJWEAVER);


    public string BackupDir => WorkDir.CombineAsOSPath(ModdingConstants.BACKUP);
    public string BackupJarPath => BackupDir.CombineAsOSPath(SpaceHavenConstants.SPACEHAVEN_JAR);
    public string BackupJarHashPath => BackupDir.CombineAsOSPath(ModdingConstants.SPACEHAVENJAR_HASH);



    public string TemplateDir => WorkDir.CombineAsOSPath(ModdingConstants.TEMPLATE);
    public string TemplateStageDir => TemplateDir.CombineAsOSPath(ModdingConstants.STAGE);
    public string TemplateStageLibraryDir => TemplateStageDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY);
    public string TemplateHavenXmlPath => TemplateStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.HAVEN);
    public string TemplateTextsXmlPath => TemplateStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.TEXTS);
    public string TemplateAudioXmlPath => TemplateStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.AUDIO);
    public string TemplateTexturesXmlPath => TemplateStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.TEXTURES);
    public string TemplateAnimationsXmlPath => TemplateStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.ANIMATIONS);
    public string TemplateSpaceHavenSettingsXmlPath => TemplateStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.FILES, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);
    public string TemplateStageVersionPath => TemplateStageDir.CombineAsOSPath(SpaceHavenConstants.VERSION_TXT);
    public string TemplateExtraCreditsTxtPath => TemplateStageDir.CombineAsOSPath(SpaceHavenConstants.EXTRA_CREDITS_TXT);

    public string TemplateJarPath => TemplateDir.CombineAsOSPath(SpaceHavenConstants.SPACEHAVEN_JAR);
    public string TemplateJarHashPath => TemplateDir.CombineAsOSPath(ModdingConstants.SPACEHAVENJAR_HASH);



    public string BuildDir => WorkDir.CombineAsOSPath(ModdingConstants.BUILD);

    public string BuildXmlHashPath => BuildDir.CombineAsOSPath(ModdingConstants.XML_BUILD_HASH);
    public string BuildJavaHashPath => BuildDir.CombineAsOSPath(ModdingConstants.JAVA_BUILD_HASH);

    public string BuildLogsDir => BuildDir.CombineAsOSPath("logs");
    public string BuildLogPath => BuildDir.CombineAsOSPath("buildLog.txt");

    public string BuildTexturesDir => BuildDir.CombineAsOSPath("textures");
    public string BuildAudioDir => BuildDir.CombineAsOSPath("audio");
    public string BuildMergeDir => BuildDir.CombineAsOSPath("merge");
    public string BuildPatchDir => BuildDir.CombineAsOSPath("patch");
    public string BuildTextsDir => BuildDir.CombineAsOSPath("texts");

    public string BuildAudioFilePath => BuildAudioDir.CombineAsOSPath(SpaceHavenConstants.AUDIO);
    public string BuildTextsXmlPath => BuildTextsDir.CombineAsOSPath(SpaceHavenConstants.TEXTS);

    public string BuildStageDir => BuildDir.CombineAsOSPath(ModdingConstants.STAGE);
    public string BuildStageVersionPath => BuildStageDir.CombineAsOSPath(SpaceHavenConstants.VERSION_TXT);
    public string BuildStageLibraryDir => BuildStageDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY);

    public IReadOnlyDictionary<EXmlFileType, string> BuildStageXmlPaths => new OrderedDictionary<EXmlFileType, string>()
    {
        [EXmlFileType.Haven] = BuildStageHavenXmlPath,
        [EXmlFileType.Animations] = BuildStageAnimationsXmlPath,
        [EXmlFileType.Textures] = BuildStageTexturesXmlPath,
        [EXmlFileType.Texts] = BuildStageTextsXmlPath,
        [EXmlFileType.Audio] = BuildStageAudioXmlPath,
        [EXmlFileType.SpaceHavenSettings] = BuildStageSpaceHavenSettingsXmlPath,
    };
    public string BuildStageHavenXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.HAVEN);
    public string BuildStageTextsXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.TEXTS);
    public string BuildStageAudioXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.AUDIO);
    public string BuildStageTexturesXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.TEXTURES);
    public string BuildStageAnimationsXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.ANIMATIONS);
    public string BuildStageSpaceHavenSettingsXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.FILES, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);
    public string BuildStageStageVersionPath => BuildStageDir.CombineAsOSPath(SpaceHavenConstants.VERSION_TXT);
    public string BuildStageExtraCreditsTxtPath => BuildStageDir.CombineAsOSPath(SpaceHavenConstants.EXTRA_CREDITS_TXT);

    public string CacheDir => WorkDir.CombineAsOSPath("cache");
    public string CacheJarPath => CacheDir.CombineAsOSPath(SpaceHavenConstants.SPACEHAVEN_JAR);
    public string CacheJarHashPath => CacheDir.CombineAsOSPath(ModdingConstants.SPACEHAVENJAR_HASH);
    public string CacheModifiedJarHashPath => CacheDir.CombineAsOSPath(ModdingConstants.SPACEHAVENJAR_HASH);
    public string CacheXmlHashPath => CacheDir.CombineAsOSPath(ModdingConstants.XML_BUILD_HASH);
    public string CacheJavaHashPath => CacheDir.CombineAsOSPath(ModdingConstants.JAVA_BUILD_HASH);
    public string CacheModsJsonPath => CacheDir.CombineAsOSPath(ModdingConstants.MODS_JSON);

    public string CacheFilesDir => CacheDir.CombineAsOSPath(ModdingConstants.STAGE);
    public string CacheVersionPath => CacheDir.CombineAsOSPath(SpaceHavenConstants.VERSION_TXT);

    public string CacheLibraryDir => CacheFilesDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY);
    public string CacheLibraryFilesDir => CacheLibraryDir.CombineAsOSPath(SpaceHavenConstants.FILES);
    public string CacheHavenXmlPath => CacheLibraryDir.CombineAsOSPath(SpaceHavenConstants.HAVEN);
    public string CacheTextsXmlPath => CacheLibraryDir.CombineAsOSPath(SpaceHavenConstants.TEXTS);
    public string CacheAudioXmlPath => CacheLibraryDir.CombineAsOSPath(SpaceHavenConstants.AUDIO);
    public string CacheTexturesXmlPath => CacheLibraryDir.CombineAsOSPath(SpaceHavenConstants.TEXTURES);
    public string CacheAnimationsXmlPath => CacheLibraryDir.CombineAsOSPath(SpaceHavenConstants.ANIMATIONS);
    public string CacheSpaceHavenSettingsXmlPath => CacheLibraryFilesDir.CombineAsOSPath(SpaceHavenConstants.SPACEHAVENSETTINGS_XML);


    public string ExportDir => WorkDir.CombineAsOSPath("export");
    public string ExportOriginalFilesDir => ExportDir.CombineAsOSPath("OriginalFiles");
    public string ExportModifiedFilesDir => ExportDir.CombineAsOSPath("ModifiedFiles");
    public string ExportOriginalTexturesDir => ExportDir.CombineAsOSPath("OriginalTextures");
    public string ExportModifiedTexturesDir => ExportDir.CombineAsOSPath("ModifiedTextures");

    #endregion Derived Properties

    public List<(string, string)> GetLogReplacements()
    {
        List<(string, string)> list = [];
        if (!AppDir.IsNullOrWhiteSpace()) list.Add((SpaceHavenDir, "SPACEHAVEN_DIR"));
        if (!WorkDir.IsNullOrWhiteSpace()) list.Add((SpaceHavenDir, "SPACEHAVEN_DIR"));
        if (!SpaceHavenDir.IsNullOrWhiteSpace()) list.Add((SpaceHavenDir, "SPACEHAVEN_DIR"));
        if (!ClassicModsDir.IsNullOrWhiteSpace()) list.Add((ClassicModsDir, "CLASSIC_MODS_DIR"));
        if (!SteamModsDir.IsNullOrWhiteSpace()) list.Add((SteamModsDir, "STEAM_MODS_DIR"));
        if (!SteamDir.IsNullOrWhiteSpace()) list.Add((SteamDir, "STEAM_DIR"));
        return list;
    }

}
