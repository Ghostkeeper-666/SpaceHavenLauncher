//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;
//using SixLabors.ImageSharp.Processing;
//using SH.Content.Xml.Animations;
//using System.Collections.Generic;
//using System.Linq;
//using System;

//namespace SH.Content.Art;

//public sealed class AnimationOld
//{
//    public string Name => Xml.Name;
//    public int Id => Xml.Id;
//    private AnimationXml Xml { get; }
//    private IDictionary<int, Sprite> Sprites { get; }
//    private IDictionary<string, AnimationOld> Animations { get; }


//    public AnimationOld(AnimationXml animationXml, IDictionary<int, Sprite> spritesByName, IDictionary<string, AnimationOld> animationsByName)
//    {
//        Xml = animationXml;
//        Sprites = spritesByName;
//        Animations = animationsByName;
//    }

//    public bool TryLoad()
//    {
//        if (Xml.KeyFrames.Count > 2)
//        {
//            Log.Info($"Not implemented: animation with multiple frames");
//            return true;
//        }
//        if (Xml.Bone.Children.Count > 1)
//        {
//            Log.Info($"Not implemented: animation with child bones");
//            return true;
//        }
//        if (Xml.Bone?.Positions.Count != 1)
//        {
//            Log.Info($"Not implemented: animation with multiple bone positions");
//            return true;
//        }
//        if (Xml.Bone?.Positions.First()?.ScaleX != 1 || Xml.Bone?.Positions.First()?.ScaleY != 1)
//        {
//            Log.Info($"Not implemented: animation bone with scaling");
//            return true;
//        }
//        if (Xml.Bone?.Positions.First()?.Rotation != 0)
//        {
//            Log.Info($"Not implemented: animation bone with rotation");
//            return true;
//        }
//        //if (Xml.Bone?.Positions.First()?.ColorMask != -1)
//        //{
//        //    Log.Info($"Not implemented: animation bone with color-mask");
//        //    return false;
//        //}
//        if (Xml.Items.Any(i => i.AnimationName != null))
//        {
//            Log.Info($"Not implemented: animation asset with reference to another animation");
//            return true;
//        }
//        //if (Xml.Items.Any(i => i.ScaleX != 1 || i.ScaleY != 1))
//        //{
//        //    Log.Info($"Not implemented: animation asset with scaling");
//        //    return false;
//        //}
//        //if (Xml.Items.Any(i => i.Rotation != 1.0f))
//        //{
//        //    Log.Info($"Not implemented: animation asset with rotation");
//        //    return false;
//        //}

//        using (Image<Rgba32> canvas = new(1024, 1024))
//        {
//            int ox = canvas.Width / 2;
//            int oy = canvas.Height / 2;

//            foreach (AnimationXml_Item asset in Xml.Items)
//            {
//                if (asset?.SpriteName == null)
//                {
//                    Log.Info($"Not implemented: animation asset does not have an image defined");
//                    return false;
//                }
//                Sprite sprite = Sprites[asset.SpriteName];
//                //sprite.TryExportToPng($@"C:\Temp2\{sprite.Name}.png");

//                Image<Rgba32> image = Image.LoadPixelData<Rgba32>(sprite.PixelData, sprite.Width, sprite.Height);
//                Image<Rgba32> croppedImage = Crop(image);
//                //croppedImage.SaveAsPng($@"C:\Temp2\{sprite.Name}-cropped.png");

//                Compose(
//                    canvas,
//                    image,
//                    ox + asset.X - (int)Math.Ceiling(croppedImage.Width / 2.0f),
//                    oy - asset.Y - (int)Math.Ceiling(croppedImage.Height / 2.0f),
//                    asset.ScaleX,
//                    asset.ScaleY,
//                    asset.Rotation);

//                //canvas.SaveAsPng($@"C:\Temp2\canvas.png");
//            }

//            using (Image<Rgba32> cropped = Crop(canvas))
//                cropped.SaveAsGif($@"C:\Temp2\Animations\{Name}.gif");
//        }
//        return true;
//    }

//    public static void Compose(Image<Rgba32> canvas, Image<Rgba32> sprite, float x, float y, float sx, float sy, float r)
//    {
//        canvas.Mutate(ctx =>
//        {
//            Draw(ctx, sprite, x, y, sx, sy, r);
//        });
//    }

//    public static void Draw(IImageProcessingContext ctx, Image<Rgba32> image, float x, float y, float sx, float sy, float r)
//    {
//        if(sx != 1 || sy != 1 || r != 0)
//            image = Transform(image, sx, sy, r);
//        ctx.DrawImage(image, new Point((int)Math.Round(x), (int)Math.Round(y)), 1f);
//    }

//    public static Image<Rgba32> Transform(Image<Rgba32> source, float sx, float sy, float r) =>
//        source.Clone(ctx =>
//        {
//            int w = (int)(source.Width * sx);
//            int h = (int)(source.Height * sy);
//            ctx.Resize(w, h, KnownResamplers.Bicubic);

//            float radians = MathF.PI * r / 180f;
//            float absCos = MathF.Abs(MathF.Cos(radians));
//            float absSin = MathF.Abs(MathF.Sin(radians));
//            int newW = (int)(w * absCos + h * absSin);
//            int newH = (int)(w * absSin + h * absCos);
//            ctx.Pad(newW, newH, Color.Transparent);
//            ctx.Rotate(r);
//        });


//    public static Image<Rgba32> Crop(Image<Rgba32> image)
//    {
//        int minX = image.Width;
//        int minY = image.Height;
//        int maxX = -1;
//        int maxY = -1;

//        for (int y = 0; y < image.Height; y++)
//        {
//            for (int x = 0; x < image.Width; x++)
//            {
//                if (image[x, y].A != 0)
//                {
//                    if (x < minX) minX = x;
//                    if (y < minY) minY = y;
//                    if (x > maxX) maxX = x;
//                    if (y > maxY) maxY = y;
//                }
//            }
//        }

//        if (maxX == -1)
//            return new Image<Rgba32>(1, 1);

//        Rectangle crop = new(minX, minY, maxX - minX + 1, maxY - minY + 1);

//        return image.Clone(ctx => ctx.Crop(crop));
//    }

//}
