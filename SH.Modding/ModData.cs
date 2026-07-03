using SH.Framework.Extensions;
using SH.Framework.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SH.Modding;

/// <summary>
/// A class containing loaded mod information and persisted mod variable values
/// </summary>
public sealed class ModData
{
    public bool IsModified { get; set; }
    public bool IsEnabled { get; set; } = true;

    public bool IsXmlMod => XmlLibraryFilePaths.Count > 0 || XmlPatchFilePaths.Count > 0;
    public bool IsJavaMod => JavaFilePaths.Count > 0;

    public string Name { get; set; }
    public string InfoXmlDescription { get; set; }
    public string MarkdownDescription { get; set; }
    public VersionInfo Version { get; set; }
    public int ModId { get; set; }
    public int AutoId { get; set; }
    public int CustomId { get; set; }
    public int ID =>
        ModId != 0 && (CustomId == 0 || CustomId == ModId) ? ModId :
        AutoId != 0 && (CustomId == 0 || CustomId == AutoId) ? AutoId :
        CustomId;
    public string Author { get; set; }
    public string ForegroundColor { get; set; }

    public List<VarData> Variables { get; } = [];
    public VersionCompatibilityList AppCompatibility { get; set; } = new();
    public VersionCompatibilityList SpaceHavenCompatibility { get; set; } = new();
    public VersionCompatibilityList ModConflicts { get; } = new();
    public VersionCompatibilityList ModDependencies { get; } = new();

    public string Directory { get; set; }

    public string XmlLibraryDirectory => Path.Combine(Directory, ModdingConstants.LIBRARY);
    public string XmlPatchesDirectory => Path.Combine(Directory, ModdingConstants.PATCHES);
    public string AudioDirectory => Path.Combine(Directory, ModdingConstants.AUDIO);
    public string TexturesDirectory => Path.Combine(Directory, ModdingConstants.TEXTURES);

    public string InfoXmlPath { get; set; }
    public string MarkdownDescriptionPath { get; set; }
    public string BackgroundImagePath { get; set; }

    public bool HasAudio => AudioFilePaths.Count > 0;
    public bool HasTextures => TextureFilePaths.Count > 0;
    public bool HasLibraryXml => XmlLibraryFilePaths.Count > 0;
    public bool HasPatchXml => XmlPatchFilePaths.Count > 0;
    public bool HasJava => JavaFilePaths.Count > 0;

    public List<string> AudioFilePaths { get; } = [];
    public List<string> TextureFilePaths { get; } = [];
    public List<string> XmlLibraryFilePaths { get; } = [];
    public List<string> XmlPatchFilePaths { get; } = [];
    public List<string> JavaFilePaths { get; } = [];
    public List<string> OtherFilePaths { get; } = [];
    
    public List<string> AllPaths { get; } = [];

    // BE CAREFUL: Paths are relative to their respective relevant base directory!
    public List<string> AudioRelativeFilePaths => AudioFilePaths.Select(path => path.RemovePrefix(AudioDirectory).TrimStart('\\', '/')).ToList();
    public List<string> TextureRelativeFilePaths => TextureFilePaths.Select(path => path.RemovePrefix(TexturesDirectory).TrimStart('\\', '/')).ToList();
    public List<string> XmlLibraryRelativeFilePaths => XmlLibraryFilePaths.Select(path => path.RemovePrefix(XmlLibraryDirectory).TrimStart('\\', '/')).ToList();
    public List<string> XmlPatchRelativeFilePaths => XmlPatchFilePaths.Select(path => path.RemovePrefix(XmlPatchesDirectory).TrimStart('\\', '/')).ToList();
    public List<string> JavaRelativeFilePaths => JavaFilePaths.Select(path => path.RemovePrefix(Directory).TrimStart('\\', '/')).ToList();
    public List<string> OtherRelativeFilePaths => OtherFilePaths.Select(path => path.RemovePrefix(Directory).TrimStart('\\', '/')).ToList();

    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => $"{Name} {Version}";
}
