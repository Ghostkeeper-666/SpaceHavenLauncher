using System;
using System.Collections.Generic;

namespace SH.Framework.Diagrams;

public sealed class GraphRow
{
    public GraphGrid Grid { get; }
    internal double SortOrder { get; set; }

    public IReadOnlyList<GraphCell> Cells { get; }
    public GraphRow RowAbove { get; internal set; }
    public GraphRow RowBelow { get; internal set; }

    public GraphCell this[int index] => Cells[index];

    public bool IsTopRow => Grid.TopRow == this;
    public bool IsBottomRow => Grid.BottomRow == this;

    internal GraphRow(GraphGrid grid)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        GraphCell[] cells = new GraphCell[Grid.ColumnCount];
        for (int i = 0; i < cells.Length; ++i)
            cells[i] = new();
        Cells = cells;
    }

    public bool IsAbove(GraphRow otherRow) =>
    SortOrder < otherRow.SortOrder;

    public bool IsBelow(GraphRow otherRow) =>
        SortOrder > otherRow.SortOrder;

    internal GraphRow InsertNewRowAbove()
    {
        GraphRow newRow = new(Grid)
        {
            RowAbove = RowAbove,
            RowBelow = this,
        };
        RowAbove = newRow;
        if (newRow.RowAbove == null)
        {
            Grid.TopRow = newRow;
            newRow.SortOrder = SortOrder - 1.0;
        }
        else
        {
            newRow.RowAbove.RowBelow = newRow;
            newRow.SortOrder = (SortOrder + newRow.RowAbove.SortOrder) / 2.0;
        }
        return newRow;
    }

    internal GraphRow InsertNewRowBelow()
    {
        GraphRow newRow = new(Grid)
        {
            RowBelow = RowBelow,
            RowAbove = this,
        };
        RowBelow = newRow;
        if (newRow.RowBelow == null)
        {
            Grid.BottomRow = newRow;
            newRow.SortOrder = SortOrder + 1.0;
        }
        else
        {
            newRow.RowBelow.RowAbove = newRow;
            newRow.SortOrder = (SortOrder + newRow.RowBelow.SortOrder) / 2.0;
        }
        return newRow;
    }
}
