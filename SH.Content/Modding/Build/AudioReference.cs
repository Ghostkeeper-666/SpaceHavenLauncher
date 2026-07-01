using System.Xml.Linq;

namespace SH.Content.Modding.Build;

internal sealed class AudioReference
{
    public AudioReference(ModBuildData mod, XElement xml, string name, string targetRelativePath)
    {
        Mod = mod;
        Xml = xml;
        Name = name;
        TargetRelativePath = targetRelativePath;
    }

    public ModBuildData Mod { get; }
    public XElement Xml { get; }
    public string Name { get; }
    public string TargetRelativePath { get; }

    public string SourceRelativePath { get; set; }
    public string SourceAbsolutePath { get; set; }
}
