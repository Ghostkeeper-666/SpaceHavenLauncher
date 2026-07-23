using SH.Content.Art;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Content.DEPRECATED;

public sealed class GifExporterOld
{
    private IReadOnlyDictionary<string, Animation> Animations { get; }

    public GifExporterOld(IReadOnlyDictionary<string, Animation> animationsByName)
    {
        Animations = animationsByName;
    }

    public async Task ExportAsync(FrameBasedAnimationRenderer player, Animation animation, int width, int height, double animationPlaybackSpeed, string outputPath, CancellationToken ct)
    {
        if (animationPlaybackSpeed <= 0)
            animationPlaybackSpeed = 1;

        List<int> timeline = BuildTimeline(animation);

        List<SKBitmap> rawFrames = new();
        List<int> delays = new();

        // ----------------------------
        // PASS 1: RENDER + TIMING
        // ----------------------------

        for (int i = 0; i < timeline.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            int frame = timeline[i];

            SKBitmap bmp = await player.RenderFrameAsync(animation, frame, width, height, ct);
            rawFrames.Add(bmp);

            int nextFrame = (i == timeline.Count - 1) ? timeline[0] : timeline[i + 1];
            int durationFrames = GetStepDuration(frame, nextFrame, animation, i == timeline.Count - 1);
            int ms = FramesToMs(durationFrames, animation.FrameRate);
            delays.Add(MsToGifDelay(ms));
        }

        // ----------------------------
        // PASS 2: COMPUTE GLOBAL BOUNDS
        // ----------------------------

        SKRectI bounds = ComputeGlobalBounds(rawFrames, ct);

        // Safety fallback
        if (bounds.Width <= 0 || bounds.Height <= 0)
            bounds = new SKRectI(0, 0, width, height);

        // ----------------------------
        // PASS 3: CROP + FLATTEN
        // ----------------------------

        List<Image<Rgba32>> frames = new();

        foreach (SKBitmap src in rawFrames)
        {
            ct.ThrowIfCancellationRequested();

            using SKBitmap cropped = CropBitmap(src, bounds);
            frames.Add(ConvertSKBitmapToImage_BlackBackground(cropped));
            src.Dispose();
        }

        // ----------------------------
        // BUILD GIF
        // ----------------------------

        Image<Rgba32> gif = frames[0];

        for (int i = 1; i < frames.Count; i++)
            gif.Frames.AddFrame(frames[i].Frames.RootFrame);

        gif.Metadata.GetGifMetadata().RepeatCount = 0;

        for (int i = 0; i < gif.Frames.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            gif.Frames[i].Metadata.GetGifMetadata().FrameDelay = (int)Math.Ceiling(delays[i] / animationPlaybackSpeed);
        }

        WuQuantizer quantizer = new(new QuantizerOptions
        {
            MaxColors = 256,
            Dither = KnownDitherings.FloydSteinberg,
            DitherScale = 1.0f
        });

        ct.ThrowIfCancellationRequested();
        gif.Mutate(ctx => ctx.Quantize(quantizer));

        ct.ThrowIfCancellationRequested();
        gif.Save(outputPath);
    }

    private static SKRectI ComputeGlobalBounds(List<SKBitmap> frames, CancellationToken ct)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (SKBitmap bmp in frames)
        {
            ct.ThrowIfCancellationRequested();

            int w = bmp.Width;
            int h = bmp.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    SKColor c = bmp.GetPixel(x, y);

                    if (c.Alpha == 0)
                        continue;

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (minX > maxX || minY > maxY)
            return SKRectI.Empty;

        return new SKRectI(minX, minY, maxX + 1, maxY + 1);
    }

    private static SKBitmap CropBitmap(SKBitmap src, SKRectI bounds)
    {
        SKBitmap dst = new(bounds.Width, bounds.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (SKCanvas canvas = new(dst))
        {
            // shift image so bounding box aligns to (0,0)
            canvas.DrawBitmap(src, -bounds.Left, -bounds.Top);
        }
        return dst;
    }

    private static Image<Rgba32> ConvertSKBitmapToImage_BlackBackground(SKBitmap bmp)
    {
        Image<Rgba32> img = new(bmp.Width, bmp.Height);

        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                SKColor c = bmp.GetPixel(x, y);

                byte r = (byte)(c.Red * c.Alpha / 255);
                byte g = (byte)(c.Green * c.Alpha / 255);
                byte b = (byte)(c.Blue * c.Alpha / 255);

                img[x, y] = new Rgba32(r, g, b, 255);
            }
        }

        return img;
    }














    private List<int> BuildTimeline(Animation anim)
    {
        HashSet<int> set = new();
        Collect(anim, set);
        return set.OrderBy(x => x).ToList();
    }

    private void Collect(Animation anim, HashSet<int> set)
    {
        foreach (int k in anim.KeyFrames)
            set.Add(k);

        foreach (Asset item in anim.Assets)
        {
            if (item.AnimationName == null)
                continue;

            if (!Animations.TryGetValue(item.AnimationName, out Animation sub))
                continue;

            if (item.Loop)
            {
                // LOOPING sub anim → include full timeline
                Collect(sub, set);
            }
            else
            {
                // NON-looping → only its bounds matter
                set.Add(item.StartFrame);
                set.Add(item.EndFrame);
            }
        }
    }



    private static int FramesToMs(int frames, int fps) =>
        (int)Math.Round(frames * 1000.0 / fps);

    private static int MsToGifDelay(int ms) =>
        Math.Max(1, (int)Math.Round(ms / 10.0));

    private static int GetStepDuration(int current, int next, Animation animation, bool isLast)
    {
        int len = animation.KeyFrames.Count > 0 ? animation.KeyFrames[^1] : 1;

        // normal forward step
        if (next > current)
            return next - current;

        // loop wrap (last segment)
        if (isLast)
            return (len - current) + animation.KeyFrames[0];

        // fallback safety (should not normally happen)
        return 1;
    }
}