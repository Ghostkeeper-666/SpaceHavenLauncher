using System.IO;

namespace SH.Content.Modding.Build;

public sealed class BuildPathData
{
    public BuildPathData() { }

    #region Configurable Properties
    public string AppDir { get; set; }
    public string WorkDir { get; set; }
    public string SpaceHavenDir { get; set; }
    public string SpaceHavenJarDir { get; set; }

    #endregion Configurable Properties


    #region Derived Properties

    public string TemplateDir => Path.Combine(WorkDir, ModdingConstants.TEMPLATE);
    public string TemplateStageDir => Path.Combine(TemplateDir, ModdingConstants.STAGE);

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

    #endregion Derived Properties

}
