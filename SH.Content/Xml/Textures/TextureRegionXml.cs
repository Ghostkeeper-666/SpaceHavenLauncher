namespace SH.Content.Xml.Textures;

public sealed class TextureRegionXml
{
    public TextureRegionXml(TextureXml texture, int name, int id, int x, int y, int width, int height)
    {
        Texture = texture;
        Name = name;
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public TextureXml Texture { get; }
    public int Name { get; }
    public int Id { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
}
