using System.Collections.Generic;

namespace SH.Content.Xml.Animations;

public sealed class AnimationXml_Item
{
    public AnimationXml Animation { get; set; }
    public IReadOnlyDictionary<int, bool> Visibility { get; set; } // vf
    public int BoneId { get; set; } // bi
    public float X { get; set; } // x
    public float Y { get; set; } // y
    public float ScaleX { get; set; } // sx
    public float ScaleY { get; set; } // sy
    public float Rotation { get; set; } // r
    public int SpriteName { get; set; } // a

    public bool Loop { get; set; } // l
    public float StartFrame { get; set; } // sf
    public float EndFrame { get; set; } // se
    public string AnimationName { get; set; } // an
}
