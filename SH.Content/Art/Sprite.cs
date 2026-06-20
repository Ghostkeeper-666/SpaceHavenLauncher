using SH.Framework.Logging;
using SH.Content.Xml.Textures;
using SkiaSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Content.Art;

public sealed class Sprite : IEquatable<Sprite>
{
    public Sprite(SpriteSheet cim, TextureRegionXml region)
    {
        SpriteSheet = cim;
        Region = region;
        PixelData = new byte[4 * Width * Height];
    }

    public SpriteSheet SpriteSheet { get; }
    public TextureRegionXml Region { get; }
    public byte[] PixelData { get; }

    public int Name => Region.Name;
    public int Id => Region.Id;
    public int X => Region.X;
    public int Y => Region.Y;
    public int Area => Width * Height;
    public int Width => Region.Width;
    public int CroppedWidth => Region.Width; // TODO
    public int Height => Region.Height;
    public int CroppedHeight => Region.Height; // TODO

    public SKBitmap SKBitmap { get; private set; }


    public bool TryReadPixelData(ILogger logger)
    {
        try
        {
            for (int y = 0; y < Height; ++y)
                Buffer.BlockCopy(SpriteSheet.PixelData, (Y + y) * 4 * SpriteSheet.Width + X * 4, PixelData, y * 4 * Width, 4 * Width);

            CreateSKBitmap();

            return true;
        }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
    }

    public void CreateSKBitmap()
    {
        SKBitmap = new(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        int expectedSize = Width * Height * 4;
        if (PixelData.Length != expectedSize)
            throw new ArgumentException("Invalid data size");
        Marshal.Copy(PixelData, 0, SKBitmap.GetPixels(), PixelData.Length);
    }

    public async Task<bool> TryExportToPngAsync(string path, ILogger logger, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using SKImage image = SKImage.FromBitmap(SKBitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            using FileStream stream = File.OpenWrite(path);
            ct.ThrowIfCancellationRequested();
            await Task.Run(() => data.SaveTo(stream), ct);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch(Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
    }


    bool IEquatable<Sprite>.Equals(Sprite other)
    {
        if (Width != other.Width)
            return false;

        if (Height != other.Height)
            return false;

        int pos = 0;
        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                byte r1 = PixelData[pos];
                byte r2 = other.PixelData[pos++];
                if (r1 != r2)
                    return false;

                byte g1 = PixelData[pos];
                byte g2 = other.PixelData[pos++];
                if (g1 != g2)
                    return false;

                byte b1 = PixelData[pos];
                byte b2 = other.PixelData[pos++];
                if (b1 != b2)
                    return false;

                byte a1 = PixelData[pos];
                byte a2 = other.PixelData[pos++];
                if (a1 != a2)
                    return false;
            }
        }
        return true;
    }

    public override string ToString() => Name.ToString();
}