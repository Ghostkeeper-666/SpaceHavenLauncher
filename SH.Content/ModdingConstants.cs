using System.Collections.Generic;

namespace SH.Content;

/// <summary>
/// NEVER USE PUBLIC CONST !!!
/// </summary>
public static class ModdingConstants
{
    public static readonly string BACKUP = "backup";
    public static readonly string TEMPLATE = "template";
    public static readonly string BUILD = "build";
    public static readonly string STAGE = "stage";

    public static readonly string INFO_XML = "info.xml";
    public static readonly string DESCRIPTION_MD = "description.md";


    public static readonly string ASPECTJ = "aspectj-1.9.19.jar";
    public static readonly string ASPECTJWEAVER = "aspectjweaver-1.9.19.jar";
    public static readonly string MODS_JSON = "mods.json";

    public static readonly string TEMPLATE_SPACEHAVEN_JAR = "templatespacehaven.jar";
    public static readonly string MODIFIED_SPACEHAVEN_JAR = "modifiedspacehaven.jar";

    public static readonly string LOG_TXT = "log.txt";
    public static readonly string XML_BUILD_HASH_TXT = "xml.hash";
    public static readonly string JAVA_BUILD_HASH_TXT = "java.hash";
    public static readonly string ORIGINAL_JAR_HASH_TXT = "originaljar.hash";
    public static readonly string MODIFIED_JAR_HASH_TXT = "modifiedjar.hash";

    public static readonly string IdVariable = "id";
    public static readonly string BracedIdVariable = "{id}";

    public static readonly string DISABLED_TXT = "disabled.txt"; // DEPRECATED
    public static readonly string CUSTOM_TEXTURE = "custom_texture_"; // DEPRECATED
    public static readonly string GENERATED_TEXTURES_XML = $"generated_textures.xml"; // DEPRECATED

    public static IReadOnlyList<string> PathsForModding => new string[]
    {
        $"{SpaceHavenConstants.VERSION_TXT}",
        $"{SpaceHavenConstants.LIBRARY}/{SpaceHavenConstants.HAVEN}",
        $"{SpaceHavenConstants.LIBRARY}/{SpaceHavenConstants.TEXTS}",
        $"{SpaceHavenConstants.LIBRARY}/{SpaceHavenConstants.AUDIO}",
        $"{SpaceHavenConstants.LIBRARY}/{SpaceHavenConstants.TEXTURES}",
        $"{SpaceHavenConstants.LIBRARY}/{SpaceHavenConstants.ANIMATIONS}",
        $"{SpaceHavenConstants.LIBRARY}/{SpaceHavenConstants.FILES}/{SpaceHavenConstants.SPACEHAVENSETTINGS_XML}",
        $"{SpaceHavenConstants.LIBRARY}/{SpaceHavenConstants.MUSIC}",
        $"{SpaceHavenConstants.LIBRARY}/{SpaceHavenConstants.SOUND}",
        $".cim"
    };
}
