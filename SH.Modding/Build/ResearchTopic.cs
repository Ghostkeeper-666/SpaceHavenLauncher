using SH.Framework.Diagrams;
using SH.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SH.Modding.Build;

public sealed class ResearchTopic : IDataNode<ResearchTopic>
{
    public static string BorderColor { get; set; } = "#ff4bf08d";
    public static string FontColor { get; set; } = "#ff4bf08d";
    public static string BackgroundColor { get; set; } = "#ff00420b";

    public ResearchTopic() { }

    public ResearchTopic(ResearchTopic other)
    {
        ArgumentNullException.ThrowIfNull(other);
        TechId = other.TechId;
        Name = other.Name;
        IsHidden = other.IsHidden;
        X = other.X;
        Y = other.Y;
        SizeX = other.SizeX;
        SizeY = other.SizeY;
    }

    public ResearchGroup Group { get; set; }
    public string TechId { get; set; }

    public string Name { get; set; }
    public bool IsHidden { get; set; }

    public int X { get; set; } = -1; // "not defined"
    public int Y { get; set; } = -1; // "not defined"

    public int SizeX { get; set; } = -1; // "not defined"
    public int SizeY { get; set; } = -1; // "not defined"

    public bool IsLeaf { get; set; } = false;

    public List<ResearchTopic> Parents { get; } = [];
    public List<ResearchTopic> Children { get; } = [];

    #region IDataNode
    IEnumerable<ResearchTopic> IDataNode<ResearchTopic>.Dependencies => Parents;
    string IDataNode.Id => TechId;
    public GraphNode GraphNode { get; set; }
    public GraphCell Cell => GraphNode.Cell;

    internal Mod Mod { get; set; }
    #endregion

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($@"""{Name}""");
        //if (IsHidden)
        //sb.Append($@" (hidden)");
        //sb.Append($@": x={X}, y={Y}, sizeX={SizeX}, sizeY={SizeY}");
        if (Parents.Count > 0)
            sb.Append($@": {{ ""deps"": {{ {Parents.Select(d => $@"""{d.Name}""").JoinToString(",")} }} }}");
        return sb.ToString();
    }
}
