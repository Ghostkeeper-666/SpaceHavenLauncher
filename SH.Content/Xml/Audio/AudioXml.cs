using SH.Content.Enums;

namespace SH.Content.Xml.Audio;

public sealed class AudioXml
{
    public string Name { get; set; } // n
    public int Id { get; set; } // id
    public EAudioType Type { get; set; } // at
    public EAudioFormat Format { get; set; } // from mp3,ogg
    public string Path { get; set; } // from mp3,ogg
    public float Length { get; set; } // from mp3l,oggl
    public ESoundType Scope { get; set; } // st
    public int Volume { get; set; } // vo
}
