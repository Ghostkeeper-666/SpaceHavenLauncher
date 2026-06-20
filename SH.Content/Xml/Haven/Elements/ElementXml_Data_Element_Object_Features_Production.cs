namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_Production
{
    public int ProductionRate { get; } // produces/l/valuePerSec
    public int ProductId { get; } // produces/l/product
    public float BasicPowerUsage { get; } // produces/l/basicPowerUsage
    public float AdvancedPowerUsage { get; } // produces/l/useHighCapPowerPerSec
    public EPowerCategory PowerCategory { get; } // produces/l/powerCategory
    public bool Suction { get; } // produces/l/suction
    public EGuardType? GuardType { get; } // produces/l/guard/type
    public ElementXml_Data_Element_Object_Features_Production_Logic Logic { get; } // produces/l/logic
    public ElementXml_Data_Element_Object_Features_Production_HeatProduction HeatProduction { get; } // produces/l/heat/type="HeatTo"
    public ElementXml_Data_Element_Object_Features_Production_HeatControl HeatControl { get; } // produces/l/heat/type="Controller"
    public ElementXml_Data_Element_Object_Features_Production_GasProduction GasProduction { get; } // produces/l/gas
}

