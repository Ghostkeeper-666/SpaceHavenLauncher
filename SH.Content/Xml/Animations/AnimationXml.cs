using System.Collections.Generic;

namespace SH.Content.Xml.Animations;

public sealed class AnimationXml
{
    public string Name { get; set; } // n
    public int Id { get; set; } // id
    public int FrameRate { get; set; } // f
    public 
        List<int> KeyFrames { get; } = []; // ks
    public AnimationXml_Bone Bone { get; set; } // <bones><b> (root bone)
    public List<AnimationXml_Item> Items { get; } = []; // <items><assetPos>
}
