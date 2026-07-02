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

    public string TemplateDirectory => Path.Combine(WorkDir, ModdingConstants.TEMPLATE);
    public string TemplateStageDirectory => Path.Combine(TemplateDirectory, ModdingConstants.STAGE);

    public string TemplateJarPath => Path.Combine(TemplateDirectory, ModdingConstants.TEMPLATE_SPACEHAVEN_JAR);
    public string TemplateJarHashPath => Path.Combine(TemplateDirectory, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string TemplateConfigJsonPath => Path.Combine(TemplateDirectory, SpaceHavenConstants.CONFIG_JSON);


    public string BuildDirectory => Path.Combine(WorkDir, ModdingConstants.BUILD);

    public string BuildJarHashPath => Path.Combine(BuildDirectory, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string BuildXmlHashPath => Path.Combine(BuildDirectory, ModdingConstants.XML_BUILD_HASH_TXT);
    public string BuildJavaHashPath => Path.Combine(BuildDirectory, ModdingConstants.JAVA_BUILD_HASH_TXT);

    public string BuildLogPath => Path.Combine(BuildDirectory, ModdingConstants.LOG_TXT);
    public string BuildLogsDirectory => Path.Combine(BuildDirectory, "logs");
    public string BuildTexturesDirectory => Path.Combine(BuildDirectory, "textures");
    public string BuildAudioDirectory => Path.Combine(BuildDirectory, "audio");
    public string BuildMergeDirectory => Path.Combine(BuildDirectory, "merge");
    public string BuildPatchDirectory => Path.Combine(BuildDirectory, "patch");
    public string BuildStageDirectory => Path.Combine(BuildDirectory, ModdingConstants.STAGE);
    public string BuildModsJsonPath => Path.Combine(BuildDirectory, ModdingConstants.MODS_JSON);
    public string BuildAudioFile => Path.Combine(BuildAudioDirectory, SpaceHavenConstants.AUDIO);

    public string BuildStageVersionPath => Path.Combine(BuildStageDirectory, SpaceHavenConstants.VERSION_TXT);

    public string BuildStageLibraryDirectory => Path.Combine(BuildStageDirectory, SpaceHavenConstants.LIBRARY);
    public string BuildStageLibraryFilesDirectory => Path.Combine(BuildStageLibraryDirectory, SpaceHavenConstants.FILES);
    public string BuildStageHavenXmlPath => Path.Combine(BuildStageLibraryDirectory, SpaceHavenConstants.HAVEN);
    public string BuildStageTextsXmlPath => Path.Combine(BuildStageLibraryDirectory, SpaceHavenConstants.TEXTS);
    public string BuildStageAudioXmlPath => Path.Combine(BuildStageLibraryDirectory, SpaceHavenConstants.AUDIO);
    public string BuildStageTexturesXmlPath => Path.Combine(BuildStageLibraryDirectory, SpaceHavenConstants.TEXTURES);
    public string BuildStageAnimationsXmlPath => Path.Combine(BuildStageLibraryDirectory, SpaceHavenConstants.ANIMATIONS);
    public string BuildStageSpaceHavenSettingsXmlPath => Path.Combine(BuildStageLibraryFilesDirectory, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);



    public string CacheDirectory => Path.Combine(WorkDir, "cache");
    public string CacheJarPath => Path.Combine(CacheDirectory, ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string CacheJarHashPath => Path.Combine(CacheDirectory, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string CacheModifiedJarHashPath => Path.Combine(CacheDirectory, ModdingConstants.MODIFIED_JAR_HASH_TXT);
    public string CacheXmlHashPath => Path.Combine(CacheDirectory, ModdingConstants.XML_BUILD_HASH_TXT);
    public string CacheJavaHashPath => Path.Combine(CacheDirectory, ModdingConstants.JAVA_BUILD_HASH_TXT);
    public string CacheConfigJsonPath => Path.Combine(CacheDirectory, SpaceHavenConstants.CONFIG_JSON);
    public string CacheModsJsonPath => Path.Combine(CacheDirectory, ModdingConstants.MODS_JSON);

    #endregion Derived Properties

}
