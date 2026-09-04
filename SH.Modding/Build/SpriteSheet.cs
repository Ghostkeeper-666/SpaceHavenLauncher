using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Build;

internal sealed class SpriteSheet : IDisposable
{
    public SpriteSheet(int localId, int width, int height, int maxSprites, int spriteSpacing, SpriteAtlas atlas)
    {
        Atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        LocalId = localId;
        Width = width;
        Height = height;
        PixelFormat = 4;
        PixelData = new byte[PixelFormat * Width * Height];
        SpriteSpacing = spriteSpacing;
        Packer = new(Width, Height, maxSprites);
    }

    public SpriteSheet(int localId, string imagePath, SpriteAtlas atlas)
    {
        Atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        LocalId = localId;
        Packer = null;

        if (imagePath.EndsWith(".cim", StringComparison.OrdinalIgnoreCase))
        {
            static int ReadInt32BigEndian(Stream stream)
            {
                Span<byte> buffer = stackalloc byte[4];
                stream.ReadExactly(buffer);
                return BinaryPrimitives.ReadInt32BigEndian(buffer);
            }
            using FileStream fs = new(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            using ZLibStream zs = new(fs, CompressionMode.Decompress);
            Width = ReadInt32BigEndian(zs);
            Height = ReadInt32BigEndian(zs);
            PixelFormat = ReadInt32BigEndian(zs);
            if (PixelFormat != 4)
                throw new InvalidDataException($"Expected pixel format = 4, read pixel format = {PixelFormat}");
            PixelData = new byte[PixelFormat * Width * Height];
            zs.ReadExactly(PixelData);
        }
        else
        {
            using FileStream fs = new(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            using SKCodec codec = SKCodec.Create(fs) ?? throw new InvalidDataException($"Unable to decode image '{imagePath}'.");
            Width = codec.Info.Width;
            Height = codec.Info.Height;
            PixelFormat = 4;
            PixelData = new byte[PixelFormat * Width * Height];
            GCHandle handle = GCHandle.Alloc(PixelData, GCHandleType.Pinned);
            try
            {
                SKImageInfo info = new(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                SKCodecResult result = codec.GetPixels(info, handle.AddrOfPinnedObject());
                if (result != SKCodecResult.Success)
                    throw new InvalidDataException($"Unable to decode image '{imagePath}'.");
            }
            finally
            {
                handle.Free();
            }
        }
    }

    public SpriteAtlas Atlas { get; private set; }
    public int GlobalId { get; set; } = int.MinValue;
    public int LocalId { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int PixelFormat { get; }
    public byte[] PixelData { get; private set; }

    internal SpritePacker Packer { get; private set; }
    internal bool IsPredefined => Packer == null;
    internal bool IsRendered { get; private set; }

    public IReadOnlyList<Sprite> Sprites => SpriteList;
    private List<Sprite> SpriteList = [];

    public int Count => SpriteList.Count;
    public int SpriteSpacing { get; set; }

    public OrderedDictionary<string, Sprite> SpritesByName { get; private set; } = [];
    public OrderedDictionary<string, Sprite> SpritesById { get; private set; } = [];

    public void Add(Sprite sprite)
    {
        ArgumentNullException.ThrowIfNull(sprite);

        SpriteList.Add(sprite);
        sprite.SpriteSheet = this;
    }

    public void Clear()
    {
        SpriteList.Clear();
        PixelData = new byte[PixelFormat * Width * Height];
        IsRendered = false;
    }

    public void Resize(int width, int height)
    {
        if (width == Width && height == Height)
            return;

        Width = width;
        Height = height;
        PixelData = new byte[PixelFormat * Width * Height];
        Packer = Packer == null ? null : new(Width, Height, Packer.MaxRectangles);
        IsRendered = false;
    }

    public bool TryRenderFromSprites(ILogger log, CancellationToken ct)
    {
        try
        {
            Sprite largestSprite = SpriteList.MaxBy(x => x.Area);
            if (largestSprite == null)
                return false;

            byte[] decodeBuffer = new byte[largestSprite.Area * PixelFormat];
            GCHandle handle = GCHandle.Alloc(decodeBuffer, GCHandleType.Pinned);
            try
            {
                IntPtr buffer = handle.AddrOfPinnedObject();
                foreach (Sprite sprite in SpriteList)
                {
                    ct.ThrowIfCancellationRequested();
                    using FileStream fs = new(sprite.AbsoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
                    using SKCodec codec = SKCodec.Create(fs) ?? throw new InvalidDataException($"Unable to decode sprite '{sprite.AbsoluteFilePath}'.");
                    SKImageInfo info = new(sprite.Width, sprite.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                    SKCodecResult result = codec.GetPixels(info, buffer);
                    if (result != SKCodecResult.Success)
                        throw new InvalidDataException($"Unable to decode sprite '{sprite.AbsoluteFilePath}'.");
                    int rowSize = sprite.Width * PixelFormat;
                    for (int row = 0; row < sprite.Height; row++)
                        Buffer.BlockCopy(decodeBuffer, row * rowSize, PixelData, ((sprite.Y + row) * Width + sprite.X) * PixelFormat, rowSize);
                }
            }
            finally
            {
                handle.Free();
            }
            IsRendered = true;
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
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
            if (!IsPredefined && !IsRendered && !TryRenderFromSprites(log, ct))
                return false;
            string dir = path.GetParentDirAsOSPath();
            if (!dir.IsNullOrWhiteSpace() && !IOUtils.TryCreateDir(dir, log))
                return false;
            await using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await using ZLibStream zs = new(fs, CompressionLevel.Fastest);
            byte[] header = new byte[12];
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), Width);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), Height);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), PixelFormat);
            await zs.WriteAsync(header, ct).ConfigureAwait(false);
            await zs.WriteAsync(PixelData, ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
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
            if (!IsPredefined && !IsRendered && !TryRenderFromSprites(log, ct))
                return false;
            string dir = path.GetParentDirAsOSPath();
            if (!dir.IsNullOrWhiteSpace() && !IOUtils.TryCreateDir(dir, log))
                return false;
            using SKBitmap bitmap = new(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            Marshal.Copy(PixelData, 0, bitmap.GetPixels(), PixelData.Length);
            using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            await using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true);
            await stream.WriteAsync(data.ToArray(), ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
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

        Atlas = null;
        PixelData = null;

        SpritesByName?.Clear();
        SpritesByName = null;

        SpritesById?.Clear();
        SpritesById = null;

        foreach (Sprite sprite in SpriteList ?? [])
            sprite?.Dispose();

        SpriteList?.Clear();
        SpriteList = null;

        Packer?.Dispose();
        Packer = null;
    }
    #endregion
}