using SH.Content;
using SH.Framework.IO;
using System;

namespace SH.Modding.Build;

internal sealed class BuildPathData
{
    public BuildPathData(string appDir, string workDir, string spaceHavenDir, string spaceHavenJarDir)
    {
        AppDir = appDir ?? throw new ArgumentNullException(nameof(appDir));
        WorkDir = workDir ?? throw new ArgumentNullException(nameof(workDir));
        SpaceHavenDir = spaceHavenDir ?? throw new ArgumentNullException(nameof(spaceHavenDir));
        SpaceHavenJarDir = spaceHavenJarDir ?? throw new ArgumentNullException(nameof(spaceHavenJarDir));
    }

    #region Primary Properties

    public string AppDir { get; set; }
    public string WorkDir { get; set; }
    public string SpaceHavenDir { get; set; }
    public string SpaceHavenJarDir { get; set; }

    #endregion Primary Properties


    #region Derived Properties

    public string TemplateDirectory => WorkDir.CombineAsOSPath(ModdingConstants.TEMPLATE);
    public string TemplateStageDirectory => TemplateDirectory.CombineAsOSPath(ModdingConstants.STAGE);

    public string TemplateJarPath => TemplateDirectory.CombineAsOSPath(ModdingConstants.TEMPLATE_SPACEHAVEN_JAR);
    public string TemplateJarHashPath => TemplateDirectory.CombineAsOSPath(ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string TemplateConfigJsonPath => TemplateDirectory.CombineAsOSPath(SpaceHavenConstants.CONFIG_JSON);
    public string TemplateExtraCreditsTxtPath => TemplateDirectory.CombineAsOSPath(ModdingConstants.STAGE, SpaceHavenConstants.EXTRA_CREDITS_TXT);


    public string BuildDirectory => WorkDir.CombineAsOSPath(ModdingConstants.BUILD);

    public string BuildJarHashPath => BuildDirectory.CombineAsOSPath(ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string BuildXmlHashPath => BuildDirectory.CombineAsOSPath(ModdingConstants.XML_BUILD_HASH_TXT);
    public string BuildJavaHashPath => BuildDirectory.CombineAsOSPath(ModdingConstants.JAVA_BUILD_HASH_TXT);

    public string BuildLogPath => BuildDirectory.CombineAsOSPath("buildLog.txt");
    public string BuildLogsDirectory => BuildDirectory.CombineAsOSPath("logs");
    public string BuildTexturesDirectory => BuildDirectory.CombineAsOSPath("textures");
    public string BuildAudioDirectory => BuildDirectory.CombineAsOSPath("audio");
    public string BuildMergeDirectory => BuildDirectory.CombineAsOSPath("merge");
    public string BuildPatchDirectory => BuildDirectory.CombineAsOSPath("patch");
    public string BuildTextsDirectory => BuildDirectory.CombineAsOSPath("texts");
    public string BuildStageDirectory => BuildDirectory.CombineAsOSPath(ModdingConstants.STAGE);
    public string BuildModsJsonPath => BuildDirectory.CombineAsOSPath(ModdingConstants.MODS_JSON);
    public string BuildAudioFile => BuildAudioDirectory.CombineAsOSPath(SpaceHavenConstants.AUDIO);
    public string BuildTextsFile => BuildTextsDirectory.CombineAsOSPath(SpaceHavenConstants.TEXTS);

    public string BuildStageVersionPath => BuildStageDirectory.CombineAsOSPath(SpaceHavenConstants.VERSION_TXT);

    public string BuildStageLibraryDirectory => BuildStageDirectory.CombineAsOSPath(SpaceHavenConstants.LIBRARY);
    public string BuildStageLibraryFilesDirectory => BuildStageLibraryDirectory.CombineAsOSPath(SpaceHavenConstants.FILES);
    public string BuildStageHavenXmlPath => BuildStageLibraryDirectory.CombineAsOSPath(SpaceHavenConstants.HAVEN);
    public string BuildStageTextsXmlPath => BuildStageLibraryDirectory.CombineAsOSPath(SpaceHavenConstants.TEXTS);
    public string BuildStageAudioXmlPath => BuildStageLibraryDirectory.CombineAsOSPath(SpaceHavenConstants.AUDIO);
    public string BuildStageTexturesXmlPath => BuildStageLibraryDirectory.CombineAsOSPath(SpaceHavenConstants.TEXTURES);
    public string BuildStageAnimationsXmlPath => BuildStageLibraryDirectory.CombineAsOSPath(SpaceHavenConstants.ANIMATIONS);
    public string BuildStageSpaceHavenSettingsXmlPath => BuildStageLibraryFilesDirectory.CombineAsOSPath(SpaceHavenConstants.SPACEHAVENSETTINGS_XML);
    public string BuildStageExtraCreditsTxtPath => BuildStageDirectory.CombineAsOSPath(SpaceHavenConstants.EXTRA_CREDITS_TXT);



    public string CacheDirectory => WorkDir.CombineAsOSPath("cache");
    public string CacheJarPath => CacheDirectory.CombineAsOSPath(ModdingConstants.MODIFIED_SPACEHAVEN_JAR);
    public string CacheJarHashPath => CacheDirectory.CombineAsOSPath(ModdingConstants.ORIGINAL_JAR_HASH_TXT);
    public string CacheModifiedJarHashPath => CacheDirectory.CombineAsOSPath(ModdingConstants.MODIFIED_JAR_HASH_TXT);
    public string CacheXmlHashPath => CacheDirectory.CombineAsOSPath(ModdingConstants.XML_BUILD_HASH_TXT);
    public string CacheJavaHashPath => CacheDirectory.CombineAsOSPath(ModdingConstants.JAVA_BUILD_HASH_TXT);
    public string CacheConfigJsonPath => CacheDirectory.CombineAsOSPath(SpaceHavenConstants.CONFIG_JSON);
    public string CacheModsJsonPath => CacheDirectory.CombineAsOSPath(ModdingConstants.MODS_JSON);
    public string CacheAspectjPath => CacheDirectory.CombineAsOSPath(ModdingConstants.ASPECTJ);
    public string CacheAspectjWeaverPath => CacheDirectory.CombineAsOSPath(ModdingConstants.ASPECTJWEAVER);

    #endregion Derived Properties

}
