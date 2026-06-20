using System.Collections.Generic;

namespace SH.Content.Xml.Textures;

public sealed class TextureXml
{
    public TextureXml(int id, int width, int height, int format, int min, int max)
    {
        Id = id;
        Width = width;
        Height = height;
        Format = format;
        Min = min;
        Max = max;
    }

    public int Id { get; }
    public int Width { get; }
    public int Height { get; }
    public int Format { get; }
    public int Min { get; }
    public int Max { get; }

    public OrderedDictionary<int, TextureRegionXml> RegionsByName { get; } = [];
}


