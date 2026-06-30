using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace SH.Content.Modding.Build;

internal sealed class SpriteReference
{
    public static string GetLocalName(string modName, string relativePathOrRelativeReference, ETextureFilter filter) =>
        $"{modName}::{relativePathOrRelativeReference?.RemoveSuffix(".png", StringComparison.OrdinalIgnoreCase)?.ToLowerInvariant().AsStdPath()}::{filter.ToString().ToUpperInvariant()}";

    public SpriteReference(string localName, int localId, ModBuildData mod, string assetPosFilenameReference, ETextureFilter filter)
    {
        LocalName = !localName.IsNullOrWhiteSpace() ? localName : throw new ArgumentNullException(nameof(localName));
        LocalID = localId;
        Mod = mod ?? throw new ArgumentNullException(nameof(mod));
        BasePath = mod.TexturesDirectory.AsOSPath();
        RelativePath = assetPosFilenameReference?.RemoveSuffix(".png", StringComparison.OrdinalIgnoreCase).AsOSPath();
        AbsolutePath = $"{IOUtils.CombineAsOSPath(BasePath, RelativePath)}.png".AsOSPath();
        Filter = filter;
    }

    public SpriteBuildData Sprite { get; internal set; }
    public List<XElement> AssetPosNodes { get; } = [];

    public int LocalID { get; }
    public string LocalName { get; }

    public ModBuildData Mod { get; }
    public ETextureFilter Filter { get; }

    public string BasePath { get; }
    public string RelativePath { get; }
    public string AbsolutePath { get; internal set; }
    public string Filename => Path.GetFileName(RelativePath);

    public override string ToString() => LocalName;
}
