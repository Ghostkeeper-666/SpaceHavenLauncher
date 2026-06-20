using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml
{
    public int Id { get; }
    public int EC { get; }
    public int CostGroupId { get; }
    public ERotation RotationOffset { get; }
    public bool IsNonSymmetrical { get; }
    public bool HasAlternateY { get; }
    public List<ElementXml_Data> Data { get; }
    public List<ElementXml_ElementReference> LinkedElements { get; }
    public List<ElementXml_ElementReference> RandomElements { get; } // <rand> and <randomElements> must always have the same content!
    public List<ElementXml_Event> Events { get; }
    public ElementXml_ObjectInfo ObjectInfo { get; }

}
