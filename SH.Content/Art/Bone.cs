using SH.Content.Xml.Animations;
using System.Collections.Generic;
using System.Linq;

namespace SH.Content.Art;

public sealed class Bone
{
    public AnimationXml_Bone Xml { get; }

    public int Id => Xml.Id;
    public List<BoneKeyFrame> KeyFrames = [];
    public List<Bone> Children = [];

    public Bone(AnimationXml_Bone xml)
    {
        Xml = xml;
        foreach (AnimationXml_BonePosition pos in xml.Positions.OrderBy(pos => pos.FrameId))
            KeyFrames.Add(new(pos));
        foreach (AnimationXml_Bone b in xml.Children.OrderBy(b => b.Id))
            Children.Add(new Bone(b));
    }
}
