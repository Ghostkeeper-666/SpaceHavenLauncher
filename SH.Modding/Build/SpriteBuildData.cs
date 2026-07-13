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

internal sealed class SpriteBuildData : IEquatable<SpriteBuildData>, IDisposable
{
    public SpriteBuildData(string localName, int localId, SpriteSheetBuildData spriteSheet, int width, int height, int x, int y)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localName);
        ArgumentNullException.ThrowIfNull(spriteSheet);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0, nameof(width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0, nameof(height));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0, nameof(x));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0, nameof(y));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, spriteSheet.Width, nameof(x));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, spriteSheet.Height, nameof(y));

        Width = width;
        Height = height;
        Area = Width * Height;
        PixelData = new byte[4 * Area];

        LocalName = localName;
        LocalId = localId;
        AbsoluteFilePath = null;

        SpriteSheet = spriteSheet;
        X = x;
        Y = y;
    }

    public SpriteBuildData(string localName, int localId, string absoluteFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localName);
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteFilePath);

        LocalName = localName;
        LocalId = localId;
        AbsoluteFilePath = absoluteFilePath.AsOSPath();

        // Read image pixel data from file:
        Image = SKBitmap.Decode(absoluteFilePath);
        PixelData = new byte[Image.ByteCount];
        Marshal.Copy(Image.GetPixels(), PixelData, 0, PixelData.Length);

        Width = Image.Width;
        Height = Image.Height;
        Area = Width * Height;
    }

    public bool TryRenderFromSpriteSheet(ILogger log = null)
    {
        try
        {
            int rowSize = Width * 4;
            for (int row = 0; row < Height; ++row)
                Buffer.BlockCopy(SpriteSheet.PixelData, (Y + row) * SpriteSheet.Width * 4 + X * 4, PixelData, row * Width * 4, rowSize);
            return true;
        }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    public SpriteSheetBuildData SpriteSheet { get; set; }

    public string GlobalName { get; set; }
    public string LocalName { get; set; }

    public int GlobalId { get; set; }
    public int LocalId { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public int Width { get; }
    public int Height { get; }

    public int Area { get; }

    public string FileName => AbsoluteFilePath.GetFileNameWithoutExtension();
    public string AbsoluteFilePath { get; }

    public byte[] PixelData { get; private set; }
    public SKBitmap Image { get; private set; }

    [Obsolete("Use TryExportToPngAsync() instead!")]
    public bool TryExportToPng(string path, ILogger log, CancellationToken ct)
    {
        try
        {
            string dir = path.GetParentDirAsOSPath();
            if (!dir.IsNullOrWhiteSpace() && !IOUtils.TryCreateDirectory(dir, log))
                return false;

            ct.ThrowIfCancellationRequested();

            using SKImage image = SKImage.FromBitmap(Image);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            using FileStream stream = File.OpenWrite(path);

            ct.ThrowIfCancellationRequested();

            data.SaveTo(stream);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    public async Task<bool> TryExportToPngAsync(string path, ILogger log, CancellationToken ct)
    {
        try
        {
            string dir = path.GetParentDirAsOSPath();
            if (!dir.IsNullOrWhiteSpace() && !IOUtils.TryCreateDirectory(dir, log))
                return false;

            using SKImage image = SKImage.FromBitmap(Image);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            await using FileStream stream = new(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                65536,
                useAsync: true);

            await stream.WriteAsync(data.ToArray(), ct);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    public bool Equals(SpriteBuildData other)
    {
        if (other is null)
            return false;
        if (Width != other.Width || Height != other.Height)
            return false;
        return PixelData.AsSpan().SequenceEqual(other.PixelData);
    }

    public override bool Equals(object obj) =>
        obj is SpriteBuildData other && Equals(other);

    public override string ToString() => LocalName.ToString();

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Width);
        hash.Add(Height);
        foreach (byte b in PixelData)
            hash.Add(b);
        return hash.ToHashCode();
    }


    #region IDisposable
    public volatile bool IsDisposed;
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        PixelData = null;
        try { Image?.Dispose(); } catch { }
        Image = null;
    }
    #endregion
}
