namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Crop_Input
{
    public ProductXml_Crop_Stage Stage { get; set; }

    /// <summary>
    /// [stages/l/needs/l/element]
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// [stages/l/needs/l/howMuch]
    /// </summary>
    public int HowMuch { get; set; }

    /// <summary>
    /// [stages/l/needs/l/consumeEvery]
    /// </summary>
    public int Every { get; set; }
}