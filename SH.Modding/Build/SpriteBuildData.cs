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

internal sealed class SpriteBuildData : IEquatable<SpriteBuildData>, IAsyncDisposable
{
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
    }

    public SpriteSheetBuildData SpriteSheet { get; set; }

    public string GlobalName { get; set; }
    public string LocalName { get; set; }

    public int GlobalId { get; set; }
    public int LocalId { get; set; }

    public int SpriteSheetX { get; set; }
    public int SpriteSheetY { get; set; }

    public int Width { get; }
    public int Height { get; }

    public string FileName => Path.GetFileNameWithoutExtension(AbsoluteFilePath);
    public string AbsoluteFilePath { get; }

    public byte[] PixelData { get; }
    public SKBitmap Image { get; private set; }

    public bool TryExportToPng(string path, ILogger log, CancellationToken ct)
    {
        try
        {
            string dir = Path.GetDirectoryName(path);
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

    public async Task<bool> TryExportToPngAsync(string path, ILogger log = null)
    {
        try
        {
            string dir = Path.GetDirectoryName(path);
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

            byte[] bytes = data.ToArray();

            await stream.WriteAsync(bytes);
            return true;
        }
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


    #region IAsyncDisposable
    public volatile bool IsDisposed;
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        try { Image?.Dispose(); } catch { }
    }
    #endregion
}
