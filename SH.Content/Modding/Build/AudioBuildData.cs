using SH.Content.Enums;

namespace SH.Content.Modding.Build;

internal sealed class AudioBuildData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string RelativePath { get; set; }
    public string AbsolutePath { get; set; }
    public EAudioEncoder AudioEncoder { get; set; }
    public EAudioType AudioType { get; set; }

    public override string ToString() => Name;
}
