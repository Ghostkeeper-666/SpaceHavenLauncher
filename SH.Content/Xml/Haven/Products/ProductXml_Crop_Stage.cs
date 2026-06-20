using SH.Content.Enums;
using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Crop_Stage
{
    public ProductXml_Crop Crop { get; set; }

    /// <summary>
    /// [stages/l/stage]
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// [stages/l/time]
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// [stages/l/co2PerMinute] Produced CO2 per minute
    /// </summary>
    public int CO2PerMinute { get; set; }

    /// <summary>
    /// [stages/l/oxygenPerMinute] Produced Oxygen per minute
    /// </summary>
    public int OxygenPerMinute { get; set; }

    /// <summary>
    /// [stages/l/h2oPerMinute] Produced Water Vapor per minute
    /// </summary>
    public int H2OPerMinute { get; set; }

    /// <summary>
    /// [stages/l/maxTemp] Maximum temperature
    /// </summary>
    public int TemperatureMax { get; set; }

    /// <summary>
    /// [stages/l/minTemp] Minimum temperature
    /// </summary>
    public int TemperatureMin { get; set; }

    /// <summary>
    /// [stages/l/lightNeeded] Minimum light required
    /// </summary>
    public ELightIntensity LightRequired { get; set; }

    /// <summary>
    /// [stages/l/tend/penalty]
    /// </summary>
    public int? Penalty { get; set; }

    /// <summary>
    /// [stages/l/needs/l]
    /// </summary>
    public OrderedDictionary<int, ProductXml_Crop_Input> Inputs { get; } = [];

    /// <summary>
    /// [stages/l/harvestable/products/l]
    /// </summary>
    public OrderedDictionary<int, ProductXml_Crop_Output> Outputs { get; } = [];

    /// <summary>
    /// [stages/l/anims/l]
    /// </summary>
    public List<ProductXml_Crop_Animation> Animations { get; } = [];

}
