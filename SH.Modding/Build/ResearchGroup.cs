using SH.Framework.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SH.Modding.Build;

public sealed class ResearchGroup
{
    public string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int SizeX { get; set; }
    public int SizeY { get; set; }

    public static string BorderColor { get; set; } = "#b7dde5e5";
    public static string FontColor { get; set; } = "#b7dde5e5";
    public static string BackgroundColor { get; set; } = "#1d3340be";

    public List<ResearchTopic> Topics { get; } = [];

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($@"""{Name}""");
        sb.Append($@": x={X}, y={Y}, sizeX={SizeX}, sizeY={SizeY}");
        if (Topics.Count > 0)
            sb.Append($@", topics=[{Topics.Select(d => $@"""{d.Name}""").JoinToString(",")}]");
        return sb.ToString();
    }
}
