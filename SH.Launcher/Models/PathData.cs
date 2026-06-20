using SH.Content;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Models;

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
    }

    public async Task<bool> TryReadSpaceHavenVersion(ILogger logger, CancellationToken ct)
    {
        try
        {
            VersionParserService svc = new();
            SpaceHavenVersion = await svc.TryReadVersion(TemplateVersionPath, logger, ct);
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
    public string SpaceHavenDir { get; set; }
    public string SpaceHavenJarDir { get; set; }
    public string SteamModsDir { get; set; }
    public string ClassicModsDir { get; set; }
    public string ModValuesDir { get; set; }
    public VersionInfo SpaceHavenVersion { get; set; }

    #endregion Configurable Properties


    #region Derived Properties

    public string AppAspectJPath => Path.Combine(AppDir, ModdingConstants.ASPECTJ);
    public string AppAspectJWeaverPath => Path.Combine(AppDir, ModdingConstants.ASPECTJWEAVER);

    public string LearningDir => Path.Combine(AppDir, "Learning");

    public string ModListPath => Path.Combine(WorkDir, "mods.xml");
    public string PathSettingsPath => Path.Combine(WorkDir, "path.xml");
    public string ApplicationSettingsPath => Path.Combine(WorkDir, "app.xml");



    public string SpaceHavenPath =>
        SpaceHavenDir == null ? null :
        OS.IsWin ? Path.Combine(SpaceHavenJarDir, "spacehaven.exe") :
        OS.IsLnx ? Path.Combine(SpaceHavenJarDir, "spacehaven") :
        OS.IsMac ? SpaceHavenDir :
        throw new NotImplementedException($"Unknown operational system");
    public string SpaceHavenJarPath => Path.Combine(SpaceHavenJarDir, SpaceHavenConstants.SPACEHAVEN_JAR);
    public string SpaceHavenModifiedJarPath => Path.Combine(SpaceHavenJarDir, ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string SpaceHavenConfigJsonPath => Path.Combine(SpaceHavenJarDir, SpaceHavenConstants.CONFIG_JSON);
    public string SpaceHavenModsJsonPath => Path.Combine(SpaceHavenJarDir, ModdingConstants.MODS_JSON);
    public string SpaceHavenAspectJPath => Path.Combine(SpaceHavenJarDir, ModdingConstants.ASPECTJ);
    public string SpaceHavenAspectJWeaverPath => Path.Combine(SpaceHavenJarDir, ModdingConstants.ASPECTJWEAVER);


    public string BackupDir => Path.Combine(WorkDir, ModdingConstants.BACKUP);
    public string BackupJarPath => Path.Combine(BackupDir, SpaceHavenConstants.SPACEHAVEN_JAR);
    public string BackupJarHashPath => Path.Combine(BackupDir, ModdingConstants.ORIGINAL_JAR_HASH_TXT);



    public string TemplateDir => Path.Combine(WorkDir, ModdingConstants.TEMPLATE);
    public string TemplateStageDir => Path.Combine(TemplateDir, ModdingConstants.STAGE);
    public string TemplateHavenXmlPath => Path.Combine(TemplateStageDir, SpaceHavenConstants.LIBRARY, SpaceHavenConstants.HAVEN);
    public string TemplateTextsXmlPath => Path.Combine(TemplateStageDir, SpaceHavenConstants.LIBRARY, SpaceHavenConstants.TEXTS);
    public string TemplateAudioXmlPath => Path.Combine(TemplateStageDir, SpaceHavenConstants.LIBRARY, SpaceHavenConstants.AUDIO);
    public string TemplateTexturesXmlPath => Path.Combine(TemplateStageDir, SpaceHavenConstants.LIBRARY, SpaceHavenConstants.TEXTURES);
    public string TemplateAnimationsXmlPath => Path.Combine(TemplateStageDir, SpaceHavenConstants.LIBRARY, SpaceHavenConstants.ANIMATIONS);
    public string TemplateSpaceHavenSettingsXmlPath => Path.Combine(TemplateStageDir, SpaceHavenConstants.LIBRARY, SpaceHavenConstants.ANIMATIONS);

    public string TemplateLibraryDir => Path.Combine(TemplateStageDir, SpaceHavenConstants.LIBRARY);
    public string TemplateVersionPath => Path.Combine(TemplateStageDir, SpaceHavenConstants.VERSION_TXT);
    public string TemplateJarPath => Path.Combine(TemplateDir, ModdingConstants.TEMPLATE_SPACEHAVEN_JAR);
    public string TemplateJarHashPath => Path.Combine(TemplateDir, ModdingConstants.ORIGINAL_JAR_HASH_TXT);


    public string BuildDir => Path.Combine(WorkDir, ModdingConstants.BUILD);

    public string BuildJarHashPath => Path.Combine(BuildDir, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string BuildXmlHashPath => Path.Combine(BuildDir, ModdingConstants.XML_BUILD_HASH_TXT);
    public string BuildJavaHashPath => Path.Combine(BuildDir, ModdingConstants.JAVA_BUILD_HASH_TXT);

    public string BuildLogPath => Path.Combine(BuildDir, ModdingConstants.LOG_TXT);
    public string BuildLogsDir => Path.Combine(BuildDir, "logs");
    public string BuildTexturesDir => Path.Combine(BuildDir, "textures");
    public string BuildAudioDir => Path.Combine(BuildDir, "audio");
    public string BuildMergeDir => Path.Combine(BuildDir, "merge");
    public string BuildPatchDir => Path.Combine(BuildDir, "patch");
    public string BuildStageDir => Path.Combine(BuildDir, ModdingConstants.STAGE);
    public string BuildModsJsonPath => Path.Combine(BuildDir, ModdingConstants.MODS_JSON);

    public string BuildgeneratedTexturesXmlPath => Path.Combine(BuildTexturesDir, SpaceHavenConstants.TEXTURES + ".xml");

    public string BuildStageVersionPath => Path.Combine(BuildStageDir, SpaceHavenConstants.VERSION_TXT);

    public string BuildStageLibraryDir => Path.Combine(BuildStageDir, SpaceHavenConstants.LIBRARY);
    public string BuildStageLibraryFilesDir => Path.Combine(BuildStageLibraryDir, SpaceHavenConstants.FILES);
    public string BuildStageHavenXmlPath => Path.Combine(BuildStageLibraryDir, SpaceHavenConstants.HAVEN);
    public string BuildStageTextsXmlPath => Path.Combine(BuildStageLibraryDir, SpaceHavenConstants.TEXTS);
    public string BuildStageAudioXmlPath => Path.Combine(BuildStageLibraryDir, SpaceHavenConstants.AUDIO);
    public string BuildStageTexturesXmlPath => Path.Combine(BuildStageLibraryDir, SpaceHavenConstants.TEXTURES);
    public string BuildStageAnimationsXmlPath => Path.Combine(BuildStageLibraryDir, SpaceHavenConstants.ANIMATIONS);
    public string BuildStageSpaceHavenSettingsXmlPath => Path.Combine(BuildStageLibraryFilesDir, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);



    public string CacheDir => Path.Combine(WorkDir, "cache");
    public string CacheJarPath => Path.Combine(CacheDir, ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string CacheJarHashPath => Path.Combine(CacheDir, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string CacheModifiedJarHashPath => Path.Combine(CacheDir, ModdingConstants.MODIFIED_JAR_HASH_TXT);
    public string CacheXmlHashPath => Path.Combine(CacheDir, ModdingConstants.XML_BUILD_HASH_TXT);
    public string CacheJavaHashPath => Path.Combine(CacheDir, ModdingConstants.JAVA_BUILD_HASH_TXT);
    public string CacheConfigJsonPath => Path.Combine(CacheDir, SpaceHavenConstants.CONFIG_JSON);
    public string CacheModsJsonPath => Path.Combine(CacheDir, ModdingConstants.MODS_JSON);

    public string CacheFilesDir => Path.Combine(CacheDir, ModdingConstants.STAGE);
    public string CacheVersionPath => Path.Combine(CacheDir, SpaceHavenConstants.LIBRARY);

    public string CacheLibraryDir => Path.Combine(CacheFilesDir, SpaceHavenConstants.LIBRARY);
    public string CacheLibraryFilesDir => Path.Combine(CacheLibraryDir, SpaceHavenConstants.FILES);
    public string CacheHavenXmlPath => Path.Combine(CacheLibraryDir, SpaceHavenConstants.HAVEN);
    public string CacheTextsXmlPath => Path.Combine(CacheLibraryDir, SpaceHavenConstants.TEXTS);
    public string CacheAudioXmlPath => Path.Combine(CacheLibraryDir, SpaceHavenConstants.AUDIO);
    public string CacheTexturesXmlPath => Path.Combine(CacheLibraryDir, SpaceHavenConstants.TEXTURES);
    public string CacheAnimationsXmlPath => Path.Combine(CacheLibraryDir, SpaceHavenConstants.ANIMATIONS);
    public string CacheSpaceHavenSettingsXmlPath => Path.Combine(CacheLibraryFilesDir, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);



    public string ExportDir => Path.Combine(WorkDir, "export");
    public string ExportOriginalDir => Path.Combine(ExportDir, "original");
    public string ExportModifiedDir => Path.Combine(ExportDir, "modified");

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
