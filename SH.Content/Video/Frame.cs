using SkiaSharp;
using System;

namespace SH.Content.Video;

public sealed class Frame : IDisposable
{
    public TimeSpan Timestamp { get; set; }
    public SKBitmap Image { get; set; }

    public Frame(TimeSpan timestamp, SKBitmap image)
    {
        Timestamp = timestamp;
        Image = image;
    }

    public void Dispose()
    {
        Image?.Dispose();
        Image = null;
    }
}