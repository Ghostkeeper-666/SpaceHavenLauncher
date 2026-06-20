namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_RobotStation
{
    public int RobotId { get; } // roboDock/robot
    public int ChargeBasicPowerUsage { get; } // roboDock/energyUsePerSec
    public int ChargeTime { get; } // roboDock/oneChargeTime
    public ElementXml_Resource ChargeResource { get; } // roboDock/oneChargeNeeds
}