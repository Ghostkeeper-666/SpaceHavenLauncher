using SkiaSharp;
using SH.Content.Xml.Animations;

namespace SH.Content.Art;

public sealed class BoneKeyFrame
{
    public AnimationXml_BonePosition Xml { get; }

    public int FrameId => Xml.FrameId;

    public float X => Xml.X;
    public float Y => Xml.Y;

    public float ScaleX => Xml.ScaleX;
    public float ScaleY => Xml.ScaleY;

    public float Rotation => Xml.Rotation;

    public SKColor ColorMask => new(
        (byte)((Xml.ColorMask >> 24) & 0xFF),
        (byte)((Xml.ColorMask >> 16) & 0xFF),
        (byte)((Xml.ColorMask >> 8) & 0xFF),
        (byte)(Xml.ColorMask & 0xFF)
    );

    public BoneKeyFrame(AnimationXml_BonePosition xml)
    {
        Xml = xml;
    }
}