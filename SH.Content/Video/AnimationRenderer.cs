using SH.Content.Art;
using SH.Framework.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Content.Video;

public sealed class AnimationRenderer
{
    private IReadOnlyDictionary<string, Animation> Animations { get; }
    private IReadOnlyDictionary<int, Sprite> Sprites { get; }

    private readonly ILogger Log;

    public AnimationRenderer(IReadOnlyDictionary<string, Animation> animationsByName, IReadOnlyDictionary<int, Sprite> sprites, ILogger logger)
    {
        Animations = animationsByName;
        Sprites = sprites;
        Log = logger ?? new VoidLogger();
    }

    public async Task<Clip> RenderClipAsync(string animationName, int canvasWidth, int canvasHeight, TimeSpan maxClipDuration, bool renderIntermediateSteps, CancellationToken ct)
    {
        // Prepare the clip:
        Animation animation = Animations[animationName];
        int frameRate = ComputeClipFramerate(animation);
        TimeSpan duration = ComputeClipDuration(animation, frameRate, maxClipDuration);
        if (duration > TimeSpan.FromSeconds(30)) duration = TimeSpan.FromSeconds(30);
        int frameCount = (int)Math.Ceiling(duration.TotalSeconds * frameRate);
        Clip clip = new(animationName, frameRate, frameCount);
        Log.Debug($@"Clip {{ Name={clip.Name}, Framerate={clip.FrameRate}, Frames={clip.Frames.Length} }}");

        // For each frame, render the animation at the given timestamp:
        for (int frameId = 0; frameId < frameCount; ++frameId)
        {
            Log.Debug($"Rendering frame {frameId + 1} of {frameCount}...");
            TimeSpan timestamp = TimeSpan.FromSeconds(frameId / (double)frameRate);
            SKBitmap image = new(canvasWidth, canvasHeight);
            using SKCanvas canvas = new(image);
            canvas.Clear(SKColors.Transparent);
            Matrix3x2 canvasTransform = Compose(canvasWidth / 2f, canvasHeight / 2f, 1, 1, 0); // render to the center of the canvas

            await RenderAnimationAsync($"(A){animationName}[{frameId.ToString("000")}]", canvas, animation, timestamp, frameRate, canvasTransform, renderIntermediateSteps, ct);
            clip.Frames[frameId] = new Frame(timestamp, image);
        }

        // Crop video images to their content:
        Log.Debug($"Cropping clip images...");
        SKRectI bounds = ComputeGlobalBounds(clip);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            bounds = new SKRectI(0, 0, clip.Frames[0].Image.Width, clip.Frames[0].Image.Height);
        SKBitmap[] frameImages = clip.Frames.Select(f => f.Image).ToArray();
        for (int frameId = 0; frameId < frameImages.Length; ++frameId)
        {
            SKBitmap cropped = Crop(frameImages[frameId], bounds);
            frameImages[frameId].Dispose();
            clip.Frames[frameId].Image = cropped;
        }

        // Done.
        Log.Debug($"The clip was successfully rendered");
        return clip;
    }

    private int ComputeClipFramerate(Animation root)
    {
        HashSet<int> fpsSet = new();
        CollectAllFramerates(root, fpsSet);

        int lcm = 1;
        foreach (int fps in fpsSet)
            lcm = Lcm(lcm, fps);

        return Math.Min(60, lcm);
    }

    private void CollectAllFramerates(Animation animation, HashSet<int> set)
    {
        set.Add(animation.FrameRate);
        foreach (Asset asset in animation.Assets)
            if (asset.AnimationName != null)
                CollectAllFramerates(Animations[asset.AnimationName], set);
    }

    private TimeSpan ComputeClipDuration(Animation root, int clipFramerate, TimeSpan maxClipDuration)
    {
        List<TimeSpan> durations = new();
        CollectAllDurations(root, durations);

        if (durations.Count == 1)
            return durations[0];

        if (clipFramerate >= 60)
            return maxClipDuration;

        TimeSpan lcm = durations[0];
        for (int i = 1; i < durations.Count; ++i)
        {
            lcm = LcmTime(lcm, durations[i]);
            if (lcm >= TimeSpan.MaxValue)
                break;
        }
        return lcm >= TimeSpan.MaxValue ? maxClipDuration : lcm;
    }

    private void CollectAllDurations(Animation animation, List<TimeSpan> list)
    {
        int loopFrames = animation.KeyFrames.Count == 0 ? 1 : animation.KeyFrames[^1];
        list.Add(TimeSpan.FromSeconds(loopFrames / (double)animation.FrameRate));
        foreach (Asset a in animation.Assets)
            if (a.AnimationName != null)
                CollectAllDurations(Animations[a.AnimationName], list);
    }

    private async Task RenderAnimationAsync(string path, SKCanvas canvas, Animation animation, TimeSpan time, int clipFps, Matrix3x2 parentTransform, bool renderIntermediateSteps, CancellationToken ct, List<Animation> stack = null)
    {
        stack ??= new List<Animation>();
        stack.Add(animation);

        int loopFrames = animation.KeyFrames.Count == 0 ? 1 : animation.KeyFrames[^1];
        int localFrame = TimeToFrame(time, animation.FrameRate, loopFrames);

        Dictionary<int, (Matrix3x2 Transform, SKColor ColorMask)> boneTransforms = new();
        EvaluateBoneRecursive(animation.Bone, animation, localFrame, parentTransform, boneTransforms, ct);

        foreach (Asset asset in animation.Assets)
        {
            ct.ThrowIfCancellationRequested();

            if (!asset.IsVisibleInFrame(localFrame))
                continue;

            if (!boneTransforms.TryGetValue(asset.BoneId, out (Matrix3x2 Transform, SKColor ColorMask) b))
                continue;

            Matrix3x2 local = Compose(
                asset.X * Math.Sign(asset.ScaleX),
                -asset.Y * Math.Sign(asset.ScaleY),
                asset.ScaleX,
                asset.ScaleY,
                asset.Rotation
            );
            Matrix3x2 global = local * b.Transform;

            if (asset.IsSprite)
            {
                await DrawSpriteAsync($"{path}--(B){asset.BoneId}--(A){asset.SpriteName}", canvas, asset.SpriteName, global, b.ColorMask, renderIntermediateSteps, ct);
            }
            else if (asset.IsAnimation)
            {
                Animation childAnimation = Animations[asset.AnimationName];
                int subLoop = childAnimation.KeyFrames.Count == 0 ? 1 : childAnimation.KeyFrames[^1];
                int subFrame = TimeToFrame(time, childAnimation.FrameRate, subLoop);
                await RenderAnimationAsync($"{path}--(B){asset.BoneId}--(AN){asset.AnimationName}", canvas, childAnimation, time, clipFps, global, renderIntermediateSteps, ct, stack);
            }
            else
            {
                Log.Warn($@"Ignoring asset in animation ""{animation.Name}"", because it has no texture nor animation defined");
                continue;
            }
        }
    }

    private int TimeToFrame(TimeSpan time, int fps, int loopFrames)
    {
        int frame = (int)(time.TotalSeconds * fps);
        return (frame % loopFrames) + 1;
    }

    private void EvaluateBoneRecursive(Bone bone, Animation animation, int frameId, Matrix3x2 parent, Dictionary<int, (Matrix3x2 Transform, SKColor ColorMask)> result, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        BoneKeyFrame boneKeyFrame = GetKeyframe(bone, animation, frameId);
        Matrix3x2 local = Compose(boneKeyFrame.X, -boneKeyFrame.Y, boneKeyFrame.ScaleX, boneKeyFrame.ScaleY, boneKeyFrame.Rotation);
        Matrix3x2 global = local * parent;
        result[bone.Id] = (global, boneKeyFrame.ColorMask);
        foreach (Bone child in bone.Children)
            EvaluateBoneRecursive(child, animation, frameId, global, result, ct);
    }

    private BoneKeyFrame GetKeyframe(Bone bone, Animation animation, int frame)
    {
        int activeFrame = GetActiveKeyframe(animation.KeyFrames, frame);

        for (int i = bone.KeyFrames.Count - 1; i >= 0; i--)
            if (bone.KeyFrames[i].FrameId == activeFrame)
                return bone.KeyFrames[i];

        BoneKeyFrame best = bone.KeyFrames[0];
        for (int i = 0; i < bone.KeyFrames.Count; i++)
        {
            if (bone.KeyFrames[i].FrameId > activeFrame)
                return best;

            best = bone.KeyFrames[i];
        }
        return best;
    }

    private int GetActiveKeyframe(IReadOnlyList<int> keyFrames, int frame)
    {
        int active = keyFrames[0];
        for (int i = 0; i < keyFrames.Count; i++)
        {
            if (keyFrames[i] > frame)
                return active;
            active = keyFrames[i];
        }
        return active;
    }

    private async Task DrawSpriteAsync(string path, SKCanvas canvas, int spriteId, Matrix3x2 transform, SKColor colorMask, bool renderIntermediateSteps, CancellationToken ct)
    {
        if (!Sprites.TryGetValue(spriteId, out Sprite sprite))
            return;

        if (renderIntermediateSteps)
            await sprite.TryExportToPngAsync(path + ".png", Log, ct);

        SKMatrix skMatrix = new()
        {
            ScaleX = transform.M11,
            SkewX = transform.M21,
            TransX = transform.M31,
            SkewY = transform.M12,
            ScaleY = transform.M22,
            TransY = transform.M32,
            Persp2 = 1
        };

        canvas.SetMatrix(skMatrix);

        float cx = sprite.CroppedWidth / 2f;
        float cy = sprite.CroppedHeight / 2f;

        ct.ThrowIfCancellationRequested();

        if (colorMask != SKColors.White)
        {
            for (int y = 0; y < sprite.SKBitmap.Height; y++)
            {
                ct.ThrowIfCancellationRequested();

                for (int x = 0; x < sprite.SKBitmap.Width; x++)
                {
                    SKColor pixel = sprite.SKBitmap.GetPixel(x, y);

                    // Tint (multiply)

                    float a = colorMask.Alpha / 255f;

                    // Interpolate between original and tinted
                    byte r = (byte)(pixel.Red * (1 - a) + (byte)(pixel.Red * colorMask.Red / 255) * a);
                    byte g = (byte)(pixel.Green * (1 - a) + (byte)(pixel.Green * colorMask.Green / 255) * a);
                    byte b = (byte)(pixel.Blue * (1 - a) + (byte)(pixel.Blue * colorMask.Blue / 255) * a);

                    sprite.SKBitmap.SetPixel(x, y, new SKColor(r, g, b, pixel.Alpha));
                }
            }
        }
        ct.ThrowIfCancellationRequested();
        canvas.DrawBitmap(sprite.SKBitmap, -cx, -cy);
    }

    public Matrix3x2 Compose(float x, float y, float sx, float sy, float rotationDeg)
    {
        float r = MathF.PI / 180f * rotationDeg;
        return Matrix3x2.CreateScale(sx, sy) * Matrix3x2.CreateRotation(r) * Matrix3x2.CreateTranslation(x, y);
    }

    public int Lcm(int a, int b)
    {
        if (a == 0 || b == 0)
            return 0;

        long gcd = Gcd(a, b);
        long lcm = a / gcd * b;

        if (lcm > int.MaxValue)
            return int.MaxValue;

        return (int)lcm;
    }

    public long Gcd(long a, long b)
    {
        while (b != 0)
        {
            long t = b;
            b = a % b;
            a = t;
        }
        return Math.Abs(a);
    }

    private TimeSpan LcmTime(TimeSpan a, TimeSpan b)
    {
        long gcd = Gcd(a.Ticks, b.Ticks);
        long lcm = (a.Ticks / gcd) * b.Ticks;
        return lcm < 0 || lcm >= int.MaxValue ? TimeSpan.MaxValue : TimeSpan.FromTicks(lcm);
    }

    private SKRectI ComputeGlobalBounds(Clip clip)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        int globalWidth = clip.Frames[0].Image.Width;
        int globalHeight = clip.Frames[0].Image.Height;

        int frameId = 0;
        foreach (SKBitmap bmp in clip.Frames.Select(f => f.Image))
        {
            Log.Debug($"Computing image bounds for frame {++frameId} of {clip.Frames.Length}...");
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

        int width = (maxX + 1) - minX;
        int height = (maxY + 1) - minY;

        // Clamp to bitmap bounds FIRST
        width = Math.Min(width, globalWidth - minX);
        height = Math.Min(height, globalHeight - minY);

        // Make even WITHOUT exceeding bounds
        if (width % 2 != 0)
        {
            if (minX + width < globalWidth)
                width++;
            else if (width > 1)
                width--;
        }

        if (height % 2 != 0)
        {
            if (minY + height < globalHeight)
                height++;
            else if (height > 1)
                height--;
        }

        return new SKRectI(
            minX,
            minY,
            minX + width,
            minY + height
        );
    }

    private static SKBitmap Crop(SKBitmap src, SKRectI bounds)
    {
        SKBitmap dst = new(bounds.Width, bounds.Height);

        using SKCanvas canvas = new(dst);
        canvas.Clear(SKColors.Transparent);

        canvas.DrawBitmap(src, -bounds.Left, -bounds.Top);

        return dst;
    }
}