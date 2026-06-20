namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_EngineHub
{
    public int MassCapacity { get; } // engineHub/massCapacity
    public int BasicPower { get; } // engineHub/useHighCapPowerPerSec
    public int AdvancedPower { get; } // engineHub/useBasicPower
    public ElementXml_Resource FuelResource { get; } // engineHub/fuel
}