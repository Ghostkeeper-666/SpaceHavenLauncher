using CommonLibrary;
using RectpackSharp;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SH.Content.Modding.Build;

internal sealed class SpriteAtlasBuildData
{
    public string Name { get; }
    public List<SpriteSheetBuildData> SpriteSheets { get; } = [];

    public List<SpriteBuildData> Sprites =>
        SpriteSheets.SelectMany(sh => sh.Sprites).ToList();
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
    public SpriteBuildData GetSpriteWithFileName(string filename) =>
        Sprites?.FirstOrDefault(s => s.FileName.Equals(filename.RemoveSuffix(".png", StringComparison.OrdinalIgnoreCase), StringComparison.Ordinal));

    public bool Add(IEnumerable<SpriteBuildData> sprites, uint maxSpriteSheetWidth, uint maxSpriteSheetHeight, bool crop, ILogger log)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(sprites);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSpriteSheetWidth);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSpriteSheetHeight);

            // Nothing to do?
            if (!sprites.Any())
                return true;

            // Collect all sprites of this atlas, then add the new ones:
            OrderedDictionary<int, SpriteBuildData> allSprites = [];
            allSprites.AddRange(Sprites, s => s.LocalId, s => s);
            allSprites.AddRange(sprites, s => s.LocalId, s => s);

            // Clear all spritesheets, since everything will be re-calculated:
            foreach (SpriteSheetBuildData spriteSheet in SpriteSheets)
                spriteSheet.Clear();

            // Fit each sprite to a spritesheet:
            foreach (SpriteBuildData sprite in allSprites.Values.OrderByDescending(sprite => ((ulong)sprite.Width) * ((ulong)sprite.Height)))
            {
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
                    // Resize spritesheet if it is below the maximum allowed size:
                    if (spriteSheet.Width < maxSpriteSheetWidth || spriteSheet.Height < maxSpriteSheetHeight)
                        spriteSheet.Resize((int)Math.Max(spriteSheet.Width, maxSpriteSheetWidth), (int)Math.Max(spriteSheet.Width, maxSpriteSheetHeight));

                    // Test if the spritesheet can hold this sprite:
                    if (!TryPack(spriteSheet, sprite, out _, out _))
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
                if (!TryPack(spriteSheet, null, out PackingRectangle[] rects, out PackingRectangle bounds))
                {
                    log?.Error($"Unable to pack all textures to sprite sheet {spriteSheet}");
                    return false;
                }

                foreach (PackingRectangle rect in rects)
                {
                    SpriteBuildData sprite = spriteSheet.Sprites.First(sprite => sprite.LocalId == rect.Id);
                    sprite.SpriteSheetX = (int)rect.X;
                    sprite.SpriteSheetY = (int)rect.Y;
                    sprite.Sheet = spriteSheet;
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

    private bool TryPack(SpriteSheetBuildData spriteSheet, SpriteBuildData additionalSprite, out PackingRectangle[] rects, out PackingRectangle bounds)
    {
        if (additionalSprite == null)
        {
            rects = spriteSheet.Sprites.Select(sprite => new PackingRectangle(0, 0, (uint)sprite.Width, (uint)sprite.Height, sprite.LocalId)).ToArray();
        }
        else
        {
            PackingRectangle rect = new(0, 0, (uint)additionalSprite.Width, (uint)additionalSprite.Height, additionalSprite.LocalId);
            rects = spriteSheet.Sprites.Select(sprite => new PackingRectangle(0, 0, (uint)sprite.Width, (uint)sprite.Height, sprite.LocalId)).Append(rect).ToArray();
        }

        RectanglePacker.Pack(rects, out bounds, PackingHints.FindBest, 1.0, 1, (uint)spriteSheet.Width, (uint)spriteSheet.Height);

        uint usedWidth = 0;
        uint usedHeight = 0;
        foreach (PackingRectangle rect in rects)
        {
            uint right = rect.X + rect.Width;
            uint bottom = rect.Y + rect.Height;
            if (right > usedWidth)
                usedWidth = right;
            if (bottom > usedHeight)
                usedHeight = bottom;
        }

        return usedWidth <= spriteSheet.Width && usedHeight <= spriteSheet.Height;
    }
}
