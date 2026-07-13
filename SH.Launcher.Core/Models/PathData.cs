using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Services;
using SH.Modding;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Models;

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

    public async Task<bool> TryReadSpaceHavenVersion(ILogger logger, CancellationToken ct)
    {
        try
        {
            VersionParserService svc = new();
            SpaceHavenVersion = await svc.TryReadVersion(TemplateStageVersionPath, logger, ct);
            return SpaceHavenVersion != null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
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
    public VersionInfo SpaceHavenVersion { get; set; }

    #endregion Mutable Properties

    #region Derived Properties

    public string AppAspectJPath => AppDir.CombineAsOSPath(ModdingConstants.ASPECTJ);
    public string AppAspectJWeaverPath => AppDir.CombineAsOSPath(ModdingConstants.ASPECTJWEAVER);

    public string LearningDir => AppDir.CombineAsOSPath("Learning");

    public string AppLogPath => WorkDir.CombineAsOSPath("appLog.txt");
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
    public string SpaceHavenModifiedJarPath => SpaceHavenJarDir.CombineAsOSPath(ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string SpaceHavenConfigJsonPath => SpaceHavenJarDir.CombineAsOSPath(SpaceHavenConstants.CONFIG_JSON);
    public string SpaceHavenModsJsonPath => SpaceHavenJarDir.CombineAsOSPath(ModdingConstants.MODS_JSON);
    public string SpaceHavenAspectJPath => SpaceHavenJarDir.CombineAsOSPath(ModdingConstants.ASPECTJ);
    public string SpaceHavenAspectJWeaverPath => SpaceHavenJarDir.CombineAsOSPath(ModdingConstants.ASPECTJWEAVER);


    public string BackupDir => WorkDir.CombineAsOSPath(ModdingConstants.BACKUP);
    public string BackupJarPath => BackupDir.CombineAsOSPath(SpaceHavenConstants.SPACEHAVEN_JAR);
    public string BackupJarHashPath => BackupDir.CombineAsOSPath(ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string BackupConfigJsonPath => BackupDir.CombineAsOSPath(SpaceHavenConstants.CONFIG_JSON);



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

    public string TemplateJarPath => TemplateDir.CombineAsOSPath(ModdingConstants.TEMPLATE_SPACEHAVEN_JAR);
    public string TemplateJarHashPath => TemplateDir.CombineAsOSPath(ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string TemplateConfigJsonPath => TemplateDir.CombineAsOSPath(SpaceHavenConstants.CONFIG_JSON);


    public string BuildDir => WorkDir.CombineAsOSPath(ModdingConstants.BUILD);

    public string BuildJarHashPath => BuildDir.CombineAsOSPath(ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string BuildXmlHashPath => BuildDir.CombineAsOSPath(ModdingConstants.XML_BUILD_HASH_TXT);
    public string BuildJavaHashPath => BuildDir.CombineAsOSPath(ModdingConstants.JAVA_BUILD_HASH_TXT);

    public string BuildLogsDir => BuildDir.CombineAsOSPath("logs");
    public string BuildTexturesDir => BuildDir.CombineAsOSPath("textures");
    public string BuildAudioDir => BuildDir.CombineAsOSPath("audio");
    public string BuildMergeDir => BuildDir.CombineAsOSPath("merge");
    public string BuildPatchDir => BuildDir.CombineAsOSPath("patch");
    public string BuildTextsDir => BuildDir.CombineAsOSPath("texts");
    public string BuildStageDir => BuildDir.CombineAsOSPath(ModdingConstants.STAGE);
    public string BuildModsJsonPath => BuildDir.CombineAsOSPath(ModdingConstants.MODS_JSON);

    public string BuildgeneratedTexturesXmlPath => BuildTexturesDir.CombineAsOSPath(SpaceHavenConstants.TEXTURES + ".xml");

    public string BuildStageVersionPath => BuildStageDir.CombineAsOSPath(SpaceHavenConstants.VERSION_TXT);

    public string BuildStageLibraryDir => BuildStageDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY);
    public string BuildStageHavenXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.HAVEN);
    public string BuildStageTextsXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.TEXTS);
    public string BuildStageAudioXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.AUDIO);
    public string BuildStageTexturesXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.TEXTURES);
    public string BuildStageAnimationsXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.ANIMATIONS);
    public string BuildStageSpaceHavenSettingsXmlPath => BuildStageLibraryDir.CombineAsOSPath(SpaceHavenConstants.FILES, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);
    public string BuildStageStageVersionPath => BuildStageDir.CombineAsOSPath(SpaceHavenConstants.VERSION_TXT);
    public string BuildStageExtraCreditsVersionPath => BuildStageDir.CombineAsOSPath("ExtraCredits.txt");

    public string CacheDir => WorkDir.CombineAsOSPath("cache");
    public string CacheJarPath => CacheDir.CombineAsOSPath(ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string CacheJarHashPath => CacheDir.CombineAsOSPath(ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string CacheModifiedJarHashPath => CacheDir.CombineAsOSPath(ModdingConstants.MODIFIED_JAR_HASH_TXT);
    public string CacheXmlHashPath => CacheDir.CombineAsOSPath(ModdingConstants.XML_BUILD_HASH_TXT);
    public string CacheJavaHashPath => CacheDir.CombineAsOSPath(ModdingConstants.JAVA_BUILD_HASH_TXT);
    public string CacheConfigJsonPath => CacheDir.CombineAsOSPath(SpaceHavenConstants.CONFIG_JSON);
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

}
