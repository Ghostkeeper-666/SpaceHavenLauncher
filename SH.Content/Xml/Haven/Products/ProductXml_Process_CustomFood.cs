using SH.Content.Enums;

namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Process_CustomFood
{
    public ProductXml_Process Process { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/foodUsageType]
    /// </summary>
    public EFoodType FoodType { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/protein] Amount of protein
    /// </summary>
    public float Protein { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/carbs] Amount of carbs
    /// </summary>
    public float Carbs { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/fat] Amount of fat
    /// </summary>
    public float Fat { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/vitamins] Amount of vitamins
    /// </summary>
    public float Vitamins { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/toxins] Amount of toxins
    /// </summary>
    public float Toxins { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/rank] How much is it ranked by humans consumption preference?
    /// </summary>
    public float Rank { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/canBeProcessed] Can be used as ingredient for other food
    /// </summary>
    public bool IsIngredient { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/raw] Is raw food? (e.g. crop or meat)
    /// </summary>
    public bool IsRawFood { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/goodFood] Is good food? (triggers good food conditions)
    /// </summary>
    public bool IsGoodFood { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/useByDefaultValue] Default amount (when used as ingredient for other food)
    /// </summary>
    public int DefaultAmountToCreateOtherFood { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/sortOrder] Sort order (when used as ingredient for other food)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/neverEat] Not eadible (e.g. IV Fluid)
    /// </summary>
    public bool NeverEat { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood/foodtype/ma] Is alcohol? (triggers alcohol conditions)
    /// </summary>
    public bool IsAlcohol { get; set; }
}
