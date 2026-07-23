using SH.Content.Art;
using SH.Framework.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Content.DEPRECATED;

public sealed class FrameBasedAnimationRenderer
{
    private IReadOnlyDictionary<string, Animation> Animations { get; }
    private IReadOnlyDictionary<int, Sprite> Sprites { get; }

    private readonly ILogger Log;


    public FrameBasedAnimationRenderer(IReadOnlyDictionary<string, Animation> animationsByName, IReadOnlyDictionary<int, Sprite> sprites, ILogger log)
    {
        Animations = animationsByName;
        Sprites = sprites;
        Log = log ?? new VoidLogger();
    }



    public int ComputeLoopLength(string root)
    {
        HashSet<string> visited = new();
        return ComputeLoopRecursive(root, visited);
    }



    private int ComputeLoopRecursive(string name, HashSet<string> visited)
    {
        if (!visited.Add(name))
            return 1;

        Animation anim = Animations[name];
        int length = anim.KeyFrames.Count > 0 ? anim.KeyFrames.Count : 1;

        foreach (Asset item in anim.Assets)
        {
            if (item.AnimationName != null)
            {
                int sub = ComputeLoopRecursive(item.AnimationName, visited);
                length = Lcm(length, sub);
            }
        }
        return length;
    }



    public async Task<SKBitmap> RenderFrameAsync(Animation animation, int frameId, int width, int height, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        SKBitmap bmp = new(width, height);
        using (SKCanvas canvas = new(bmp))
        {
            canvas.Clear(SKColors.Transparent);
            Matrix3x2 canvasMatrix = Compose(bmp.Width / 2, bmp.Height / 2, 1, 1, 0);
            ct.ThrowIfCancellationRequested();
            await RenderAnimationAsync($"A[{animation.Name}]--F[{frameId}]", canvas, animation, frameId, canvasMatrix, ct);
        }
        return bmp;
    }



    private async Task RenderAnimationAsync(string path, SKCanvas canvas, Animation animation, int frameId, Matrix3x2 parentTransform, CancellationToken ct)
    {
        int length = animation.KeyFrames.Count == 0 ? 1 : animation.KeyFrames[^1]; // last keyframe defines duration
        int localFrame = ((frameId - 1) % length) + 1; // convert to 1-based looping
        Dictionary<int, Matrix3x2> boneTransforms = new();

        ct.ThrowIfCancellationRequested();

        EvaluateBoneRecursive(animation.Bone, animation, localFrame, parentTransform, boneTransforms);

        foreach (Asset asset in animation.Assets)
        {
            ct.ThrowIfCancellationRequested();

            if (!asset.IsVisibleInFrame(localFrame))
                continue;

            if (!boneTransforms.TryGetValue(asset.BoneId, out Matrix3x2 boneTransform))
                continue;

            Matrix3x2 local = Compose(asset.X * Math.Sign(asset.ScaleX), -asset.Y * Math.Sign(asset.ScaleY), asset.ScaleX, asset.ScaleY, asset.Rotation);
            Matrix3x2 global = local * boneTransform;

            if (asset.IsSprite)
            {
                string itemPath = $"{path}--B[{asset.BoneId}]--T[{asset.SpriteName}]";
                await DrawSpriteAsync(itemPath, canvas, asset.SpriteName, global, ct);
            }
            else if (asset.AnimationName != null)
            {
                string itemPath = $"{path}--B[{asset.BoneId}]--A[{asset.AnimationName}]";
                Animation subAnim = Animations[asset.AnimationName];
                int subFrame = MapFrame(localFrame, asset, subAnim);
                await RenderAnimationAsync(itemPath, canvas, Animations[asset.AnimationName], subFrame, global, ct);
            }
        }
    }



    private int MapFrame(int parentFrame, Asset asset, Animation subAnimation)
    {
#warning The animation should NOT be rendered after the end frame! Otherwise is causes the last frame to stay visible in the final image! This method should return -1 when the animation should not be rendered!


        int start = asset.StartFrame;
        int end = asset.EndFrame;
        int range = Math.Max(1, end - start + 1);
        int localFrame = parentFrame;

        if (asset.Loop)
        {
            // LOOPING: wrap
            localFrame = ((localFrame - 1) % range) + 1;
            return start + (localFrame - 1);
        }
        else
        {
            // NON-LOOPING: clamp
            if (localFrame > range)
                localFrame = range;
            return start + (localFrame - 1);
        }
    }



    private void EvaluateBoneRecursive(Bone bone, Animation animation, int frameId, Matrix3x2 parent, Dictionary<int, Matrix3x2> result)
    {
        BoneKeyFrame kf = GetKeyframe(bone, animation, frameId);
        Matrix3x2 local = Compose(kf.X, kf.Y, kf.ScaleX, kf.ScaleY, kf.Rotation);
        Matrix3x2 global = local * parent;
        result[bone.Id] = global;
        foreach (Bone child in bone.Children)
            EvaluateBoneRecursive(child, animation, frameId, global, result);
    }



    private BoneKeyFrame GetKeyframe(Bone bone, Animation animation, int frame)
    {
        int activeFrame = GetActiveKeyframe(animation.KeyFrames, frame);

        // find exact keyframe match on bone
        for (int i = bone.KeyFrames.Count - 1; i >= 0; i--)
            if (bone.KeyFrames[i].FrameId == activeFrame)
                return bone.KeyFrames[i];

        // fallback: last known <= activeFrame
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



    private async Task DrawSpriteAsync(string path, SKCanvas canvas, int spriteId, Matrix3x2 transform, CancellationToken ct)
    {
        if (!Sprites.TryGetValue(spriteId, out Sprite sprite))
            return;

        await sprite.TryExportToPngAsync(path + ".png", Log, ct);

        SKMatrix skMatrix = new()
        {
            ScaleX = transform.M11,
            SkewX = transform.M21,
            TransX = transform.M31,
            SkewY = transform.M12,
            ScaleY = transform.M22,
            TransY = transform.M32,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };

        canvas.SetMatrix(skMatrix);

        // draw centered
        float cx = sprite.CroppedWidth / 2f;
        float cy = sprite.CroppedHeight / 2f;

        ct.ThrowIfCancellationRequested();

        canvas.DrawBitmap(sprite.SKBitmap, -cx, -cy);
    }

    public static Matrix3x2 Compose(float x, float y, float sx, float sy, float rotationDeg)
    {
        float r = MathF.PI / 180f * rotationDeg;

        Matrix3x2 scale = Matrix3x2.CreateScale(sx, sy);
        Matrix3x2 rot = Matrix3x2.CreateRotation(r);
        Matrix3x2 trans = Matrix3x2.CreateTranslation(x, y);

        return scale * rot * trans;
    }

    public static int Lcm(int a, int b)
    {
        return Math.Abs(a * b) / Gcd(a, b);
    }

    public static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            int t = b;
            b = a % b;
            a = t;
        }
        return a;
    }
}