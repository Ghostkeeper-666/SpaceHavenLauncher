using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_AdditionalFullLit
{
    public int X { get; }
    public int Y { get; }

    public int MirrorX { get; }
    public int MirrorY { get; }

    public bool AtNoPower { get; }
    public bool AtStandby { get; }
    public bool AtInUse { get; }

    public OrderedDictionary<ERotation, ElementXml_AnimationReference> AnimationIds { get; } = [];
}