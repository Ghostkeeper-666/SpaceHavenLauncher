using SH.Content.Xml;
using SH.Content.Xml.Animations;
using SH.Content.Xml.Textures;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Content.Art;

public sealed class ArtRepository
{
    private readonly ILogger Log;

    private AnimationsXmlRepository AnimationsXmlRepository { get; }

    public int LastSpriteSheetName { get; private set; }
    public int LastSpriteName { get; private set; }
    public int LastSpriteId { get; private set; }
    public int LastAnimationId { get; private set; }

    public OrderedDictionary<int, SpriteSheet> SpriteSheets { get; } = new();
    public OrderedDictionary<int, Sprite> SpritesByName { get; } = new();
    public OrderedDictionary<int, Sprite> SpritesById { get; } = new();
    private TexturesXmlRepository TextureXmlRepository { get; }
    public OrderedDictionary<string, Animation> AnimationsByName { get; } = new();
    public OrderedDictionary<int, Animation> AnimationsById { get; } = new();

    private readonly object Lock = new();

    public ArtRepository(TexturesXmlRepository textureXmlRepository, AnimationsXmlRepository animationsXmlRepository, ILogger logger)
    {
        TextureXmlRepository = textureXmlRepository;
        AnimationsXmlRepository = animationsXmlRepository;
        Log = logger ?? new VoidLogger();
    }

    public async Task<bool> TryLoadAsync(string baseInputDir, CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            bool success = true;

            LastSpriteSheetName = -1;
            LastSpriteName = -1;
            LastSpriteId = -1;
            LastAnimationId = -1;
            SpriteSheets.Clear();
            SpritesByName.Clear();
            SpritesById.Clear();
            AnimationsByName.Clear();

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct,
            };

            List<string> files = Directory.GetFiles(baseInputDir, "*.cim", SearchOption.TopDirectoryOnly)?.OrderBy(path => Path.GetFileNameWithoutExtension(path).PadLeft(3, '0'))?.ToList() ?? [];

            // Generate a progress update only 100 times:
            int pi = 0;
            double p = 0.0;
            double delta = 100.0 / files.Count;
            SemaphoreSlim semaphore = new(1, 1);
            await Parallel.ForEachAsync(files, parallelOptions, async (cimFilePath, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    if (!TryLoadSpriteSheet(cimFilePath, ct))
                        success = false;

                    await semaphore.WaitAsync(ct);
                    try
                    {
                        // Generate a progress update only 100 times:
                        if (pi < (int)(p += delta)) progress?.SetNormalized((pi = (int)p) / 100.0);
                    }
                    finally { semaphore.Release(); }
                }
                catch (Exception ex)
                {
                    success = false;
                    Log.Error($@"Load(""{cimFilePath}""): {ex}");
                }
            });

            return success;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log?.Error(ex);
            return false;
        }
    }

    private bool TryLoadSpriteSheet(string cimFilePath, CancellationToken ct)
    {
        try
        {
            string filename = Path.GetFileName(cimFilePath);
            string cimName = Path.GetFileNameWithoutExtension(cimFilePath);
            if (!int.TryParse(cimName, out int textureId) || !TextureXmlRepository.ById.TryGetValue(textureId, out TextureXml texture))
            {
                Log.Error($"[{filename}] Unable to locate the corresponding texture XML information for this CIM file");
                return false;
            }

            Log.Debug($"[{filename}] Reading sprite data from CIM file");
            if (!SpriteSheet.TryLoad(cimFilePath, texture, out SpriteSheet spriteSheet, Log))
            {
                Log.Error($"[{filename}] Unable to read sprite data from CIM file");
                return false;
            }

            lock (Lock)
            {
                if (!SpriteSheets.TryAdd(spriteSheet.Name, spriteSheet))
                {
                    Log.Error($@"ERROR: A duplicate texture NAME ""{spriteSheet.Name}"" was found");
                    return false;
                }
                if (spriteSheet.Name > LastSpriteSheetName)
                    LastSpriteSheetName = spriteSheet.Name;

                foreach (KeyValuePair<int, Sprite> kvp in spriteSheet.SpritesByName)
                {
                    ct.ThrowIfCancellationRequested();

                    if (SpritesByName.TryAdd(kvp.Key, kvp.Value))
                    {
                        if (kvp.Value.Name > LastSpriteName)
                            LastSpriteName = kvp.Value.Name;
                    }
                    else
                    {
                        Sprite sprite = kvp.Value;
                        int name = kvp.Key;
                        bool isSameImage = SpritesByName[name].Equals(sprite);
                        string comparisonText = isSameImage ? "identical" : "DIFFERENT";
                        string message = $@"Ignoring sprite image in sprite sheet ""{spriteSheet.Name}"" with a DUPLICATE REGION NAME=""{name}"": it was reused for {comparisonText} sprite image content";

                        if (isSameImage) Log.Debug(message);
                        else Log.Warn(message);
                        
                        if (SpritesByName[kvp.Key].SpriteSheet.Name > kvp.Value.SpriteSheet.Name)
                            SpritesByName[kvp.Key] = kvp.Value;
                    }
                }

                foreach (KeyValuePair<int, Sprite> kvp in spriteSheet.SpritesById)
                {
                    ct.ThrowIfCancellationRequested();

                    if (SpritesById.TryAdd(kvp.Key, kvp.Value))
                    {
                        if (kvp.Value.Id > LastSpriteId)
                            LastSpriteId = kvp.Value.Id;
                    }
                    else
                    {
                        Sprite sprite = kvp.Value;
                        int id = kvp.Key;
                        bool isSameImage = SpritesById[id].Equals(sprite);
                        string comparisonText = isSameImage ? "identical" : "DIFFERENT";
                        string message = $@"Ignoring sprite image in sprite sheet ""{spriteSheet.Name}"" with a DUPLICATE REGION ID=""{id}"": it was reused for {comparisonText} sprite image content";

                        if (isSameImage || id == 0) Log.Debug(message);
                        else Log.Warn(message);

                        if (SpritesById[kvp.Key].SpriteSheet.Name > kvp.Value.SpriteSheet.Name)
                            SpritesById[kvp.Key] = kvp.Value;
                    }
                }
            }

            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"[{cimFilePath}] {ex}");
            return false;
        }
    }

    public async Task<bool> TryExportSpriteSheetsToPngAsync(string exportDir, ParallelOptions parallelOptions, IProgressInfo progress)
    {
        try
        {
            bool success = true;
            parallelOptions ??= new() { MaxDegreeOfParallelism = Environment.ProcessorCount, };

            if (!await IOUtils.TryCreateDirectoryAsync(exportDir, Log, parallelOptions.CancellationToken))
                return false;

            List<SpriteSheet> spriteSheets = SpriteSheets.Values.OrderByDescending(ss => ss.Area).ToList();

            // Generate a progress update only 100 times:
            int pi = 0;
            double p = 0.0;
            double delta = 100.0 / spriteSheets.Count;
            SemaphoreSlim semaphore = new(1, 1);
            await Parallel.ForEachAsync(spriteSheets, parallelOptions, async (spriteSheet, ct) =>
            {
                ct.ThrowIfCancellationRequested();

                if (!await spriteSheet.TryExportToPngAsync(exportDir, Log, ct))
                    success = false;

                await semaphore.WaitAsync(ct);
                try
                {
                    // Generate a progress update only 100 times:
                    if (pi < (int)(p += delta)) progress?.SetNormalized((pi = (int)p) / 100.0);
                }
                finally { semaphore.Release(); }
            });

            // Done.
            progress.Complete();
            return success;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }

    public async Task<bool> TryExportSpritesToPngAsync(string exportDir, ParallelOptions parallelOptions, IProgressInfo progress)
    {
        try
        {
            bool success = true;
            parallelOptions ??= new() { MaxDegreeOfParallelism = Environment.ProcessorCount, };

            if (!await IOUtils.TryCreateDirectoryAsync(exportDir, Log, parallelOptions.CancellationToken))
                return false;

            // List all sprites to be exported:
            List<Sprite> sprites = SpriteSheets.Values.OrderByDescending(ss => ss.SpritesByName.Count).ThenBy(s => s.Name).SelectMany(ss => ss.SpritesByName.Values.OrderBy(s => s.Y * s.SpriteSheet.Width + s.X)).ToList();

            // Create directories using SpriteSheet names first:
            string[] dirs = SpriteSheets.Keys.Select(name => Path.Combine(exportDir, name.ToString())).ToArray() ?? [];
            foreach (string dir in dirs)
                if(!await IOUtils.TryCreateDirectoryAsync(dir, Log, parallelOptions.CancellationToken))
                    return false;

            // Generate a progress update only 100 times:
            int pi = 0;
            double p = 0.0;
            double delta = 100.0 / sprites.Count;

            // Write individual sprites:
            SemaphoreSlim semaphore = new(1, 1);
            await Parallel.ForEachAsync(sprites, parallelOptions, async (sprite, ct) =>
            {
                ct.ThrowIfCancellationRequested();

                if (!await sprite.TryExportToPngAsync(Path.Combine(exportDir, sprite.SpriteSheet.Name.ToString(), $"{sprite.Name}.png"), Log, ct))
                    success = false;

                await semaphore.WaitAsync(ct);
                try
                {
                    // Generate a progress update only 100 times:
                    if (pi < (int)(p += delta)) progress?.SetNormalized((pi = (int)p) / 100.0);
                }
                finally { semaphore.Release(); }
            });

            // Done.
            progress.Complete();
            return success;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log?.Error(ex);
            return false;
        }
    }

    public bool TryLoadAnimations(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            OrderedDictionary<string, AnimationXml> remaining = new(AnimationsXmlRepository.ByName);
            List<AnimationXml> current;
            List<AnimationXml> errors = [];

            // Generate a progress update only 100 times:
            int pi = 0;
            double p = 0.0;
            double delta = 100.0 / (remaining.Count);

            do
            {
                ct.ThrowIfCancellationRequested();

                current = remaining.Values
                    .Where(a =>
                        a.Items.All(i => i.AnimationName.IsNullOrEmpty() ||
                        AnimationsByName.ContainsKey(i.AnimationName)))
                    .ToList();

                foreach (AnimationXml a in current)
                    remaining.Remove(a.Name);

                foreach (AnimationXml axml in current)
                {
                    Animation a = new(axml);
                    AnimationsByName.Add(a.Name, a);
                    AnimationsById.Add(a.Id, a);
                }

                // Generate a progress update only 100 times:
                if (pi < (int)(p += current.Count * delta)) progress?.SetNormalized((pi = (int)p) / 100.0);
            }
            while (current.Count > 0);

            Log.Debug($@"{AnimationsByName.Count} animations were successfully loaded");

            if (remaining.Count > 0)
            {
                Log.Debug($@"The following {errors.Count} animations have loading errors:");
                Log.Debug($@"{errors.Select(a => a.Name).JoinToString(", ")}");

                Log.Debug($@"The following {remaining.Count} animations were not loaded because they depend on animations with loading errors:");
                Log.Debug($@"{errors.Select(a => a.Name).JoinToString(", ")}");

                return false;
            }

            // Done.
            progress.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log?.Error(ex);
            return false;
        }
    }
}
