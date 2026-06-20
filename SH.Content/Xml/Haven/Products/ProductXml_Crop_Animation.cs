using SH.Content.Enums;

namespace SH.Content.Xml.Haven.Products;

public sealed class ProductXml_Crop_Animation
{
    public ProductXml_Crop_Stage Stage { get; set; }

    /// <summary>
    /// [stages/l/anims/l/animation/aid]
    /// </summary>
    public string AnimationId { get; set; }

    /// <summary>
    /// [stages/l/anims/l/type]
    /// </summary>
    public EAnimationType AnimationType { get; set; }

    /// <summary>
    /// [stages/l/anims/l/dir]
    /// </summary>
    public EDirection Direction { get; set; }

    /// <summary>
    /// [stages/l/anims/l/useFlipped]
    /// </summary>
    public bool UseFlipped { get; set; }
}