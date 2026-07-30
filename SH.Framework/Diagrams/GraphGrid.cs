using System;
using System.Collections.Generic;
using System.Linq;

namespace SH.Framework.Diagrams;

public sealed class GraphGrid
{
    internal GraphGrid() { }

    public OrderedDictionary<string, GraphNode> Nodes { get; internal set; } = [];
    public List<GraphNodeGroup> Groups { get; internal set; } = [];

    public int Columns { get; private set; }
    public GraphRow TopRow { get; internal set; }
    public GraphRow BottomRow { get; internal set; }

    public void CalculateColumns() =>
        Columns = Nodes.Count <= 0 ? 0 : Nodes.Values.Where(n => n.Children.Count <= 0).Max(n => n.Depth);

    public void AddTopRow()
    {
        if(TopRow == null)
            TopRow = BottomRow = new GraphRow(this, Columns);
        else TopRow.InsertNewRowAbove();
    }

    public void AddBottomRow()
    {
        if(BottomRow == null)
            TopRow = BottomRow = new GraphRow(this, Columns);
        else BottomRow.InsertNewRowBelow();
    }
}
