using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Build;

internal sealed class Sprite : IDisposable
{
    public SpriteSheet SpriteSheet { get; set; }

    public string GlobalName { get; set; }
    public string LocalName { get; set; }

    public int GlobalId { get; set; }
    public int LocalId { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public int Width { get; }
    public int Height { get; }

    public int Area { get; }

    public string FileName => AbsoluteFilePath?.GetFileNameWithoutExtension();
    public string AbsoluteFilePath { get; private set; }

    public override string ToString() => LocalName;


    public Sprite(string localName, int localId, SpriteSheet spriteSheet, int width, int height, int x, int y)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localName);
        ArgumentNullException.ThrowIfNull(spriteSheet);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0, nameof(width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0, nameof(height));
        ArgumentOutOfRangeException.ThrowIfLessThan(x, 0, nameof(x));
        ArgumentOutOfRangeException.ThrowIfLessThan(y, 0, nameof(y));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(x + width, spriteSheet.Width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(y + height, spriteSheet.Height, nameof(height));

        LocalName = localName;
        LocalId = localId;
        Width = width;
        Height = height;
        Area = Width * Height;
        AbsoluteFilePath = null;
        SpriteSheet = spriteSheet;
        X = x;
        Y = y;
    }

    public Sprite(string localName, int localId, string absoluteFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localName);
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteFilePath);

        AbsoluteFilePath = absoluteFilePath.AsOSPath();
        using SKCodec codec = SKCodec.Create(AbsoluteFilePath)
            ?? throw new InvalidDataException($"Unable to decode image '{AbsoluteFilePath}'.");

        LocalName = localName;
        LocalId = localId;
        Width = codec.Info.Width;
        Height = codec.Info.Height;
        Area = Width * Height;
    }

    public async Task<bool> TryRenderFromSpriteSheetToPngAsync(string absolutePath, ILogger log = null, CancellationToken ct = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(SpriteSheet);

            AbsoluteFilePath = absolutePath.AsOSPath();
            string dir = AbsoluteFilePath.GetParentDirAsOSPath();
            if (!dir.IsNullOrWhiteSpace() && !IOUtils.TryCreateDir(dir, log))
                return false;

            int rowSize = Width * 4;
            byte[] pixelData = new byte[rowSize * Height];
            for (int row = 0; row < Height; ++row)
                Buffer.BlockCopy(SpriteSheet.PixelData, (Y + row) * SpriteSheet.Width * 4 + X * 4, pixelData, row * rowSize, rowSize);

            GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
            try
            {
                using SKBitmap bitmap = new();
                if (!bitmap.InstallPixels(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul), handle.AddrOfPinnedObject(), rowSize))
                    return false;
                using SKImage image = SKImage.FromBitmap(bitmap);
                using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
                await using FileStream fs = new(AbsoluteFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
                await fs.WriteAsync(data.ToArray(), ct).ConfigureAwait(false);
                return true;
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }



    #region IDisposable
    private int _disposed;
    public bool IsDisposed => _disposed != 0;
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        SpriteSheet = null;
        AbsoluteFilePath = null;
    }
    #endregion
}