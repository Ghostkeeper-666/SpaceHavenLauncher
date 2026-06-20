using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_ObjectInfo
{
    // From Properties:
    public int PlaceInMenu { get; }
    public bool DisableRotation { get; }
    public bool DisableBuildTest { get; }
    public bool BuildInstantly { get; }
    public bool DisableAirlockTest { get; }
    public bool DebugOnly { get; }
    public bool CanDismantleOnly { get; }
    public bool IsRandomRot { get; }
    public EElementViewMode ViewMode { get; }
    public int SystemPoints { get; }
    public EElementCanBeBuilt CanBuildAt { get; }
    public bool IsOutdoorObject { get; }
    public bool IsIndoorObject { get; }

    // From Nodes:
    public int NameId { get; } // objectInfo/name/tid
    public int DescriptionId { get; } // objectInfo/desc/tid
    public int GuiAnimationId { get; } // objectInfo/guiIcon/aid
    public int SubMenuCategoryId { get; } // objectInfo/subCat/id
    public int CustomHitPoints { get; } // objectInfo/customHitPoints/hitPoints
    public ESkill? ConstructionSkill { get; } // objectInfo/difficultyLevel/skill
    public int ConstructionSkillLevel { get; } // objectInfo/difficultyLevel/level
    public List<ElementXml_ObjectInfo_CustomRepair> CustomRepair { get; } // objectInfo/cu/groups/l
    public List<ElementXml_ObjectInfo_BuildRestrictions> BuildRestrictions { get; } // objectInfo/restrictions
}

public sealed class ElementXml_ObjectInfo_BuildRestrictions
{
    public EBuildRestrictionType Type { get; }

}
