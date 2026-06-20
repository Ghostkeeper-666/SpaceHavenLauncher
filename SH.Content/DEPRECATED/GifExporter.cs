using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SkiaSharp;
using SH.Content.Video;
using System.Collections.Generic;
using System;

namespace SH.Content.Art;

public sealed class GifExporter
{
    // =========================================================
    // NEW API
    // =========================================================
    public void Export(Clip clip, SKColor bgColor, string outputPath)
    {
        // ----------------------------
        // PASS 1: EXTRACT BITMAPS + DELAYS
        // ----------------------------
        List<SKBitmap> raw = new(clip.Frames.Length);
        List<int> delays = new(clip.Frames.Length);

        foreach (Frame f in clip.Frames)
        {
            raw.Add(f.Image);
            delays.Add(1); // placeholder, updated later if needed externally
        }

        // ----------------------------
        // PASS 2: GLOBAL TRIM BOUNDS
        // ----------------------------
        SKRectI bounds = ComputeGlobalBounds(raw);

        if (bounds.Width <= 0 || bounds.Height <= 0)
            bounds = new SKRectI(0, 0, raw[0].Width, raw[0].Height);

        int finalWidth = bounds.Width;
        int finalHeight = bounds.Height;

        // ----------------------------
        // PASS 3: CROPPED + BACKGROUND COMPOSITING
        // ----------------------------
        List<Image<Rgba32>> images = new(clip.Frames.Length);

        foreach (SKBitmap src in raw)
        {
            using SKBitmap cropped = CropBitmap(src, bounds);
            images.Add(ConvertToImageWithBackground(cropped, bgColor));
        }

        // ----------------------------
        // PASS 4: BUILD GIF
        // ----------------------------
        Image<Rgba32> gif = images[0];

        for (int i = 1; i < images.Count; i++)
            gif.Frames.AddFrame(images[i].Frames.RootFrame);

        gif.Metadata.GetGifMetadata().RepeatCount = 0;

        WuQuantizer quantizer = new(new QuantizerOptions
        {
            MaxColors = 256,
            Dither = KnownDitherings.FloydSteinberg,
            DitherScale = 1.0f
        });

        gif.Mutate(ctx => ctx.Quantize(quantizer));

        // ----------------------------
        // PASS 5: APPLY DELAYS (SCALED)
        // ----------------------------
        for (int i = 0; i < gif.Frames.Count; i++)
        {
            int delayMs = delays[i];
            gif.Frames[i].Metadata.GetGifMetadata().FrameDelay =
                Math.Max(1, delayMs / 10);
        }

        gif.Save(outputPath);
    }

    // =========================================================
    // GLOBAL BOUNDS (keeps old behavior)
    // =========================================================
    private static SKRectI ComputeGlobalBounds(List<SKBitmap> frames)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (SKBitmap bmp in frames)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
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

    // =========================================================
    // CROPPING
    // =========================================================
    private static SKBitmap CropBitmap(SKBitmap src, SKRectI bounds)
    {
        SKBitmap dst = new(bounds.Width, bounds.Height);

        using SKCanvas canvas = new(dst);
        canvas.Clear(SKColors.Transparent);

        canvas.DrawBitmap(src, -bounds.Left, -bounds.Top);

        return dst;
    }

    // =========================================================
    // BACKGROUND COMPOSITING (NEW FEATURE)
    // =========================================================
    private static Image<Rgba32> ConvertToImageWithBackground(SKBitmap bmp, SKColor bgColor)
    {
        Image<Rgba32> img = new(bmp.Width, bmp.Height);

        Rgba32 bg = new(bgColor.Red, bgColor.Green, bgColor.Blue, 255);

        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                SKColor c = bmp.GetPixel(x, y);

                if (c.Alpha == 0)
                {
                    img[x, y] = bg;
                    continue;
                }

                byte r = (byte)(c.Red * c.Alpha / 255 + bg.R * (255 - c.Alpha) / 255);
                byte g = (byte)(c.Green * c.Alpha / 255 + bg.G * (255 - c.Alpha) / 255);
                byte b = (byte)(c.Blue * c.Alpha / 255 + bg.B * (255 - c.Alpha) / 255);

                img[x, y] = new Rgba32(r, g, b, 255);
            }
        }

        return img;
    }
}