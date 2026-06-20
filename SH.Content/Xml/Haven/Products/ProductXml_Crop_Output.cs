namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Crop_Output
{
    public ProductXml_Crop_Stage Stage { get; set; }

    /// <summary>
    /// [stages/l/harvestable/products/l/element]
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// [stages/l/harvestable/products/l/howMuch]
    /// </summary>
    public int HowMuch { get; set; }

    /// <summary>
    /// [stages/l/harvestable/products/l/consumeEvery]
    /// </summary>
    public int Every { get; set; }
}