namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_CryoTank
{
    public int OffsetX { get; } // cryoTank/offsetX
    public int OffsetY { get; } // cryoTank/offsetY

    public int LiquidOffsetY { get; } // cryoTank/liquidOffY
    public float LiquidAlpha { get; } // cryoTank/liquidAlpha
    public int LiquidAnimationId { get; } // cryoTank/liquid/aid

    public ElementXml_Resource ChargeResource { get; } // cryoTank/cryoCharge

    public int ConditionId { get; } // cryoTank/cryoCondition/condition
}