namespace SH.Content.Xml.Animations;

public sealed class AnimationXml_BonePosition
{
    public AnimationXml_Bone Bone { get; set; }
    public int FrameId { get; set; } // f
    public float X { get; set; } // x
    public float Y { get; set; } // y
    public float ScaleX { get; set; } // sx
    public float ScaleY { get; set; } // sy
    public int Rotation { get; set; } // r
    public int ColorMask { get; set; } // col
}
