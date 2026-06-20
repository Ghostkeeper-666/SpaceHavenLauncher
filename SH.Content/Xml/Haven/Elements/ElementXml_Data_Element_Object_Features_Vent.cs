using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_Vent
{
    // vent/r0Closed,r90Closed,r180Closed,r270Closed
    public OrderedDictionary<ERotation, int> AnimationIds { get; } = [];

    // vent/additionaFullLitOpen (ATTENTION: TYPO!)
    public ElementXml_Data_Element_AdditionalFullLit AdditionalFullLitOpen { get; }

    // vent/additionalFullLitClosed
    public ElementXml_Data_Element_AdditionalFullLit AdditionalFullLitClosed { get; }
}
