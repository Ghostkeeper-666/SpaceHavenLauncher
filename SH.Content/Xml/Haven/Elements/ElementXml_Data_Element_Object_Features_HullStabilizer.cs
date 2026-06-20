namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_HullStabilizer
{
    public int TotalHullPoints { get; } // stabilizer/hull
    public int EnergyConsumedPerHullPoint { get; } // stabilizer/powerUsgPerPoint
    public int ChargeDuration { get; } // stabilizer/rechargeTimeSec
    public int ChargeHullPoints { get; } // stabilizer/chargePoints
    public bool IgnoreShipSize { get; } // stabilizer/ignoreSizeValue
    public ElementXml_Resource ChargeResource { get; } // stabilizer/useResource
}
