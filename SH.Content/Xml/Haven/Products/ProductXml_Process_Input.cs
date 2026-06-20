namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Process_Input
{
    public ProductXml_Process Process { get; set; }

    /// <summary>
    /// [needs/l/element]
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// [needs/l/howMuch]
    /// </summary>
    public int HowMuch { get; set; }

    /// <summary>
    /// [needs/l/consumeEvery]
    /// </summary>
    public int Every { get; set; }
}
