using SH.Content.Xml.Animations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SH.Content.Art;

public sealed class Animation
{
    public AnimationXml Xml { get; }

    public string Name => Xml.Name;
    public int FrameRate => Xml.FrameRate;
    public int Id => Xml.Id;
    public IReadOnlyList<int> KeyFrames { get; }
    public Bone Bone { get; }
    public IReadOnlyDictionary<int, Bone> BoneById { get; }
    public IReadOnlyList<Asset> Assets { get; }

    public Animation(AnimationXml animationXml)
    {
        Xml = animationXml ?? throw new ArgumentNullException(nameof(animationXml));
        KeyFrames = Xml.KeyFrames.OrderBy(f => f).ToArray();
        Bone = new(Xml.Bone);
        OrderedDictionary<int, Bone> boneById = [];
        IndexBones(boneById);
        BoneById = boneById;
        Assets = Xml.Items.Select(i => new Asset(i)).ToArray();
    }

    public void IndexBones(IDictionary<int, Bone> dict)
    {
        dict.Clear();
        IndexRecursive(Bone, dict);
    }

    private void IndexRecursive(Bone bone, IDictionary<int, Bone> dict)
    {
        dict[bone.Id] = bone;
        foreach (Bone child in bone.Children)
            IndexRecursive(child, dict);
    }

    public override string ToString() => Name;
}