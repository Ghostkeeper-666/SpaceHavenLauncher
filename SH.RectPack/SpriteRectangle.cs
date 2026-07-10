namespace SH.RectPack;

public struct SpriteRectangle
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int SortKey;
    public object Sprite;

    public readonly int Right => X + Width;

    public readonly int Bottom => Y + Height;

    public readonly int Area => Width * Height;

    public SpriteRectangle(int x, int y, int width, int height, object sprite)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        SortKey = 0;
        Sprite = sprite;
    }

    internal SpriteRectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        SortKey = 0;
    }

    public readonly bool IsEqual(in SpriteRectangle other) =>
        X == other.X &&
        Y == other.Y &&
        Width == other.Width &&
        Height == other.Height &&
        Equals(Sprite, other.Sprite);

    public override readonly string ToString() =>
        $"x={X} y={Y} w={Width} h={Height} tag={Sprite}";
}