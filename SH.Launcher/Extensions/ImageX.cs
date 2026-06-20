using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SH.Framework.Extensions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SH.Launcher.Extensions;

internal static class ImageX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(this Image imageControl, string resourceUri)
    {
        if (imageControl == null || resourceUri.IsNullOrWhiteSpace())
            return;
        using Stream stream = AssetLoader.Open(new Uri(resourceUri));
        Bitmap oldBitmap = imageControl.Source as Bitmap;
        imageControl.Source = new Bitmap(stream);
        oldBitmap?.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitmap FromAssetLoader(string uri)
    {
        using Stream stream = AssetLoader.Open(new Uri(uri));
        return new Bitmap(stream);
    }
}
