//using FFMpegCore;
//using FFMpegCore.Pipes;
//using SkiaSharp;
//using SH.Content.Logging;
//using SH.Content.Video;

//namespace SH.Content.Art;

//public sealed class Mp4Exporter
//{
//    public bool Export(Clip clip, SKColor bgColor, string outputPath)
//    {
//        int width = clip.Frames.First().Image.Width;
//        int height = clip.Frames.First().Image.Height;
//        if (!clip.Frames.All(f => f.Image.Width == width && f.Image.Height == height))
//        {
//            return false;
//        }

//        // ----------------------------
//        // PASS 1: EXTRACT BITMAPS + DELAYS
//        // ----------------------------
//        List<SKBitmap> raw = new(clip.Frames.Length);
//        List<int> delays = new(clip.Frames.Length);

//        foreach (Frame f in clip.Frames)
//        {
//            raw.Add(f.Image);
//            delays.Add(1); // placeholder, updated later if needed externally
//        }

//        // ----------------------------
//        // PASS 2: GLOBAL TRIM BOUNDS
//        // ----------------------------
//        SKRectI bounds = ComputeGlobalBounds(raw);

//        if (bounds.Width <= 0 || bounds.Height <= 0)
//            bounds = new SKRectI(0, 0, raw[0].Width, raw[0].Height);


//        // ----------------------------
//        // PASS 3: CROPPED + BACKGROUND COMPOSITING
//        // ----------------------------
//        List<SKBitmap> images = new(clip.Frames.Length);
//        foreach (SKBitmap src in raw)
//        {
//            using SKBitmap cropped = CropBitmap(src, bounds);
//            images.Add(ConvertToImageWithBackground(cropped, bgColor));
//        }





//        RawVideoPipeSource source = new(images.Select(image => new SKBitmapFrame(image)))
//        {
//            FrameRate = clip.FrameRate,
//        };


//        string ffmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg");

//        GlobalFFOptions.Configure(options =>
//        {
//            options.BinaryFolder = ffmpegPath;
//            options.LogLevel = FFMpegCore.Enums.FFMpegLogLevel.Trace;
//        });

//        _ = FFMpegArguments
//            .FromPipeInput(source)

//            .AddFileInput("anullsrc=channel_layout=stereo:sample_rate=44100", true)

//            .OutputToFile(outputPath, overwrite: true, options => options

//                .WithVideoCodec("libx264")
//                .WithConstantRateFactor(18)
//                .WithCustomArgument("-pix_fmt yuv420p")
//                .WithCustomArgument("-profile:v baseline")
//                .WithCustomArgument("-level 4.0")
//                .WithCustomArgument("-r 30")
//                .WithCustomArgument("-vsync cfr")
//                .WithCustomArgument("-movflags +faststart")
//                .WithCustomArgument("-shortest")

//                .WithCustomArgument("-loglevel trace")
//                .WithFastStart())
//            .NotifyOnOutput(line => Log.Info($"OUT: {line}"))
//            .NotifyOnError(line => Log.Info($"ERR: {line}"))
//            .ProcessSynchronously();

//        return true;
//    }


//    // =========================================================
//    // GLOBAL BOUNDS (keeps old behavior)
//    // =========================================================
//    private static SKRectI ComputeGlobalBounds(List<SKBitmap> frames)
//    {
//        int minX = int.MaxValue;
//        int minY = int.MaxValue;
//        int maxX = int.MinValue;
//        int maxY = int.MinValue;

//        int globalWidth = frames[0].Width;
//        int globalHeight = frames[0].Height;

//        foreach (SKBitmap bmp in frames)
//        {
//            for (int y = 0; y < bmp.Height; y++)
//            {
//                for (int x = 0; x < bmp.Width; x++)
//                {
//                    SKColor c = bmp.GetPixel(x, y);

//                    if (c.Alpha == 0)
//                        continue;

//                    if (x < minX) minX = x;
//                    if (y < minY) minY = y;
//                    if (x > maxX) maxX = x;
//                    if (y > maxY) maxY = y;
//                }
//            }
//        }

//        if (minX > maxX || minY > maxY)
//            return SKRectI.Empty;

//        int width = (maxX + 1) - minX;
//        int height = (maxY + 1) - minY;

//        // Clamp to bitmap bounds FIRST
//        width = Math.Min(width, globalWidth - minX);
//        height = Math.Min(height, globalHeight - minY);

//        // Make even WITHOUT exceeding bounds
//        if (width % 2 != 0)
//        {
//            if (minX + width < globalWidth)
//                width++;
//            else if (width > 1)
//                width--;
//        }

//        if (height % 2 != 0)
//        {
//            if (minY + height < globalHeight)
//                height++;
//            else if (height > 1)
//                height--;
//        }

//        return new SKRectI(
//            minX,
//            minY,
//            minX + width,
//            minY + height
//        );
//    }

//    // =========================================================
//    // CROPPING
//    // =========================================================
//    private static SKBitmap CropBitmap(SKBitmap src, SKRectI bounds)
//    {
//        SKBitmap dst = new(bounds.Width, bounds.Height);

//        using SKCanvas canvas = new(dst);
//        canvas.Clear(SKColors.Transparent);

//        canvas.DrawBitmap(src, -bounds.Left, -bounds.Top);

//        return dst;
//    }

//    // =========================================================
//    // BACKGROUND COMPOSITING (NEW FEATURE)
//    // =========================================================
//    private static SKBitmap ConvertToImageWithBackground(SKBitmap bmp, SKColor bgColor)
//    {
//        SKBitmap img = new(bmp.Width, bmp.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

//        for (int y = 0; y < bmp.Height; y++)
//        {
//            for (int x = 0; x < bmp.Width; x++)
//            {
//                SKColor c = bmp.GetPixel(x, y);

//                if (c.Alpha == 0)
//                {
//                    img.SetPixel(x, y, bgColor);
//                    continue;
//                }

//                byte r = (byte)(c.Red * c.Alpha / 255 + bgColor.Red * (255 - c.Alpha) / 255);
//                byte g = (byte)(c.Green * c.Alpha / 255 + bgColor.Green * (255 - c.Alpha) / 255);
//                byte b = (byte)(c.Blue * c.Alpha / 255 + bgColor.Blue * (255 - c.Alpha) / 255);

//                img.SetPixel(x, y, new SKColor(r, g, b, 255));
//            }
//        }

//        return img;
//    }
//}
