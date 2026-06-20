using SH.Content.Enums;

namespace SH.Content.Xml.Haven.Products;

/// <summary>
/// [product] Product definition: Process, Crop or Elementary. Links to a carried item type.
/// </summary>
public sealed class ProductXml
{
    /// <summary>
    /// [eid]
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// [name]
    /// </summary>
    public int? NameId { get; set; }

    /// <summary>
    /// [desc]
    /// </summary>
    public int? DescriptionId { get; set; }



    /// <summary>
    /// [type]
    /// </summary>
    public EProductType ProductType { get; set; }

    /// <summary>
    /// Elementary data
    /// </summary>
    public ProductXml_Elementary Elementary { get; set; }

    /// <summary>
    /// Normal process data
    /// </summary>
    public ProductXml_Process Process { get; set; }

    /// <summary>
    /// Crop process data
    /// </summary>
    public ProductXml_Crop Crop { get; set; }
}
