using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_DefinedState
{
    public EElementState State { get; }
    public bool PlayAnimation { get; }
    public List<ElementXml_AnimationReference> AnimationReferences { get; }
    public List<ElementXml_Data_Element_Object_DefinedState_EventListener> EventListeners { get; }
}
