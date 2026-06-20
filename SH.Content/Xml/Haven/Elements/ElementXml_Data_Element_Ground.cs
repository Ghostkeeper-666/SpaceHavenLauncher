using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Ground : ElementXml_Data_Element
{
    public bool RenderOnFloor { get; }
    public bool RenderOnFloorPartially { get; }
    public bool NoMoveDismantle { get; }
    public bool CannotBeSelected { get; }
    public int WalkGridCost { get; }
    public ELightAbsorption Light { get; }
    public int BottomGroundAnimationId { get; }
    public int TopGroundAnimationId { get; }
    public bool IsSpecial { get; }
    public bool IsSelectable { get; }
    public bool IsPermanent { get; }
    public bool CanDuplicate { get; }
    public List<ElementXml_AnimationReference> BottomDeco { get; }
    public List<ElementXml_AnimationReference> BottomFullLit { get; }
}
