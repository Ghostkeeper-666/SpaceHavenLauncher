using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.RectPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SH.Modding.Build;

internal sealed class SpriteAtlasBuildData
{
    public string Name { get; }
    public List<SpriteSheetBuildData> SpriteSheets { get; } = [];
    public int SpriteSheetSize { get; set; } = 2048;
    public int SpriteSpacing { get; set; } = 4;

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

    public SpriteAtlasBuildData(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

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

    public bool Add(IEnumerable<SpriteBuildData> sprites, ILogger log, CancellationToken ct)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(sprites);

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

            // Fit each sprite to a spritesheet:
            int count = 0;
            SpriteBuildData[] sortedSprites = allSprites.Values.OrderByDescending(sprite => ((ulong)sprite.Width) * ((ulong)sprite.Height)).ToArray();
            foreach (SpriteBuildData sprite in sortedSprites)
            {
                ct.ThrowIfCancellationRequested();

                ++count;

                // Check for very large sprite:
                if (sprite.Width > SpriteSheetSize || sprite.Height > SpriteSheetSize)
                {
                    log?.Error($"Texture '{sprite.LocalName}' with size {sprite.Width}x{sprite.Height} does not fit into the maximum allowed size of {SpriteSheetSize}x{SpriteSheetSize}");
                    return false;
                }

                // Try to fit sprite to an existing spritesheet:
                bool spriteWasAdded = false;
                foreach (SpriteSheetBuildData spriteSheet in SpriteSheets)
                {
                    ct.ThrowIfCancellationRequested();

                    // Resize spritesheet if it is below the maximum allowed size:
                    if (spriteSheet.Width < SpriteSheetSize || spriteSheet.Height < SpriteSheetSize)
                        spriteSheet.Resize(Math.Max(spriteSheet.Width, SpriteSheetSize), Math.Max(spriteSheet.Width, SpriteSheetSize));

                    // Check if occupancy would exceed max allowed:
                    double spriteOccupancy = ((sprite.Height + SpriteSpacing) * (sprite.Width + SpriteSpacing)) / (double)(SpriteSheetSize * SpriteSheetSize);
                    double newOccupancy = spriteSheet.Packer.Occupancy + spriteOccupancy;
                    if (newOccupancy > spriteSheet.Packer.MaxOccupancy)
                        continue;

                    // Test if the spritesheet can hold this new sprite:
                    List<SpriteRectangle> rects =
                        spriteSheet.Packer.Rectangles
                        .Append(new SpriteRectangle(0, 0, sprite.Width + SpriteSpacing, sprite.Height + SpriteSpacing, sprite))
                        .ToList();
                    if (!spriteSheet.Packer.TryPack(rects))
                        continue;

                    // Add sprite to spritesheet:
                    spriteSheet.Add(sprite);
                    spriteWasAdded = true;
                    break;
                }
                if (spriteWasAdded)
                    continue;

                // Create a new sprite sheet, and manually add the first sprite:
                SpriteSheetBuildData newSpriteSheet =
                    new(SpriteSheets.Count, SpriteSheetSize, SpriteSheetSize, allSprites.Count, SpriteSpacing, 0.90, this);
                newSpriteSheet.Add(sprite);
                newSpriteSheet.Packer.Rectangles.Add(new SpriteRectangle(0, 0, sprite.Width + SpriteSpacing, sprite.Height + SpriteSpacing, sprite));
                SpriteSheets.Add(newSpriteSheet);
            }

            // Pack each sprite:
            foreach (SpriteSheetBuildData spriteSheet in SpriteSheets)
            {
                foreach (SpriteRectangle r in spriteSheet.Packer.Rectangles)
                {
                    ct.ThrowIfCancellationRequested();
                    SpriteBuildData sprite = (SpriteBuildData)r.Sprite;
                    int borderX = (r.Width - sprite.Width) >> 1;
                    int borderY = (r.Height - sprite.Height) >> 1;
                    sprite.SpriteSheetX = r.X + borderX;
                    sprite.SpriteSheetY = r.Y + borderY;
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

    private bool TryPack(SpriteSheetBuildData spriteSheet, SpriteBuildData additionalSprite, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        List<SpriteRectangle> rectList =
            spriteSheet.Sprites
            .Select(s => new SpriteRectangle(0, 0, s.Width + spriteSheet.SpriteSpacing, s.Height + spriteSheet.SpriteSpacing, s))
            .ToList();
        if (additionalSprite != null)
            rectList.Add(new SpriteRectangle(0, 0, additionalSprite.Width + spriteSheet.SpriteSpacing, additionalSprite.Height + spriteSheet.SpriteSpacing, additionalSprite));

        return spriteSheet.Packer.TryPack(rectList);
    }


}
