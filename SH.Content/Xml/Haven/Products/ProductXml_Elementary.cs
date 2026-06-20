using SH.Content.Enums;

namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Elementary
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
    /// [item] Elementary: The corresponding ItemId
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// [elementType]
    /// </summary>
    public EElementType ElementType { get; set; }
}
