using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SH.Modding.Build;

internal sealed class SpriteReference
{
    public static string GetKey(string modName, string relativePathOrRelativeReference, ETextureFilter filter) =>
        $"{filter.ToString().ToUpperInvariant()}::{modName}::{relativePathOrRelativeReference?.RemoveSuffix(".png", StringComparison.OrdinalIgnoreCase)?.ToLowerInvariant().AsStdPath()}";

    public SpriteReference(string key, int localId, Mod mod, string assetPosFilenameReference, ETextureFilter filter)
    {
        Key = !key.IsNullOrWhiteSpace() ? key : throw new ArgumentNullException(nameof(key));
        LocalID = localId;
        Mod = mod ?? throw new ArgumentNullException(nameof(mod));
        BasePath = mod.SpritesDir.AsOSPath();
        RelativePathWithoutExtension = assetPosFilenameReference?.RemoveSuffix(".png", StringComparison.OrdinalIgnoreCase).AsOSPath();
        Filter = filter;
    }

    public Sprite Sprite { get; internal set; }
    public List<XElement> AssetPosNodes { get; } = [];

    public int LocalID { get; }
    public string Key { get; }

    public Mod Mod { get; }
    public ETextureFilter Filter { get; }

    public string BasePath { get; }
    public string RelativePathWithoutExtension { get; }
    public string FilenameWithoutExtension => RelativePathWithoutExtension.GetFileName();
    public string AbsolutePath { get; internal set; }

    public override string ToString() => Key;
}
