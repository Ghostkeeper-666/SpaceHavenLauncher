using SH.Framework.Extensions;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SH.Modding.Build;

internal sealed class SpriteAtlas : IDisposable
{
    public string Name { get; }
    public IReadOnlyList<SpriteSheet> SpriteSheets => SpriteSheetList;
    private List<SpriteSheet> SpriteSheetList = [];
    public int SpriteSheetSize { get; set; } = 2048;
    public int SpriteSpacing { get; set; } = 4;

    public List<Sprite> Sprites
    {
        get
        {
            List<Sprite> list = [];
            foreach (SpriteSheet ss in SpriteSheetList)
                foreach (Sprite s in ss.Sprites.OrderBy(s => s.AbsoluteFilePath ?? string.Empty))
                    list.Add(s);
            return list;
        }
    }

    public int SpriteCount =>
        SpriteSheetList.Sum(sh => sh.Count);

    public SpriteAtlas(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void Clear() =>
        SpriteSheetList.Clear();

    public Sprite GetSpriteWithGlobalId(int globalId) =>
        Sprites?.FirstOrDefault(s => s.GlobalId == globalId);
    public Sprite GetSpriteWithLocalId(int localId) =>
        Sprites?.FirstOrDefault(s => s.LocalId == localId);
    public Sprite GetSpriteWithGlobalName(string globalName) =>
        Sprites?.FirstOrDefault(s => s.LocalName.Equals(globalName, StringComparison.Ordinal));
    public Sprite GetSpriteWithLocalName(string localName) =>
        Sprites?.FirstOrDefault(s => s.LocalName.Equals(localName, StringComparison.Ordinal));

    public void Add(SpriteSheet spritesheet) =>
        SpriteSheetList.Add(spritesheet);

    public bool Add(IEnumerable<Sprite> sprites, ILogger log, CancellationToken ct)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(sprites);

            // Nothing to do?
            if (!sprites.Any())
                return true;

            ct.ThrowIfCancellationRequested();

            // Collect all sprites of this atlas, then add the new ones:
            OrderedDictionary<int, Sprite> allSprites = [];
            allSprites.AddRange(Sprites, s => s.LocalId, s => s);
            allSprites.AddRange(sprites, s => s.LocalId, s => s);
            Sprite[] sortedSprites = allSprites.Values.OrderByDescending(sprite => ((ulong)sprite.Width) * ((ulong)sprite.Height)).ToArray();

            // Clear all existing spritesheets, since everything will be re-calculated:
            foreach (SpriteSheet spriteSheet in SpriteSheetList)
            {
                spriteSheet.Clear();
                spriteSheet.Resize(SpriteSheetSize, SpriteSheetSize);
            }

            // Estimate the amount of spritesheets required and create them:
            long totalArea = allSprites.Values.Sum(s => (long)s.Area);
            long spriteSheetArea = SpriteSheetSize * SpriteSheetSize;
            double efficiency = 0.80;
            int estimatedSpriteSheetCount = 1 + (int)(totalArea / (efficiency * spriteSheetArea));
            for (int i = SpriteSheetList.Count; i < estimatedSpriteSheetCount; ++i)
                SpriteSheetList.Add(new(SpriteSheetList.Count, SpriteSheetSize, SpriteSheetSize, allSprites.Count, SpriteSpacing, this));

            ct.ThrowIfCancellationRequested();

            // Fit each sprite to a spritesheet:
            int count = 0;
            foreach (Sprite sprite in sortedSprites)
            {
                ct.ThrowIfCancellationRequested();

                if (count++ % 100 == 0 && count > 0)
                    log?.Info($"{Name} Sprite Atlas: {count} of {allSprites.Count} sprite(s) packed");

                // Check for very large sprite:
                if (sprite.Width > SpriteSheetSize || sprite.Height > SpriteSheetSize)
                {
                    log?.Error($"Texture '{sprite.LocalName}' with size {sprite.Width}x{sprite.Height} does not fit into the maximum allowed size of {SpriteSheetSize}x{SpriteSheetSize}");
                    return false;
                }

                // Fill spritesheets with less sprites first:
                bool spriteWasAdded = false;
                foreach (SpriteSheet spriteSheet in SpriteSheetList.OrderBy(ss => ss.Sprites.Count))
                {
                    ct.ThrowIfCancellationRequested();

                    // Check if occupancy is enough:
                    double spriteOccupancy = ((sprite.Height + SpriteSpacing) * (sprite.Width + SpriteSpacing)) / (double)(SpriteSheetSize * SpriteSheetSize);
                    double newOccupancy = spriteSheet.Packer.Occupancy + spriteOccupancy;
                    if (newOccupancy > efficiency)
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
                SpriteSheet newSpriteSheet =
                    new(SpriteSheetList.Count, SpriteSheetSize, SpriteSheetSize, allSprites.Count, SpriteSpacing, this);
                newSpriteSheet.Add(sprite);
                newSpriteSheet.Packer.Rectangles.Add(new SpriteRectangle(0, 0, sprite.Width + SpriteSpacing, sprite.Height + SpriteSpacing, sprite));
                SpriteSheetList.Add(newSpriteSheet);
            }
            log?.Info($"{Name} Sprite Atlas: {count} of {allSprites.Count} sprite(s) packed");

            // Pack each sprite:
            foreach (SpriteSheet spriteSheet in SpriteSheetList)
            {
                foreach (SpriteRectangle r in spriteSheet.Packer.Rectangles)
                {
                    ct.ThrowIfCancellationRequested();
                    Sprite sprite = (Sprite)r.Sprite;
                    int borderX = (r.Width - sprite.Width) >> 1;
                    int borderY = (r.Height - sprite.Height) >> 1;
                    sprite.X = r.X + borderX;
                    sprite.Y = r.Y + borderY;
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

    #region IDisposable
    public volatile bool IsDisposed;
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        foreach (SpriteSheet ss in SpriteSheetList ?? [])
            ss?.Dispose();
        SpriteSheetList?.Clear();
        SpriteSheetList = null;
    }
    #endregion
}
