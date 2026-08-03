using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SH.Framework.Diagrams;

public sealed class GraphGrid : IReadOnlyList<GraphRow>
{
    internal GraphGrid(GraphGroup group)
    {
        Group = group ?? throw new ArgumentNullException(nameof(group));
        ColCount = group.MaxTrunkDepth + 1;

        int rowCount = 100 * Nodes.Where(n => n.IsTrunk).Sum(n => n.Size);
        List<GraphRow> rows = [];
        for (int r = 0; r < rowCount; ++r)
            rows.Add(new GraphRow(this, r));

        Rows = rows;
    }

    public GraphGroup Group { get; }
    public IReadOnlyList<GraphNode> Nodes => Group;
    public int ColCount { get; }
    public int RowCount => Rows.Count;

    private List<GraphRow> Rows;

    public int Count => Rows.Count;
    public GraphRow this[int index] => Rows[index];

    internal void ClearCell(GraphCell cell)
    {
        cell.Node = null;
    }

    internal void SetNode(GraphNode node, int r, int c) =>
        SetNode(node, Rows[r][c]);

    internal void SetNode(GraphNode node, GraphCell cell)
    {
        if (cell.Node == node)
            return;

        if (cell.Node != null)
            throw new Exception("Cell is not empty!");

        cell.Node = node;
        node.Cell = cell;
    }

    internal int NextRowFit(GraphNode node, int r, int c)
    {
        for (; r < Rows.Count; ++r)
            if (NodeFits(node, r, c))
                return r;
        return -1;
    }

    internal bool NodeFits(GraphNode n, int r, int c)
    {
        for (int rr = r; rr < r + 1 /*n.Size*/; ++rr)
        {
            // Out of Grid:
            if (rr < 0 || rr >= Rows.Count)
                return false;

            // Cell is occupied:
            if (Rows[rr][c].Node != null)
                return false;
        }

        // Check whether dependency lines hits any node to the right:
        GraphRow row = Rows[r];
        int maxDescendantCol = n.GetMaxTrunkChildDepth();
        for (int cc = n.TrunkDepth + 1; cc < maxDescendantCol; ++cc)
            if (row[cc].Node != null)
                return false;

        // Check whether any node to the left has a dependency line occupying the required rows: 
        for (int rr = r; rr < r + 1 /* n.Size*/; ++rr)
        {
            for (int cc = 0; cc < c; ++cc)
            {
                GraphCell cell = Rows[rr][cc];
                if (cell.Node == null)
                    continue;
                maxDescendantCol = cell.Node.GetMaxTrunkChildDepth();
                if (maxDescendantCol > c)
                    return false;
            }
        }

        // It fits.
        return true;
    }

    public void TrimRows()
    {
        List<GraphRow> rows = [];
        foreach (GraphRow row in Rows.Where(row => !row.IsEmpty()))
        {
            row.RowNum = rows.Count;
            rows.Add(row);
        }
        Rows = rows;
    }

    public void WriteDebug()
    {
        StringBuilder sb = new();
        foreach (GraphRow row in Rows)
        {
            sb.Append(row.RowNum.ToString("000  "));
            foreach (GraphCell cell in row)
            {
                if (cell.Node == GraphNode.Dummy)
                    sb.Append("=");
                else if (cell.Node != null)
                    sb.Append($"{(cell.Node.IsLeaf ? "L" : cell.Node.IsRoot ? "R" : "T")}");
                else
                {
                    List<GraphCell> leftSideCells = [];

                    for (int c = 0; c < cell.ColNum; ++c)
                    {
                        GraphCell otherCell = cell.Row[c];
                        if (otherCell.Node == null || otherCell.Node == GraphNode.Dummy)
                            continue;
                        leftSideCells.Add(otherCell);
                    }

                    if (leftSideCells.Count <= 0)
                    {
                        sb.Append(" ");
                        continue;
                    }

                    if (leftSideCells.Any(c => c.Node.NeedsCell(cell)))
                    {
                        sb.Append("=");
                        continue;
                    }

                    sb.Append(" ");
                    continue;
                }
            }
            sb.AppendLine();
        }
        sb.AppendLine();
        Debug.WriteLine(sb.ToString());
    }

    public void InsertRowsAt(int pos, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            GraphRow row = new(this, pos + i);
            Rows.Insert(pos + i, row);
        }

        for (int r = pos + count; r < Rows.Count; ++r)
            Rows[r].RowNum = r;
    }

    public IEnumerator<GraphRow> GetEnumerator() => Rows.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Rows.GetEnumerator();
}
