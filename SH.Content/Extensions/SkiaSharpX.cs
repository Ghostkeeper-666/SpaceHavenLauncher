using SkiaSharp;
using System;

namespace SH.Content.Extensions;

public static class SkiaSharpX
{
    public static SKBitmap AutoCrop(this SKBitmap src)
    {
        int minX = src.Width, minY = src.Height;
        int maxX = -1, maxY = -1;

        for (int y = 0; y < src.Height; ++y)
        {
            for (int x = 0; x < src.Width; ++x)
            {
                if (src.GetPixel(x, y).Alpha != 0)
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (maxX < minX || maxY < minY)
            return null; // fully transparent

        SKRectI rect = new(minX, minY, maxX + 1, maxY + 1);
        SKBitmap cropped = new(rect.Width, rect.Height);
        src.ExtractSubset(cropped, rect);
        return cropped;
    }

}
