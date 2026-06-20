namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_NoiseAndComfort
{
    public bool OnlyWhenInUse { get; }
    public EComfortCategory Category { get; }

    public bool HasComfortRadiusBeauty { get; }
    public int ComfortRadiusBeauty_RoomDrop { get; }
    public int ComfortRadiusBeauty_Radius { get; }
    public int ComfortRadiusBeauty_Work { get; }
    public int ComfortRadiusBeauty_Sleep { get; }
    public int ComfortRadiusBeauty_Leisure { get; }

    public bool HasComfortVisualBeauty { get; }
    public int ComfortVisualBeauty_Radius { get; }
    public int ComfortVisualBeauty_Work { get; }
    public int ComfortVisualBeauty_Sleep { get; }
    public int ComfortVisualBeauty_Leisure { get; }

    public bool HasNoiseTaskStart { get; }
    public bool NoiseTaskStart_IgnoreZoomLevel { get; }
    public float NoiseTaskStart_Volume { get; } // noise
    public int? NoiseTaskStart_AudioId { get; }

    public bool HasNoise { get; }
    public bool Noise_IgnoreZoomLevel { get; }
    public double Noise_Volume { get; } // noise
    public int? Noise_AudioId { get; }

    public bool HasConstantSound { get; }
    public int? ConstantSound_StandBy_AudioId { get; }
    public int? ConstantSound_InUse_AudioId { get; }
}
