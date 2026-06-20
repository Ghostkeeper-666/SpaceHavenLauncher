using SH.Content.Enums;
using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Process
{
    public ProductXml Product { get; }

    /// <summary>
    /// [eid]
    /// </summary>
    public int Id => Product.Id;

    /// <summary>
    /// [name]
    /// </summary>
    public int? NameId => Product.NameId;

    /// <summary>
    /// [desc]
    /// </summary>
    public int? DescriptionId => Product.DescriptionId;

    /// <summary>
    /// [type]
    /// </summary>
    public EProductType ProductType => Product.ProductType;



    /// <summary>
    /// [list/processes/l/process] List of processes
    /// </summary>
    public OrderedDictionary<int, ProductXml> Children { get; } = [];
    public bool HasListOfProcesses => Children.Count > 0;



    /// <summary>
    /// [needs/l]
    /// </summary>
    public OrderedDictionary<int, ProductXml_Process_Input> Inputs { get; } = [];

    /// <summary>
    /// [products/l]
    /// </summary>
    public OrderedDictionary<int, ProductXml_Process_Output> Outputs { get; } = [];



    /// <summary>
    /// [interactive] Process: Does this process require a worker?
    /// </summary>
    public bool IsInteractiveProcess { get; set; }

    /// <summary>
    /// [startOnly] Process: DEPRECATED - always set to false
    /// </summary>
    public bool IsStartOnlyProcess { get; set; }

    /// <summary>
    /// [itemScrapper] Process: DEPRECATED - always set to false
    /// </summary>
    public bool IsItemScrapperProcess { get; set; }

    /// <summary>
    /// [smelter] Process: 
    /// </summary>
    public bool IsSmelterProcess { get; set; }

    /// <summary>
    /// [scrapper] Process: Is this a Recycler process?
    /// </summary>
    public bool IsScrapperProcess { get; set; }



    /// <summary>
    /// [accidentList/listId]
    /// </summary>
    public int? AccidentListId { get; set; }



    /// <summary>
    /// [difficulty/skill]
    /// </summary>
    public string SkillName { get; set; }
    public bool IsSkillDefined => SkillName != null;

    /// <summary>
    /// [difficulty/level]
    /// </summary>
    public int? SkillLevel { get; set; }

    /// <summary>
    /// [difficulty/custom/time]
    /// </summary>
    public int? CustomDuration { get; set; }
    public bool IsCustomDurationDefined => CustomDuration != null;



    /// <summary>
    /// [foodProcessing/foodUsageType]
    /// </summary>
    public EFoodType? FoodType { get; set; }

    /// <summary>
    /// [foodProcessing/ownProductsOnly]
    /// </summary>
    public bool? OwnFoodProductsOnly { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/capacity]
    /// </summary>
    public int? DispenserCapacity { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/timeToCharge]
    /// </summary>
    public int? DispenserTimeToCharge { get; set; }

    /// <summary>
    /// [foodProcessing/dispenser/customFood]
    /// </summary>
    public ProductXml_Process_CustomFood CustomFood { get; set; }
}
