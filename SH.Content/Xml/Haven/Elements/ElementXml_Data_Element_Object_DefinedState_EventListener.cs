namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_DefinedState_EventListener
{
    public int ElementId { get; } // from l event="..."
    public int EventId { get; } // from l event="..."
    public bool OnEnter { get; }
    public bool OnLeave { get; }
    public ElementXml_Event_SwitchState SwitchState { get; }
    public ElementXml_Event_WorkConsole WorkConsole { get; }
}
