using SH.Content.Enums;
using SH.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
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

    public NodeType(string xPath, string idAttribute, string nameAttribute, bool isNumericId, EIdPool idPool, EXmlFileType xmlFile)
    {
        XPath = xPath ?? throw new ArgumentNullException(nameof(xPath));
        IdAttribute = idAttribute;
        NameAttribute = nameAttribute;
        IsNumericId = isNumericId;
        IdPool = idPool;
        XmlFileType = xmlFile;
        ParentXPath = Path.GetDirectoryName(XPath).Replace("\\", "/");
    }

    public string XPath { get; }
    public string ParentXPath { get; }
    public string IdAttribute { get; }
    public string NameAttribute { get; }
    public bool IsNumericId { get; }
    public EIdPool IdPool { get; }
    public EXmlFileType XmlFileType { get; }

    public static IReadOnlyDictionary<string, NodeType> RegisteredTypes { get; } = new NodeType[]
    {
        // haven (generic ID pool):
        new("/data/Accident/accident", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/AccidentList/list", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Augmentation/augment", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/BackPack/item", "mid", null,true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/BackStory/backstory", "id", null,true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/CelestialObject/celestialObject", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Character/character", "cid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/CharacterCondition/condition", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/CharacterSet/characters", "cid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/CharacterTrait/trait", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/CostGroup/group", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Craft/craft", "cid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/DataLog/dataLog", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/DataLogFragment/fragment", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/DefaultStuff/stuff", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/DefinedRoomType/definedRoom", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/DialogChoice/choice", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/DifficultySettings/settings", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Effect/effect", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Element/me", "mid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Encounter/encounter", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/ExodusFleetEvent/event", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/ExodusMissionDialog/missionDialog", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Explosion/explosion", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Faction/faction", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/FloorExpPackage/expPackage", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/GameScenario/game", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/GOAPAction/action", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/IsoFX/fx", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Item/item", "mid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/MainCat/cat", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Monster/monster", "cid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Notes/stuff", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/ObjectiveCollection/collection", "nid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/PersonalitySettings/settings", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Plan/plan", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Product/product", "eid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Randomizer/randomizer", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/RandomShip/ship", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Robot/robot", "cid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/RoofExpPackage/expPackage", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Room/data", "rid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Sector/bg", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Ship/data", "rid", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/SubCat/cat", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/Tech/tech", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),
        new("/data/TechTree/tree", "id", null, true, EIdPool.Generic, EXmlFileType.Haven),

        // haven (special ID pools):
        new("/data/IdleAnim/an", "id", null, true, EIdPool.IdleAnim, EXmlFileType.Haven),
        new("/data/ShipStarMapData/data", "id", null, true, EIdPool.ShipStarMapData, EXmlFileType.Haven),
        new("/data/TradingValues/trade/t", "eid", null, true, EIdPool.Trade, EXmlFileType.Haven),

        // texts:
        new("/t/t", "id", null, true, EIdPool.Resource, EXmlFileType.Texts),

        // audio:
        new("/audio/a", "id", "n", true, EIdPool.Resource, EXmlFileType.Audio),

        // textures:
        new("/AllTexturesAndRegions/textures/t", "i", null, true, EIdPool.TexturesCim, EXmlFileType.Textures),
        new("/AllTexturesAndRegions/regions/re", "id", "n", true, EIdPool.TexturesRegion, EXmlFileType.Textures),

        // animations:
        // ID is ignored by the game, NAME is the primary key!
        new("/AllAnimations/animations/ba", "n", null, true, EIdPool.Animation, EXmlFileType.Animations),

        // spacehavensettings:
        new("/settings/e", null, "field", false, EIdPool.SpaceHavenSettings, EXmlFileType.SpaceHavenSettings),

    }.ToOrderedDictionary(item => item.XPath, item => item);


    public override string ToString() => XPath;
}
