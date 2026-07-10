using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Services;
using SH.Modding;
using System;
using System.Collections.Generic;
using System.IO;
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

    #region Configurable Properties
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

    #endregion Configurable Properties


    #region Derived Properties


    public string AppAspectJPath => IOUtils.CombineAsOSPath(AppDir, ModdingConstants.ASPECTJ);
    public string AppAspectJWeaverPath => IOUtils.CombineAsOSPath(AppDir, ModdingConstants.ASPECTJWEAVER);

    public string LearningDir => IOUtils.CombineAsOSPath(AppDir, "Learning");

    public string AppLogPath => IOUtils.CombineAsOSPath(WorkDir, "appLog.txt");
    public string ModListPath => IOUtils.CombineAsOSPath(WorkDir, "mods.xml");
    public string PathSettingsPath => IOUtils.CombineAsOSPath(WorkDir, "path.xml");
    public string ApplicationSettingsPath => IOUtils.CombineAsOSPath(WorkDir, "app.xml");
    public string SystemInformationFilePath => IOUtils.CombineAsOSPath(WorkDir, "system.txt");

    public static readonly string DebugFilename = "DEBUG.ZIP";
    public string DebugFilePath => IOUtils.CombineAsOSPath(WorkDir, DebugFilename);


    public string SpaceHavenPath
    {
        get
        {
            if (SpaceHavenDir.IsNullOrWhiteSpace())
                return null;

            switch (OS.Type)
            {
                case EOSType.Windows:
                    return IOUtils.CombineAsOSPath(SpaceHavenJarDir, "spacehaven.exe");
                case EOSType.Linux:
                    return IOUtils.CombineAsOSPath(SpaceHavenJarDir, "spacehaven");
                case EOSType.OSX:
                    return Directory.GetDirectories(SpaceHavenDir, "*.app", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(dir => dir.Equals("spacehaven.app", StringComparison.OrdinalIgnoreCase));
                default:
                    throw new NotImplementedException($"Unknown operational system");
            }
        }
    }
    public string SpaceHavenJarPath => IOUtils.CombineAsOSPath(SpaceHavenJarDir, SpaceHavenConstants.SPACEHAVEN_JAR);
    public string SpaceHavenModifiedJarPath => IOUtils.CombineAsOSPath(SpaceHavenJarDir, ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string SpaceHavenConfigJsonPath => IOUtils.CombineAsOSPath(SpaceHavenJarDir, SpaceHavenConstants.CONFIG_JSON);
    public string SpaceHavenModsJsonPath => IOUtils.CombineAsOSPath(SpaceHavenJarDir, ModdingConstants.MODS_JSON);
    public string SpaceHavenAspectJPath => IOUtils.CombineAsOSPath(SpaceHavenJarDir, ModdingConstants.ASPECTJ);
    public string SpaceHavenAspectJWeaverPath => IOUtils.CombineAsOSPath(SpaceHavenJarDir, ModdingConstants.ASPECTJWEAVER);


    public string BackupDir => IOUtils.CombineAsOSPath(WorkDir, ModdingConstants.BACKUP);
    public string BackupJarPath => IOUtils.CombineAsOSPath(BackupDir, SpaceHavenConstants.SPACEHAVEN_JAR);
    public string BackupJarHashPath => IOUtils.CombineAsOSPath(BackupDir, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string BackupConfigJsonPath => IOUtils.CombineAsOSPath(BackupDir, SpaceHavenConstants.CONFIG_JSON);



    public string TemplateDir => IOUtils.CombineAsOSPath(WorkDir, ModdingConstants.TEMPLATE);
    public string TemplateStageDir => IOUtils.CombineAsOSPath(TemplateDir, ModdingConstants.STAGE);
    public string TemplateStageLibraryDir => IOUtils.CombineAsOSPath(TemplateStageDir, SpaceHavenConstants.LIBRARY);
    public string TemplateHavenXmlPath => IOUtils.CombineAsOSPath(TemplateStageLibraryDir, SpaceHavenConstants.HAVEN);
    public string TemplateTextsXmlPath => IOUtils.CombineAsOSPath(TemplateStageLibraryDir, SpaceHavenConstants.TEXTS);
    public string TemplateAudioXmlPath => IOUtils.CombineAsOSPath(TemplateStageLibraryDir, SpaceHavenConstants.AUDIO);
    public string TemplateTexturesXmlPath => IOUtils.CombineAsOSPath(TemplateStageLibraryDir, SpaceHavenConstants.TEXTURES);
    public string TemplateAnimationsXmlPath => IOUtils.CombineAsOSPath(TemplateStageLibraryDir, SpaceHavenConstants.ANIMATIONS);
    public string TemplateSpaceHavenSettingsXmlPath => IOUtils.CombineAsOSPath(TemplateStageLibraryDir, SpaceHavenConstants.FILES, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);
    public string TemplateStageVersionPath => IOUtils.CombineAsOSPath(TemplateStageDir, SpaceHavenConstants.VERSION_TXT);

    public string TemplateJarPath => IOUtils.CombineAsOSPath(TemplateDir, ModdingConstants.TEMPLATE_SPACEHAVEN_JAR);
    public string TemplateJarHashPath => IOUtils.CombineAsOSPath(TemplateDir, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string TemplateConfigJsonPath => IOUtils.CombineAsOSPath(TemplateDir, SpaceHavenConstants.CONFIG_JSON);


    public string BuildDir => IOUtils.CombineAsOSPath(WorkDir, ModdingConstants.BUILD);

    public string BuildJarHashPath => IOUtils.CombineAsOSPath(BuildDir, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string BuildXmlHashPath => IOUtils.CombineAsOSPath(BuildDir, ModdingConstants.XML_BUILD_HASH_TXT);
    public string BuildJavaHashPath => IOUtils.CombineAsOSPath(BuildDir, ModdingConstants.JAVA_BUILD_HASH_TXT);

    public string BuildLogsDir => IOUtils.CombineAsOSPath(BuildDir, "logs");
    public string BuildTexturesDir => IOUtils.CombineAsOSPath(BuildDir, "textures");
    public string BuildAudioDir => IOUtils.CombineAsOSPath(BuildDir, "audio");
    public string BuildMergeDir => IOUtils.CombineAsOSPath(BuildDir, "merge");
    public string BuildPatchDir => IOUtils.CombineAsOSPath(BuildDir, "patch");
    public string BuildTextsDir => IOUtils.CombineAsOSPath(BuildDir, "texts");
    public string BuildStageDir => IOUtils.CombineAsOSPath(BuildDir, ModdingConstants.STAGE);
    public string BuildModsJsonPath => IOUtils.CombineAsOSPath(BuildDir, ModdingConstants.MODS_JSON);

    public string BuildgeneratedTexturesXmlPath => IOUtils.CombineAsOSPath(BuildTexturesDir, SpaceHavenConstants.TEXTURES + ".xml");

    public string BuildStageVersionPath => IOUtils.CombineAsOSPath(BuildStageDir, SpaceHavenConstants.VERSION_TXT);

    public string BuildStageLibraryDir => IOUtils.CombineAsOSPath(BuildStageDir, SpaceHavenConstants.LIBRARY);
    public string BuildStageHavenXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDir, SpaceHavenConstants.HAVEN);
    public string BuildStageTextsXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDir, SpaceHavenConstants.TEXTS);
    public string BuildStageAudioXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDir, SpaceHavenConstants.AUDIO);
    public string BuildStageTexturesXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDir, SpaceHavenConstants.TEXTURES);
    public string BuildStageAnimationsXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDir, SpaceHavenConstants.ANIMATIONS);
    public string BuildStageSpaceHavenSettingsXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDir, SpaceHavenConstants.FILES, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);
    public string BuildStageStageVersionPath => IOUtils.CombineAsOSPath(BuildStageDir, SpaceHavenConstants.VERSION_TXT);
    public string BuildStageExtraCreditsVersionPath => IOUtils.CombineAsOSPath(BuildStageDir, "ExtraCredits.txt");

    public string CacheDir => IOUtils.CombineAsOSPath(WorkDir, "cache");
    public string CacheJarPath => IOUtils.CombineAsOSPath(CacheDir, ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string CacheJarHashPath => IOUtils.CombineAsOSPath(CacheDir, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string CacheModifiedJarHashPath => IOUtils.CombineAsOSPath(CacheDir, ModdingConstants.MODIFIED_JAR_HASH_TXT);
    public string CacheXmlHashPath => IOUtils.CombineAsOSPath(CacheDir, ModdingConstants.XML_BUILD_HASH_TXT);
    public string CacheJavaHashPath => IOUtils.CombineAsOSPath(CacheDir, ModdingConstants.JAVA_BUILD_HASH_TXT);
    public string CacheConfigJsonPath => IOUtils.CombineAsOSPath(CacheDir, SpaceHavenConstants.CONFIG_JSON);
    public string CacheModsJsonPath => IOUtils.CombineAsOSPath(CacheDir, ModdingConstants.MODS_JSON);

    public string CacheFilesDir => IOUtils.CombineAsOSPath(CacheDir, ModdingConstants.STAGE);
    public string CacheVersionPath => IOUtils.CombineAsOSPath(CacheDir, SpaceHavenConstants.VERSION_TXT);

    public string CacheLibraryDir => IOUtils.CombineAsOSPath(CacheFilesDir, SpaceHavenConstants.LIBRARY);
    public string CacheLibraryFilesDir => IOUtils.CombineAsOSPath(CacheLibraryDir, SpaceHavenConstants.FILES);
    public string CacheHavenXmlPath => IOUtils.CombineAsOSPath(CacheLibraryDir, SpaceHavenConstants.HAVEN);
    public string CacheTextsXmlPath => IOUtils.CombineAsOSPath(CacheLibraryDir, SpaceHavenConstants.TEXTS);
    public string CacheAudioXmlPath => IOUtils.CombineAsOSPath(CacheLibraryDir, SpaceHavenConstants.AUDIO);
    public string CacheTexturesXmlPath => IOUtils.CombineAsOSPath(CacheLibraryDir, SpaceHavenConstants.TEXTURES);
    public string CacheAnimationsXmlPath => IOUtils.CombineAsOSPath(CacheLibraryDir, SpaceHavenConstants.ANIMATIONS);
    public string CacheSpaceHavenSettingsXmlPath => IOUtils.CombineAsOSPath(CacheLibraryFilesDir, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);



    public string ExportDir => IOUtils.CombineAsOSPath(WorkDir, "export");
    public string ExportOriginalFilesDir => IOUtils.CombineAsOSPath(ExportDir, "OriginalFiles");
    public string ExportModifiedFilesDir => IOUtils.CombineAsOSPath(ExportDir, "ModifiedFiles");
    public string ExportOriginalTexturesDir => IOUtils.CombineAsOSPath(ExportDir, "OriginalTextures");
    public string ExportModifiedTexturesDir => IOUtils.CombineAsOSPath(ExportDir, "ModifiedTextures");

    #endregion Derived Properties


    public List<string> ModRootDirectories
    {
        get
        {
            List<string> list = new();
            if (SteamModsDir != null) list.Add(SteamModsDir);
            if (ClassicModsDir != null) list.Add(ClassicModsDir);
            return list;
        }
    }



}
