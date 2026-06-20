using SH.Content.Enums;
using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Crop
{
    public ProductXml Product { get; set; }

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
    /// [GUIAnimation/aid]
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// [plantingPenalty/penalty]
    /// </summary>
    public int PlantingPenalty { get; set; }

    /// <summary>
    /// [difficulty/skill]
    /// </summary>
    public string RequiredSkillName { get; set; }

    /// <summary>
    /// [difficulty/level]
    /// </summary>
    public int RequiredSkillLevel { get; set; }

    /// <summary>
    /// [stages]
    /// </summary>
    public OrderedDictionary<int, ProductXml_Crop_Stage> Stages { get; } = [];
}
