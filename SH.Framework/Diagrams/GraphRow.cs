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

    internal GraphRow(GraphGrid grid, int columns)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        Cells = new GraphCell[columns];
    }

    public bool IsAbove(GraphRow otherRow) =>
    SortOrder < otherRow.SortOrder;

    public bool IsBelow(GraphRow otherRow) =>
        SortOrder > otherRow.SortOrder;

    internal void InsertNewRowAbove()
    {
        GraphRow newRow = new(Grid, Grid.Columns)
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
    }

    internal void InsertNewRowBelow()
    {
        GraphRow newRow = new(Grid, Grid.Columns)
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
    }
}
