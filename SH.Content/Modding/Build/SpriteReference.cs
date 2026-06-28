using SH.Content.Enums;
using SH.Content.Xml;
using SH.Framework.Extensions;
using SH.Framework.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace SH.Content.Modding.Build;

internal sealed class SpriteReference
{
    public static string GetName(string relativePathOrRelativeReference) =>
        relativePathOrRelativeReference?.RemoveSuffix(".png", StringComparison.OrdinalIgnoreCase)?.ToLowerInvariant().AsStdPath();


    public SpriteReference(string localName, int localID)
    {
        LocalName = !localName.IsNullOrWhiteSpace() ? localName : throw new ArgumentNullException(nameof(localName));
        LocalID = localID;
    }

    public XmlFile XmlFile { get; set; }
    public int LocalID { get; set; }
    public string LocalName { get; set; }
    public string RelativePath { get; set; }
    public string BasePath { get; set; }
    public HashSet<ETextureFilter> Filters = [];

    public string Filename => Path.GetFileName(RelativePath);
    public string AbsolutePath => IOUtils.CombineAsOSPath(BasePath, RelativePath);

    public override string ToString() => $@"Sprite Reference {{ LocalName=""{LocalName}"", LocalID={LocalID}, Filters=[{Filters.JoinToString(",")}] }}";
}
