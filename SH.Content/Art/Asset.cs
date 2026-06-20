using SH.Content.Extensions;
using SH.Content.Xml.Animations;
using SH.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SH.Content.Art;

public sealed class Asset
{
    public AnimationXml_Item Xml { get; }

    public int BoneId => Xml.BoneId;

    public float X => Xml.X;
    public float Y => Xml.Y;

    public float ScaleX => Xml.ScaleX;
    public float ScaleY => Xml.ScaleY;

    public float Rotation => Xml.Rotation;

    public IReadOnlyDictionary<int, bool> Visibility => Xml.Visibility;

    public bool IsSprite => SpriteName >= 0;
    public int SpriteName => Xml.SpriteName;

    public bool IsAnimation => !AnimationName.IsNullOrEmpty();
    public string AnimationName => Xml.AnimationName;

    public bool Loop => Xml.Loop;
    public int StartFrame => (int)Xml.StartFrame;
    public int EndFrame => (int)Xml.EndFrame;

    public Asset(AnimationXml_Item xml) =>
        Xml = xml;

    public bool IsVisibleInFrame(int frameId)
    {
        if (Visibility == null || Visibility.Count == 0)
            throw new Exception("No visibility info available");
        
        bool visible = true;
        foreach (KeyValuePair<int, bool> kvp in Visibility.OrderBy(k => k.Key))
        {
            if (kvp.Key > frameId)
                return visible;
            visible = kvp.Value;
        }
        return visible;
    }
}

