using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.RectPack;
using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Build;

internal sealed class SpriteSheetBuildData : IDisposable
{
    public SpriteSheetBuildData(int localId, int width, int height, int maxSprites, int spriteSpacing, SpriteAtlasBuildData atlas)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        Atlas = atlas;

        LocalId = localId;
        Width = width;
        Height = height;
        PixelFormat = 4;
        PixelData = new byte[PixelFormat * Width * Height];
        SpriteSpacing = spriteSpacing;
        Packer = new(Width, Height, maxSprites);
    }

    public SpriteSheetBuildData(int localId, string path, SpriteAtlasBuildData atlas)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        Atlas = atlas;

        LocalId = localId;

        static int readInt32BigEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        using (FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, false))
        using (ZLibStream zs = new(fs, CompressionMode.Decompress))
        {
            Width = readInt32BigEndian(zs);
            Height = readInt32BigEndian(zs);
            PixelFormat = readInt32BigEndian(zs);
            if (PixelFormat != 4)
                throw new Exception($"Expected pixel format = 4, read pixel format = {PixelFormat}");
            PixelData = new byte[PixelFormat * Width * Height];
            zs.ReadExactly(PixelData);
        }
    }

    public SpriteAtlasBuildData Atlas { get; }
    public int GlobalId { get; set; } = int.MinValue;
    public int LocalId { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int PixelFormat { get; }
    public byte[] PixelData { get; private set; }
    internal SpritePacker Packer { get; private set; }

    public IReadOnlyList<SpriteBuildData> Sprites => SpriteList;
    private List<SpriteBuildData> SpriteList = [];
    public int Count => SpriteList.Count;

    public int SpriteSpacing { get; set; }

    public OrderedDictionary<string, SpriteBuildData> SpritesByName { get; } = [];
    public OrderedDictionary<string, SpriteBuildData> SpritesById { get; } = [];

    public void Add(SpriteBuildData sprite)
    {
        SpriteList.Add(sprite);
        sprite.SpriteSheet = this;
    }

    public void Clear()
    {
        SpriteList.Clear();
        PixelData = new byte[PixelFormat * Width * Height];
    }

    public void Resize(int width, int height)
    {
        if (width == Width && height == Height)
            return;
        Width = width;
        Height = height;
        PixelData = new byte[PixelFormat * Width * Height];
        Packer = new(Width, Height, Packer.MaxRectangles);
    }

    public bool TryGenerateFromSprites(ILogger log = null)
    {
        try
        {
            using SKBitmap bitmap = new(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            using SKCanvas canvas = new(bitmap);
            foreach (SpriteBuildData sprite in SpriteList)
                canvas.DrawBitmap(sprite.Image, sprite.SpriteSheetX, sprite.SpriteSheetY);

            Marshal.Copy(bitmap.GetPixels(), PixelData, 0, PixelData.Length);
            return true;
        }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }


    public bool TryExportToCim(string path, ILogger log = null)
    {
        try
        {
            using FileStream fs = new(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: false);

            using ZLibStream zs = new(fs, CompressionLevel.SmallestSize, leaveOpen: false);

            static void writeInt32BigEndian(Stream s, int value)
            {
                Span<byte> buffer = stackalloc byte[4];
                BinaryPrimitives.WriteInt32BigEndian(buffer, value);
                s.Write(buffer);
            }

            writeInt32BigEndian(zs, Width);
            writeInt32BigEndian(zs, Height);
            writeInt32BigEndian(zs, 4);
            zs.Write(PixelData, 0, PixelData.Length);
            zs.Flush();

            return true;
        }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    public async Task<bool> TryExportToCimAsync(string path, ILogger log, CancellationToken ct)
    {
        try
        {
            await using FileStream fs = new(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await using ZLibStream zs = new(fs, CompressionLevel.SmallestSize, leaveOpen: false);

            // Build the full payload in memory (header + pixel data)
            byte[] header = new byte[12]; // 3 × 4 bytes
            {
                Span<byte> width = header.AsSpan(0, 4);
                BinaryPrimitives.WriteInt32BigEndian(width, Width);
            }
            {
                Span<byte> height = header.AsSpan(4, 4);
                BinaryPrimitives.WriteInt32BigEndian(height, Height);
            }
            {
                Span<byte> buffer = header.AsSpan(8, 4);
                BinaryPrimitives.WriteInt32BigEndian(buffer, 4);
            }

            using MemoryStream ms = new(header.Length + PixelData.Length);
            ms.Write(header, 0, header.Length);
            ms.Write(PixelData, 0, PixelData.Length);
            ms.Position = 0;

            await ms.CopyToAsync(zs, ct).ConfigureAwait(false);
            await zs.FlushAsync(ct).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    public bool TryExportAllSpritesToPng(string exportDir, ILogger log, CancellationToken ct)
    {
        try
        {
            exportDir = Path.Combine(exportDir, LocalId.ToString());
            if (!IOUtils.TryCreateDirectory(exportDir, log))
                return false;

            foreach (SpriteBuildData sprite in SpritesByName.Values)
            {
                string exportPath = Path.Combine(exportDir, $"{sprite.LocalId}.png");
                sprite.TryExportToPng(exportPath, log, ct);
            }
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex, exportDir);
            return false;
        }
    }

    public bool TryExportToPng(string path, ILogger log, CancellationToken ct)
    {
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!dir.IsNullOrWhiteSpace() && !IOUtils.TryCreateDirectory(dir, log))
                return false;

            ct.ThrowIfCancellationRequested();

            SKBitmap bitmap = new(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            Marshal.Copy(PixelData, 0, bitmap.GetPixels(), PixelData.Length);

            ct.ThrowIfCancellationRequested();

            using SKImage image = SKImage.FromBitmap(bitmap);
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

    public override string ToString() => LocalId.ToString();

    #region IDisposable
    public volatile bool IsDisposed;
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        PixelData = null;
        Packer = null;
        foreach (SpriteBuildData sprite in SpriteList)
            sprite?.Dispose();
        SpriteList = null;
    }
    #endregion
}

