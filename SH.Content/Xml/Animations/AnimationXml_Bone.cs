using System.Collections.Generic;

namespace SH.Content.Xml.Animations;

public sealed class AnimationXml_Bone
{
    public AnimationXml Animation { get; set; }
    public int Id { get; set; }
    public List<AnimationXml_BonePosition> Positions { get; } = [];
    public List<AnimationXml_Bone> Children { get; } = [];
}
