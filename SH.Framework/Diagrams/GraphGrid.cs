using System.Collections;
using System.Collections.Generic;

namespace SH.Framework.Diagrams;

public sealed class GraphGrid : IEnumerable<GraphRow>
{
    internal GraphGrid() { }

    public OrderedDictionary<string, GraphNode> Nodes { get; internal set; } = [];
    public List<GraphNodeGroup> Groups { get; internal set; } = [];

    public int RowCount { get; internal set; }
    public int ColumnCount { get; internal set; }
    public GraphRow TopRow { get; internal set; }
    public GraphRow BottomRow { get; internal set; }

    internal GraphRow AddTopRow()
    {
        ++RowCount;
        if (TopRow == null)
            return TopRow = BottomRow = new GraphRow(this);
        else return TopRow.InsertNewRowAbove();
    }

    internal GraphRow AddBottomRow()
    {
        ++RowCount;
        if (BottomRow == null)
            return TopRow = BottomRow = new GraphRow(this);
        else return BottomRow.InsertNewRowBelow();
    }

    public IEnumerator<GraphRow> GetEnumerator()
    {
        for (GraphRow row = TopRow; row != null; row = row.RowBelow)
            yield return row;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        for (GraphRow row = TopRow; row != null; row = row.RowBelow)
            yield return row;
    }
}
