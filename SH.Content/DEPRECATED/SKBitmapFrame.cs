//using FFMpegCore.Pipes;
//using SkiaSharp;

//namespace SH.Content.Art;

//public class SKBitmapFrame : IVideoFrame
//{
//    private readonly SKBitmap Image;

//    public int Width => Image.Width;
//    public int Height => Image.Height;
//    public string Format => "rgba";

//    public SKBitmapFrame(SKBitmap image) =>
//        Image = image;

//    public void Serialize(Stream stream)
//    {
//        if (Image.ColorType != SKColorType.Rgba8888)
//        {
//            using SKBitmap converted = Image.Copy(SKColorType.Rgba8888);
//            stream.Write(converted.GetPixelSpan());
//        }
//        else
//        {
//            stream.Write(Image.GetPixelSpan());
//        }
//    }

//    public Task SerializeAsync(Stream stream, CancellationToken ct)
//    {
//        Serialize(stream);
//        return Task.CompletedTask;
//    }
//}