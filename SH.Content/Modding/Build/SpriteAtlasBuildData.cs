using CommonLibrary;
using RectpackSharp;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SH.Content.Modding.Build;

internal sealed class SpriteAtlasBuildData
{
    public string Name { get; }
    public List<SpriteSheetBuildData> SpriteSheets { get; } = [];

    public List<SpriteBuildData> Sprites
    {
        get
        {
            List<SpriteBuildData> list = [];
            foreach (SpriteSheetBuildData ss in SpriteSheets)
                foreach (SpriteBuildData s in ss.Sprites.OrderBy(s => s.AbsoluteFilePath))
                    list.Add(s);
            return list;
        }
    }

    public int SpriteCount =>
        SpriteSheets.Sum(sh => sh.Count);

    public SpriteAtlasBuildData(string name) =>
        Name = name ?? throw new ArgumentNullException(nameof(name));

    public void Clear() =>
        SpriteSheets.Clear();

    public SpriteBuildData GetSpriteWithGlobalId(int globalId) =>
        Sprites?.FirstOrDefault(s => s.GlobalId == globalId);
    public SpriteBuildData GetSpriteWithLocalId(int localId) =>
        Sprites?.FirstOrDefault(s => s.LocalId == localId);
    public SpriteBuildData GetSpriteWithGlobalName(string globalName) =>
        Sprites?.FirstOrDefault(s => s.LocalName.Equals(globalName, StringComparison.Ordinal));
    public SpriteBuildData GetSpriteWithLocalName(string localName) =>
        Sprites?.FirstOrDefault(s => s.LocalName.Equals(localName, StringComparison.Ordinal));

    public bool Add(IEnumerable<SpriteBuildData> sprites, uint maxSpriteSheetWidth, uint maxSpriteSheetHeight, bool crop, ILogger log, CancellationToken ct)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(sprites);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSpriteSheetWidth);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSpriteSheetHeight);

            // Nothing to do?
            if (!sprites.Any())
                return true;

            ct.ThrowIfCancellationRequested();

            // Collect all sprites of this atlas, then add the new ones:
            OrderedDictionary<int, SpriteBuildData> allSprites = [];
            allSprites.AddRange(Sprites, s => s.LocalId, s => s);
            allSprites.AddRange(sprites, s => s.LocalId, s => s);

            // Clear all spritesheets, since everything will be re-calculated:
            foreach (SpriteSheetBuildData spriteSheet in SpriteSheets)
                spriteSheet.Clear();

            ct.ThrowIfCancellationRequested();

            int maxLocalId = sprites.Max(s => s.LocalId);
            SpriteBuildData[] spritesByLocalId = new SpriteBuildData[maxLocalId + 1];
            foreach (SpriteBuildData sprite in sprites)
                spritesByLocalId[sprite.LocalId] = sprite;

            // Fit each sprite to a spritesheet:
            foreach (SpriteBuildData sprite in allSprites.Values.OrderByDescending(sprite => ((ulong)sprite.Width) * ((ulong)sprite.Height)))
            {
                ct.ThrowIfCancellationRequested();

                // Check for very large sprite:
                if (sprite.Width > maxSpriteSheetWidth || sprite.Height > maxSpriteSheetHeight)
                {
                    log?.Error($"Texture '{sprite.LocalName}' with size {sprite.Width}x{sprite.Height} does not fit into the maximum allowed size of {maxSpriteSheetWidth}x{maxSpriteSheetHeight}");
                    return false;
                }

                // Try to fit sprite to an existing spritesheet:
                bool added = false;
                foreach (SpriteSheetBuildData spriteSheet in SpriteSheets)
                {
                    ct.ThrowIfCancellationRequested();

                    // Resize spritesheet if it is below the maximum allowed size:
                    if (spriteSheet.Width < maxSpriteSheetWidth || spriteSheet.Height < maxSpriteSheetHeight)
                        spriteSheet.Resize((int)Math.Max(spriteSheet.Width, maxSpriteSheetWidth), (int)Math.Max(spriteSheet.Width, maxSpriteSheetHeight));

                    // Test if the spritesheet can hold this sprite:
                    if (!TryPack(spritesByLocalId, spriteSheet, sprite, out _, ct))
                        continue;

                    spriteSheet.Sprites.Add(sprite);
                    added = true;
                    break;
                }
                if (added)
                    continue;

                // Create a new sprite sheet, then add the sprite:
                SpriteSheets.Add(new(SpriteSheets.Count, (int)maxSpriteSheetWidth, (int)maxSpriteSheetHeight, this));
                SpriteSheets.Last().Sprites.Add(sprite);
            }

            // Pack each sprite:
            foreach (SpriteSheetBuildData spriteSheet in SpriteSheets)
            {
                ct.ThrowIfCancellationRequested();

                if (!TryPack(spritesByLocalId, spriteSheet, null, out PackingRectangle bounds, ct))
                {
                    log?.Error($"Unable to pack all textures to sprite sheet {spriteSheet}");
                    return false;
                }

                foreach (SpriteBuildData sprite in spriteSheet.Sprites)
                {
                    ct.ThrowIfCancellationRequested();

                    int distancingOffset = sprite.PackingRectangleHasBorder ? 1 : 0;
                    sprite.SpriteSheetX = distancingOffset + (int)sprite.PackingRectangle.X;
                    sprite.SpriteSheetY = distancingOffset + (int)sprite.PackingRectangle.Y;
                    sprite.SpriteSheet = spriteSheet;
                }

                // Crop spritesheet to its content:
                if (crop)
                {
                    int size = Math.Max(MathHelpers.NextPowerOfTwo((int)bounds.Width), MathHelpers.NextPowerOfTwo((int)bounds.Height));
                    spriteSheet.Resize(size, size);
                }
            }

            // Done.
            return true;
        }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    private bool TryPack(SpriteBuildData[] spritesByLocalId, SpriteSheetBuildData spriteSheet, SpriteBuildData additionalSprite, out PackingRectangle bounds, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        List<PackingRectangle> rectList = spriteSheet.Sprites.Select(CreatePackingRectangleForSprite).ToList();
        if (additionalSprite != null)
            rectList.Add(CreatePackingRectangleForSprite(additionalSprite));
        PackingRectangle[] rects = rectList.ToArray();

        ct.ThrowIfCancellationRequested();

        try
        {
            RectanglePacker.Pack(rects, out bounds, PackingHints.FindBest, 1.0, 1, (uint)spriteSheet.Width, (uint)spriteSheet.Height);
        }
        catch
        {
            bounds = default;
            return false;
        }

        uint usedWidth = 0;
        uint usedHeight = 0;
        foreach (PackingRectangle rect in rects)
        {
            ct.ThrowIfCancellationRequested();

            SpriteBuildData sprite = spritesByLocalId[rect.Id];
            sprite.PackingRectangle = rect; // set calculated rectangle
            uint right = rect.X + rect.Width;
            uint bottom = rect.Y + rect.Height;
            if (right > usedWidth)
                usedWidth = right;
            if (bottom > usedHeight)
                usedHeight = bottom;
        }

        return usedWidth <= spriteSheet.Width && usedHeight <= spriteSheet.Height;
    }


    private PackingRectangle CreatePackingRectangleForSprite(SpriteBuildData s) =>
        s.Width <= 510 && s.Height <= 510 ?
        new(0, 0, (uint)(s.Width + 2), (uint)(s.Height + 2), s.LocalId) :
        new(0, 0, (uint)s.Width, (uint)s.Height, s.LocalId);
}
