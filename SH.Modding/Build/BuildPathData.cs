using SH.Content;
using SH.Framework.IO;

namespace SH.Modding.Build;

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

    public string TemplateDirectory => IOUtils.CombineAsOSPath(WorkDir, ModdingConstants.TEMPLATE);
    public string TemplateStageDirectory => IOUtils.CombineAsOSPath(TemplateDirectory, ModdingConstants.STAGE);

    public string TemplateJarPath => IOUtils.CombineAsOSPath(TemplateDirectory, ModdingConstants.TEMPLATE_SPACEHAVEN_JAR);
    public string TemplateJarHashPath => IOUtils.CombineAsOSPath(TemplateDirectory, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string TemplateConfigJsonPath => IOUtils.CombineAsOSPath(TemplateDirectory, SpaceHavenConstants.CONFIG_JSON);
    public string TemplateExtraCreditsTxtPath => IOUtils.CombineAsOSPath(TemplateDirectory, ModdingConstants.STAGE, SpaceHavenConstants.EXTRA_CREDITS_TXT);


    public string BuildDirectory => IOUtils.CombineAsOSPath(WorkDir, ModdingConstants.BUILD);

    public string BuildJarHashPath => IOUtils.CombineAsOSPath(BuildDirectory, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string BuildXmlHashPath => IOUtils.CombineAsOSPath(BuildDirectory, ModdingConstants.XML_BUILD_HASH_TXT);
    public string BuildJavaHashPath => IOUtils.CombineAsOSPath(BuildDirectory, ModdingConstants.JAVA_BUILD_HASH_TXT);

    public string BuildLogPath => IOUtils.CombineAsOSPath(BuildDirectory, ModdingConstants.LOG_TXT);
    public string BuildLogsDirectory => IOUtils.CombineAsOSPath(BuildDirectory, "logs");
    public string BuildTexturesDirectory => IOUtils.CombineAsOSPath(BuildDirectory, "textures");
    public string BuildAudioDirectory => IOUtils.CombineAsOSPath(BuildDirectory, "audio");
    public string BuildMergeDirectory => IOUtils.CombineAsOSPath(BuildDirectory, "merge");
    public string BuildPatchDirectory => IOUtils.CombineAsOSPath(BuildDirectory, "patch");
    public string BuildTextsDirectory => IOUtils.CombineAsOSPath(BuildDirectory, "texts");
    public string BuildStageDirectory => IOUtils.CombineAsOSPath(BuildDirectory, ModdingConstants.STAGE);
    public string BuildModsJsonPath => IOUtils.CombineAsOSPath(BuildDirectory, ModdingConstants.MODS_JSON);
    public string BuildAudioFile => IOUtils.CombineAsOSPath(BuildAudioDirectory, SpaceHavenConstants.AUDIO);
    public string BuildTextsFile => IOUtils.CombineAsOSPath(BuildTextsDirectory, SpaceHavenConstants.TEXTS);

    public string BuildStageVersionPath => IOUtils.CombineAsOSPath(BuildStageDirectory, SpaceHavenConstants.VERSION_TXT);

    public string BuildStageLibraryDirectory => IOUtils.CombineAsOSPath(BuildStageDirectory, SpaceHavenConstants.LIBRARY);
    public string BuildStageLibraryFilesDirectory => IOUtils.CombineAsOSPath(BuildStageLibraryDirectory, SpaceHavenConstants.FILES);
    public string BuildStageHavenXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDirectory, SpaceHavenConstants.HAVEN);
    public string BuildStageTextsXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDirectory, SpaceHavenConstants.TEXTS);
    public string BuildStageAudioXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDirectory, SpaceHavenConstants.AUDIO);
    public string BuildStageTexturesXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDirectory, SpaceHavenConstants.TEXTURES);
    public string BuildStageAnimationsXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryDirectory, SpaceHavenConstants.ANIMATIONS);
    public string BuildStageSpaceHavenSettingsXmlPath => IOUtils.CombineAsOSPath(BuildStageLibraryFilesDirectory, SpaceHavenConstants.SPACEHAVENSETTINGS_XML);
    public string BuildStageExtraCreditsTxtPath => IOUtils.CombineAsOSPath(BuildStageDirectory, SpaceHavenConstants.EXTRA_CREDITS_TXT);



    public string CacheDirectory => IOUtils.CombineAsOSPath(WorkDir, "cache");
    public string CacheJarPath => IOUtils.CombineAsOSPath(CacheDirectory, ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string CacheJarHashPath => IOUtils.CombineAsOSPath(CacheDirectory, ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string CacheModifiedJarHashPath => IOUtils.CombineAsOSPath(CacheDirectory, ModdingConstants.MODIFIED_JAR_HASH_TXT);
    public string CacheXmlHashPath => IOUtils.CombineAsOSPath(CacheDirectory, ModdingConstants.XML_BUILD_HASH_TXT);
    public string CacheJavaHashPath => IOUtils.CombineAsOSPath(CacheDirectory, ModdingConstants.JAVA_BUILD_HASH_TXT);
    public string CacheConfigJsonPath => IOUtils.CombineAsOSPath(CacheDirectory, SpaceHavenConstants.CONFIG_JSON);
    public string CacheModsJsonPath => IOUtils.CombineAsOSPath(CacheDirectory, ModdingConstants.MODS_JSON);

    #endregion Derived Properties

}
