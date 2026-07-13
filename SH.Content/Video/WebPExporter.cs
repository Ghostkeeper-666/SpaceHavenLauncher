using Imazen.WebP;
using SH.Framework.IO;
using SH.Framework.Logging;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Content.Video;

public static class WebpExporter
{
    /// <summary>
    /// Exports an animation
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="outputDir"></param>
    /// <param name="scale">default = 1.0</param>
    /// <param name="playbackSpeed">default = 1.0</param>
    /// <param name="logger"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<bool> TryExportAsync(Clip clip, string outputDir, double scale, double playbackSpeed, ILogger logger, CancellationToken ct)
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
                ct.ThrowIfCancellationRequested();
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

            string dir = path.GetParentDirAsOSPath();
            if (!IOUtils.DirExists(dir))
                try { await IOUtils.TryCreateDirectoryAsync(dir, logger, ct); }
                catch (Exception ex) { Debug.WriteLine(ex); }

            await IOUtils.TryWriteAllBytesAsync(path, animatedWebP, logger, ct);

            return true;
        }
        catch(OperationCanceledException) { throw; }
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