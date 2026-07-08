using Imazen.WebP;
using SH.Framework.IO;
using SH.Framework.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;

namespace SH.Content.Video;

public static class WebpExporter
{
    public static bool TryExport(Clip clip, string outputDir, double scale = 1.0, double playbackSpeed = 1.0, ILogger logger = null)
    {
        try
        {
            string path = IOUtils.CombineAsOSPath(outputDir, $"{clip.Name}.webp");
            logger?.Debug($@"Exporting clip to ""{path}""");

            if (scale <= 0.0)
                scale = 1.0;
            else if (scale <= 0.01)
                scale = 0.01;
            else if (scale > 64)
                scale = 64;

            if (playbackSpeed < 1.0 / 60.0)
                playbackSpeed = 1.0 / 60.0;
            else if (playbackSpeed > 60.0)
                playbackSpeed = 60.0;

            int width = scale == 1.0 ?
                clip.Frames.First().Image.Width :
                ((int)(clip.Frames.First().Image.Width * scale + 1) >> 1) << 1;

            int height = scale == 1.0 ?
                clip.Frames.First().Image.Height :
                ((int)(clip.Frames.First().Image.Height * scale + 1) >> 1) << 1;

            using AnimEncoder encoder = new(width, height, loopCount: 0);
            int frameId = 0;
            int prevTimestamp = -1;
            foreach (Frame frame in clip.Frames)
            {
                logger?.Debug($"Exporting frame {++frameId} of {clip.Frames.Length}...");
                SKBitmap src = scale == 1.0 ? frame.Image : Scale(frame.Image, width, height);
                using SKBitmap converted = new(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
                src.CopyTo(converted);
                int timestamp = (int)(frame.Timestamp.TotalMilliseconds / playbackSpeed);
                if (timestamp < prevTimestamp) timestamp = prevTimestamp + 1;
                encoder.AddFrame(converted.Bytes, timestampMs: timestamp, quality: 100);
                prevTimestamp = timestamp;
            }
            byte[] animatedWebP = encoder.Assemble();

            string dir = Path.GetDirectoryName(path);
            if (!IOUtils.DirectoryExists(dir))
                try { Directory.CreateDirectory(dir); } catch { }
            File.WriteAllBytes(path, animatedWebP);

            return true;
        }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
    }

    public static SKBitmap Scale(SKBitmap src, int width, int height)
    {
        SKImageInfo info = new(width, height, src.ColorType, src.AlphaType);
        SKSamplingOptions sampling = new(SKFilterMode.Nearest, SKMipmapMode.Nearest);
        return src.Resize(info, sampling);
    }
}