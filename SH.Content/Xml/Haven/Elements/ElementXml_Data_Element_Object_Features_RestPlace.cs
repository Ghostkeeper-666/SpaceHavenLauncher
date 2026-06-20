namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_RestPlace
{
    public int RestValue { get; } // rest/restValue
    public bool UseAllTask { get; } // rest/useAllTask  ???????????
    public bool RestIsBed { get; } // rest/isBed
    public bool IsMedical { get; } // rest/isMedical
    public bool IsSitRest { get; } // rest/isSitRest
    public bool CanBeAccessedFromAllTiles { get; } // rest/accessAllTiles

    public int? ConditionId { get; } // rest/condition/condition
    public int? WhenBrokenConditionId { get; } // rest/whenBroken/condition
}