using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data
{
    public EElementType Type { get; }
    public int EID { get; }
    public int GridOffsetX { get; }
    public int GridOffsetY { get; }
    public ELayer Layer { get; }
    public List<ElementXml_Data_Element> Element { get; } // e.g. Medical Bed has 2/3 !!!
}
