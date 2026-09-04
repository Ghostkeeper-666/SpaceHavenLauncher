using SH.Content.Xml.Textures;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Content.Art;

public sealed class SpriteSheet
{
    private SpriteSheet(TextureXml textureXml, string cimFilePath)
    {
        TextureXml = textureXml;
        CimFilePath = cimFilePath;
        FileName = Path.GetFileName(CimFilePath);
        Name = int.Parse(Path.GetFileNameWithoutExtension(CimFilePath));
    }

    public TextureXml TextureXml { get; }
    public string CimFilePath { get; }
    public int Name { get; }
    public string FileName { get; }
    public int Area => Width * Height;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int PixelFormat { get; private set; }
    public byte[] PixelData { get; private set; }
    public OrderedDictionary<int, Sprite> SpritesByName { get; } = [];
    public OrderedDictionary<int, Sprite> SpritesById { get; } = [];

    public static bool TryLoad(string cimFilePath, TextureXml textureXml, out SpriteSheet spriteSheet, ILogger log)
    {
        try
        {
            spriteSheet = new(textureXml, cimFilePath);
            spriteSheet.Load(log);

            foreach (TextureRegionXml region in spriteSheet.TextureXml.RegionsByName.Values)
            {
                Sprite sprite = new(spriteSheet, region);
                if (!sprite.TryReadPixelData(log))
                {
                    log?.Error($"[{spriteSheet.Name}] Unable to read sprite pixel data from texture region {region.Name}");
                    continue;
                }

                if (!spriteSheet.SpritesByName.TryAdd(region.Name, sprite))
                {
                    int name = region.Name;
                    bool isSameImage = spriteSheet.SpritesByName[name].Equals(sprite);
                    string comparisonText = isSameImage ? "identical" : "DIFFERENT";
                    string message = $@"Ignoring sprite image in sprite sheet ""{spriteSheet.Name}"" with a DUPLICATE REGION NAME=""{name}"": it was reused for {comparisonText} sprite image content";

                    if (isSameImage) log?.Debug(message);
                    else log?.Warn(message);
                }

                if (!spriteSheet.SpritesById.TryAdd(region.Id, sprite))
                {
                    int id = region.Id;
                    bool isSameImage = spriteSheet.SpritesById[id].Equals(sprite);
                    string comparisonText = isSameImage ? "identical" : "DIFFERENT";
                    string message = $@"Ignoring sprite image in sprite sheet ""{spriteSheet.Name}"" with a DUPLICATE REGION ID=""{id}"": it was reused for {comparisonText} sprite image content";

                    if (isSameImage || id == 0) log?.Debug(message);
                    else log?.Warn(message);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            log?.Error(ex);
            spriteSheet = null;
            return false;
        }
    }

    private void Load(ILogger log)
    {
        static int readInt32BigEndian(Stream s)
        {
            Span<byte> buffer = stackalloc byte[4];
            s.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        using (FileStream fs = new(CimFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: false))
        using (ZLibStream zs = new(fs, CompressionMode.Decompress))
        {
            Width = readInt32BigEndian(zs);
            Height = readInt32BigEndian(zs);
            PixelFormat = readInt32BigEndian(zs);
            if (PixelFormat != 4)
                log?.Info($@"WARNING: Unexpected PixelFormat={PixelFormat}bytes for CIM file ""{CimFilePath}""");
            PixelData = new byte[4 * Width * Height];
            zs.ReadExactly(PixelData);
        }
    }

    public bool TryWrite(string cimFilePath, ILogger log)
    {
        try
        {
            using FileStream fs = new(
                cimFilePath,
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

    public async Task<bool> TryExportSpritesToPngAsync(string exportDir, ILogger log, CancellationToken ct)
    {
        try
        {
            log?.Debug($"[{FileName}] Exporting individual sprites to PNG");

            exportDir = IOUtils.CombineAsOSPath(exportDir, Name.ToString());
            if (!IOUtils.DirExists(exportDir))
                try { IOUtils.TryCreateDir(exportDir, log); } catch { }

            foreach (Sprite sprite in SpritesByName.Values)
            {
                string exportPath = IOUtils.CombineAsOSPath(exportDir, $"{sprite.Name}.png");
                await sprite.TryExportToPngAsync(exportPath, log, ct);
            }

            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    public async Task<bool> TryExportToPngAsync(string exportDir, ILogger log, CancellationToken ct)
    {
        try
        {
            if (!IOUtils.DirExists(exportDir))
                try { IOUtils.TryCreateDir(exportDir, log); } catch { }

            using Image<Rgba32> image = new(Width, Height, new Rgba32(0, 0, 0, 0));

            int pos = 0;
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    byte r = PixelData[pos++];
                    byte g = PixelData[pos++];
                    byte b = PixelData[pos++];
                    byte a = PixelData[pos++];
                    image[x, y] = new Rgba32(r, g, b, a);
                }
            }

            PngEncoder encoder = new()
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                FilterMethod = PngFilterMethod.Adaptive,
                ColorType = PngColorType.RgbWithAlpha,
                TransparentColorMode = PngTransparentColorMode.Preserve,
            };

            await image.SaveAsync(IOUtils.CombineAsOSPath(exportDir, $"{Name}.png"), encoder, ct);
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    public override string ToString() => Name.ToString();
}

