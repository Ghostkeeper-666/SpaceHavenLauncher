using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SH.Content.Xml;

public sealed class NodeType
{
    public static readonly string ATTRIBUTE_OWNER = "_owner";
    public static readonly string ATTRIBUTE_LIBRARY = "_library";
    public static readonly string ATTRIBUTE_PATCH = "_patch";

    public static NodeType Audio { get; }
    public static NodeType Animations { get; }
    public static NodeType TexturesCim { get; }
    public static NodeType TexturesRegion { get; }
    public static NodeType SpacehavenSettings { get; }

    static NodeType()
    {
        Audio = RegisteredTypes.Values.FirstOrDefault(n => n.XmlFileType == EXmlFileType.Audio);
        Animations = RegisteredTypes.Values.FirstOrDefault(n => n.XmlFileType == EXmlFileType.Animations);
        TexturesCim = RegisteredTypes.Values.FirstOrDefault(n => n.XmlFileType == EXmlFileType.Textures && n.XPath.EndsWith("/t"));
        TexturesRegion = RegisteredTypes.Values.FirstOrDefault(n => n.XmlFileType == EXmlFileType.Textures && n.XPath.EndsWith("/re"));
        SpacehavenSettings = RegisteredTypes.Values.FirstOrDefault(n => n.XmlFileType == EXmlFileType.SpaceHavenSettings);
    }

    public NodeType(string xPath, string keyAttribute, bool isNumericId, EKeyPool keyPool, EXmlFileType xmlFile)
    {
        XPath = xPath ?? throw new ArgumentNullException(nameof(xPath));
        KeyAttribute = keyAttribute;
        IsNumericId = isNumericId;
        KeyPool = keyPool;
        XmlFileType = xmlFile;
        ParentXPath = XPath.GetParentDirAsStdPath().Replace("\\", "/");
    }

    public string XPath { get; }
    public string ParentXPath { get; }
    public string KeyAttribute { get; }
    public bool IsNumericId { get; }
    public EKeyPool KeyPool { get; }
    public EXmlFileType XmlFileType { get; }

    public static IReadOnlyDictionary<string, NodeType> RegisteredTypes { get; } = new NodeType[]
    {
        // haven (generic ID pool):
        new("/data/Accident/accident", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/AccidentList/list", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Augmentation/augment", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/BackPack/item", "mid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/BackStory/backstory", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/CelestialObject/celestialObject", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Character/character", "cid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/CharacterCondition/condition", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/CharacterSet/characters", "cid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/CharacterTrait/trait", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/CostGroup/group", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Craft/craft", "cid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/DataLog/dataLog", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/DataLogFragment/fragment", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/DefaultStuff/stuff", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/DefinedRoomType/definedRoom", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/DialogChoice/choice", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/DifficultySettings/settings", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Effect/effect", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Element/me", "mid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Encounter/encounter", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/ExodusFleetEvent/event", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/ExodusMissionDialog/missionDialog", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Explosion/explosion", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Faction/faction", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/FloorExpPackage/expPackage", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/GameScenario/game", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/GOAPAction/action", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/IsoFX/fx", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Item/item", "mid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/MainCat/cat", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Monster/monster", "cid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Notes/stuff", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/ObjectiveCollection/collection", "nid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/PersonalitySettings/settings", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Plan/plan", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Product/product", "eid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Randomizer/randomizer", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/RandomShip/ship", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Robot/robot", "cid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/RoofExpPackage/expPackage", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Room/data", "rid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Sector/bg", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Ship/data", "rid", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/SubCat/cat", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/Tech/tech", "id", true, EKeyPool.Generic, EXmlFileType.Haven),
        new("/data/TechTree/tree", "id", true, EKeyPool.Generic, EXmlFileType.Haven),

        // haven (special ID pools):
        new("/data/IdleAnim/an", "id", true, EKeyPool.IdleAnim, EXmlFileType.Haven),
        new("/data/ShipStarMapData/data", "id", true, EKeyPool.ShipStarMapData, EXmlFileType.Haven),
        new("/data/TradingValues/trade/t", "eid", true, EKeyPool.Trade, EXmlFileType.Haven),

        // texts:
        new("/t/t", "id", true, EKeyPool.Resource, EXmlFileType.Texts),

        // audio:
        new("/audio/a", "id", true, EKeyPool.Resource, EXmlFileType.Audio),

        // textures:
        new("/AllTexturesAndRegions/textures/t", "i", true, EKeyPool.TexturesCim, EXmlFileType.Textures),
        new("/AllTexturesAndRegions/regions/re", "n", true, EKeyPool.TexturesRegion, EXmlFileType.Textures),

        // animations:
        // ID is ignored by the game, NAME is the primary key!
        new("/AllAnimations/animations/ba", "n", true, EKeyPool.Animation, EXmlFileType.Animations),

        // spacehavensettings:
        new("/settings/e", "field", false, EKeyPool.SpaceHavenSettings, EXmlFileType.SpaceHavenSettings),

    }.ToOrderedDictionary(item => item.XPath, item => item);


    public override string ToString() => XPath;
}
