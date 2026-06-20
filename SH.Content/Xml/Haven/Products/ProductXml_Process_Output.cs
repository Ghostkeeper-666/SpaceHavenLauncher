namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Process_Output
{
    public ProductXml_Process Process { get; set; }

    /// <summary>
    /// [products/l/element]
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// [products/l/howMuch]
    /// </summary>
    public int HowMuch { get; set; }

    /// <summary>
    /// [products/l/produceEvery]
    /// </summary>
    public int Every { get; set; }
}
