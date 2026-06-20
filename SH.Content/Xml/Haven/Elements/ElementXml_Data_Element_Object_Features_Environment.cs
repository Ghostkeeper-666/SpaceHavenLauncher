namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_Environment
{
    public float RepairBlockFail { get; }

    public int ColdOffTempC { get; }
    public int HotOffTempC { get; }

    public int ColdSlowTempC { get; }
    public int HotSlowTempC { get; }

    public float ColdTempSlowdown { get; }
    public float HotTempSlowdown { get; }

    public int CyclesForDmg { get; }
    public float CyclesDmgChance { get; }

    public int HazTimeDmg { get; }
    public float HazDmgChance { get; }

    public int TempTimeDmg { get; }
    public float TempDmgChance { get; }

    public int WaterTimeDmg { get; }
    public float WaterDmgChance { get; }
}
