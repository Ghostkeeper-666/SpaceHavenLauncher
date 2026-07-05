using System.Text;

namespace SH.Content;

/// <summary>
/// NEVER USE PUBLIC CONST !!!
/// </summary>
public static class SpaceHavenConstants
{
    public static readonly string SpaceHavenName = "Space Haven";

    public static readonly string SPACE_HAVEN_EXECUTABLE_FILENAME =
#if WINDOWS
    "spacehaven.exe";
#elif MACOS
    "spacehaven.app";
#else
    "spacehaven";
#endif

    public static readonly string SPACEHAVEN_JAR = "spacehaven.jar";
    public static readonly string CONFIG_JSON = "config.json";
    public static readonly string VERSION_TXT = "version.txt";
    public static readonly string EXTRA_CREDITS_TXT = "ExtraCredits.txt";

    public static readonly string HAVEN = "haven";
    public static readonly string TEXTS = "texts";
    public static readonly string AUDIO = "audio";
    public static readonly string TEXTURES = "textures";
    public static readonly string ANIMATIONS = "animations";
    public static readonly string SPACEHAVENSETTINGS_XML = "spacehavensettings.xml";

    public static readonly string LIBRARY = "library";
    public static readonly string FILES = "files";
    public static readonly string MUSIC = "music";
    public static readonly string SOUND = "sound";
}
