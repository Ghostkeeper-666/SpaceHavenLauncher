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

    public static readonly string CRD1 = Encoding.UTF8.GetString(new byte[] { 0x45, 0x78, 0x74, 0x72, 0x61, 0x43, 0x72, 0x65, 0x64, 0x69, 0x74, 0x73, 0x2E, 0x74, 0x78, 0x74 });
    public static readonly byte[] CRD2 = new byte[] { 0xA4, 0xAB, 0x90, 0x8F, 0x96, 0x9C, 0xA2, 0xAC, 0x8F, 0x9E, 0x9C, 0x9A, 0xDF, 0xB7, 0x9E, 0x89, 0x9A, 0x91, 0xDF, 0xB3, 0x9E, 0x8A, 0x91, 0x9C, 0x97, 0x9A, 0x8D, 0xF2, 0xF5, 0xB8, 0x97, 0x90, 0x8C, 0x8B, 0x94, 0x9A, 0x9A, 0x8F, 0x9A, 0x8D, 0xC9, 0xC9, 0xC9, 0xF2, 0xF5, 0xB4, 0x9E, 0x96, 0x8C, 0x9A, 0x8D, 0xB2, 0x9E, 0x91, 0x91, 0x86, 0xF2, 0xF5, 0xF2, 0xF5 };
}
