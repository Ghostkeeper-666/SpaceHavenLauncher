using SH.Content.Enums;
using SH.Content.Xml.Haven.Products;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SH.Content.Xml;

public sealed class HavenXmlRepository
{
    private readonly ILogger Log;

    public HavenXmlRepository(ILogger logger)
    {
        Log = logger ?? new VoidLogger();
    }

    public OrderedDictionary<int, ProductXml> Products { get; } = [];

#if false
    public OrderedDictionary<int, ItemXml> Items { get; } = [];
    
    public OrderedDictionary<int, FactionXml> Factions { get; } = [];
    public OrderedDictionary<int, TradingValueXml> TradingValues { get; } = [];

    public OrderedDictionary<int, TechXml> Technologies { get; } = [];
    public OrderedDictionary<int, TechTreeXml> TechTrees { get; } = [];

    public OrderedDictionary<int, CostGroupXml> CostGroups { get; } = [];
    public OrderedDictionary<int, ElementXml> Elements { get; } = [];

    public OrderedDictionary<int, ExplosionXml> Explosions { get; } = [];
    public OrderedDictionary<int, AccidentXml> Accidents { get; } = [];
    public OrderedDictionary<int, AccidentListXml> AccidentLists { get; } = [];
    
    public OrderedDictionary<int, CraftXml> Crafts { get; } = [];
    public OrderedDictionary<int, CharacterConditionXml> CharacterConditions { get; } = [];
    public OrderedDictionary<int, AugmentationXml> Augmentations { get; } = [];

    public OrderedDictionary<int, PersonalitySettingXml> PersonalitySettings { get; } = [];
    public OrderedDictionary<int, RobotXml> Robots { get; } = [];
    public OrderedDictionary<int, MonsterXml> Monsters { get; } = [];

    public OrderedDictionary<int, CharacterTraitXml> CharacterTraits { get; } = [];
#endif

    public bool TryRead(string havenXmlPath)
    {
        try
        {
            XDocument doc = XDocument.Load(havenXmlPath);
            if (doc == null)
                return false;
            XElement root = doc.Element("data") ?? throw new Exception("Invalid XML root");

            if (!TryReadProducts(root.Element("Product")))
                return false;

            if (!TryReadItems(root.Element("Item")))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }

    private bool TryReadItems(XElement itemRoot)
    {
        try
        {


            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }


    private bool TryReadProducts(XElement productRoot)
    {
        Products.Clear();

        foreach (XElement p in productRoot.Elements("product"))
        {
            ProductXml product = new();
            product.Id = Convert.ToInt32(p.Attribute("eid").Value);
            product.ProductType = Enum.Parse<EProductType>(p.Attribute("type").Value);
            product.NameId = NullableInt.Parse(p.Element("name")?.Attribute("tid")?.Value);
            product.DescriptionId = NullableInt.Parse(p.Element("desc")?.Attribute("tid")?.Value);
            switch (product.ProductType)
            {
                // ELEMENTARY
                case EProductType.Elementary:
                    ProductXml_Elementary elementary = product.Elementary = new();
                    elementary.Product = product;
                    elementary.ItemId = Convert.ToInt32(p.Attribute("item").Value);
                    elementary.ElementType = Enum.Parse<EElementType>(p.Attribute("elementType").Value);
                    break;

                // PROCESS
                case EProductType.Process:
                    ProductXml_Process process = product.Process = new();
                    process.IsInteractiveProcess = bool.Parse(p.Attribute("interactive").Value);
                    process.IsStartOnlyProcess = bool.Parse(p.Attribute("startOnly").Value);
                    process.IsItemScrapperProcess = bool.Parse(p.Attribute("itemScrapper").Value);
                    process.IsSmelterProcess = bool.Parse(p.Attribute("smelter").Value);
                    process.IsScrapperProcess = bool.Parse(p.Attribute("scrapper").Value);

                    process.AccidentListId = NullableInt.Parse(p.Element("accidentList")?.Attribute("listId")?.Value);

                    process.SkillName = p.Element("difficulty")?.Attribute("skill")?.Value;
                    process.SkillLevel = NullableInt.Parse(p.Element("difficulty")?.Attribute("level")?.Value);
                    process.CustomDuration = NullableInt.Parse(p.Element("difficulty")?.Element("custom")?.Attribute("time")?.Value);

                    process.FoodType = NullableEnum.Parse<EFoodType>(p.Element("foodProcessing")?.Attribute("foodUsageType")?.Value);
                    process.OwnFoodProductsOnly = NullableBool.Parse(p.Element("foodProcessing")?.Attribute("ownProductsOnly")?.Value);
                    process.DispenserCapacity = NullableInt.Parse(p.Element("foodProcessing")?.Element("dispenser")?.Attribute("capacity")?.Value);
                    process.DispenserTimeToCharge = NullableInt.Parse(p.Element("foodProcessing")?.Element("dispenser")?.Attribute("timeToCharge")?.Value);

                    XElement f = p.Element("foodProcessing")?.Element("dispenser")?.Element("customFood");
                    if (f != null)
                    {
                        ProductXml_Process_CustomFood customFood = process.CustomFood = new();
                        customFood.Process = process;
                        customFood.FoodType = Enum.Parse<EFoodType>(f.Attribute("foodUsageType").Value);
                        customFood.Protein = float.Parse(f.Attribute("protein").Value);
                        customFood.Carbs = float.Parse(f.Attribute("carbs").Value);
                        customFood.Fat = float.Parse(f.Attribute("fat").Value);
                        customFood.Vitamins = float.Parse(f.Attribute("vitamins").Value);
                        customFood.Toxins = float.Parse(f.Attribute("toxins").Value);
                        customFood.Rank = float.Parse(f.Attribute("rank").Value);
                        customFood.IsIngredient = bool.Parse(f.Attribute("canBeProcessed").Value);
                        customFood.IsRawFood = bool.Parse(f.Attribute("raw").Value);
                        customFood.IsGoodFood = bool.Parse(f.Attribute("goodFood").Value);
                        customFood.DefaultAmountToCreateOtherFood = int.Parse(f.Attribute("useByDefaultValue").Value);
                        customFood.SortOrder = int.Parse(f.Attribute("sortOrder").Value);
                        customFood.NeverEat = bool.Parse(f.Attribute("neverEat").Value);
                        customFood.IsAlcohol = f.Element("foodtype")?.Attribute("ma")?.Value == "1";
                    }

                    foreach (XElement l in p.Element("list")?.Element("processes")?.Elements("l") ?? [])
                    {
                        int productId = Convert.ToInt32(l.Attribute("process").Value);
                        process.Children.Add(productId, null); // will be linked later...
                    }

                    foreach (XElement l in p.Element("needs")?.Elements("l") ?? [])
                    {
                        ProductXml_Process_Input input = new();
                        input.Process = process;
                        input.Id = int.Parse(l.Attribute("element").Value);
                        input.HowMuch = int.Parse(l.Attribute("howMuch").Value);
                        input.Every = int.Parse(l.Attribute("consumeEvery").Value);
                        process.Inputs.Add(input.Id, input);
                    }

                    foreach (XElement l in p.Element("products")?.Elements("l") ?? [])
                    {
                        ProductXml_Process_Output output = new();
                        output.Process = process;
                        output.Id = int.Parse(l.Attribute("element").Value);
                        output.HowMuch = int.Parse(l.Attribute("howMuch").Value);
                        output.Every = int.Parse(l.Attribute("produceEvery").Value);
                        process.Outputs.Add(output.Id, output);
                    }
                    break;

                // CROP
                case EProductType.Crop:
                    ProductXml_Crop crop = product.Crop = new();
                    crop.Product = product;
                    crop.Icon = p.Element("GUIAnimation").Attribute("aid").Value;
                    crop.PlantingPenalty = int.Parse(p.Element("plantingPenalty").Attribute("penalty").Value);
                    crop.RequiredSkillName = p.Element("difficulty").Attribute("skill").Value;
                    crop.RequiredSkillLevel = int.Parse(p.Element("difficulty").Attribute("level").Value);

                    foreach (XElement s in p.Element("stages")?.Elements("l") ?? [])
                    {
                        ProductXml_Crop_Stage stage = new();
                        stage.Crop = crop;
                        stage.Id = int.Parse(s.Attribute("stage").Value);
                        stage.Duration = int.Parse(s.Attribute("time").Value);
                        stage.CO2PerMinute = int.Parse(s.Attribute("co2PerMinute").Value);
                        stage.OxygenPerMinute = int.Parse(s.Attribute("oxygenPerMinute").Value);
                        stage.H2OPerMinute = int.Parse(s.Attribute("h2oPerMinute").Value);
                        stage.TemperatureMin = int.Parse(s.Attribute("minTemp").Value);
                        stage.TemperatureMax = int.Parse(s.Attribute("maxTemp").Value);
                        stage.LightRequired = Enum.Parse<ELightIntensity>(s.Attribute("lightNeeded").Value);

                        stage.Penalty = NullableInt.Parse(s.Element("tend")?.Attribute("penalty")?.Value);

                        foreach (XElement i in s.Element("needs")?.Elements("l") ?? [])
                        {
                            ProductXml_Crop_Input input = new();
                            input.Stage = stage;
                            input.Id = int.Parse(i.Attribute("element").Value);
                            input.HowMuch = int.Parse(i.Attribute("howMuch").Value);
                            input.Every = int.Parse(i.Attribute("consumeEvery").Value);
                            stage.Inputs.Add(input.Id, input);
                        }

                        foreach (XElement o in s.Element("harvestable")?.Element("products")?.Elements("l") ?? [])
                        {
                            ProductXml_Crop_Output output = new();
                            output.Stage = stage;
                            output.Id = int.Parse(o.Attribute("element").Value);
                            output.HowMuch = int.Parse(o.Attribute("howMuch").Value);
                            output.Every = int.Parse(o.Attribute("produceEvery").Value);
                            stage.Outputs.Add(output.Id, output);
                        }

                        foreach (XElement a in s.Element("anims")?.Elements("l") ?? [])
                        {
                            ProductXml_Crop_Animation animation = new();
                            animation.Stage = stage;
                            animation.AnimationId = a.Element("animation").Attribute("aid").Value;
                            animation.AnimationType = Enum.Parse<EAnimationType>(a.Attribute("type").Value);
                            animation.Direction = Enum.Parse<EDirection>(a.Attribute("dir").Value);
                            animation.UseFlipped = bool.Parse(a.Attribute("useFlipped").Value);
                            stage.Animations.Add(animation);
                        }

                        crop.Stages.Add(stage.Id, stage);
                    }
                    break;

                // NOT IMPLEMENTED
                default:
                    throw new NotImplementedException($"{nameof(product.ProductType)} = {product.ProductType}");
            }
            Products.Add(product.Id, product);
        }

        // Bind list of products:
        foreach (ProductXml p in Products.Values.Where(p => p.Process?.HasListOfProcesses ?? false))
            foreach (int id in p.Process.Children.Keys)
                p.Process.Children[id] = Products[id];

        // Done.
        return true;
    }

}
