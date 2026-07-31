using System;
using System.Collections;
using System.Collections.Generic;

namespace SH.Framework.Diagrams;

public sealed class GraphRow : IReadOnlyList<GraphCell>
{
    internal GraphRow(GraphGrid grid)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        Cells = new GraphCell[Grid.ColumnCount];
        for (int i = 0; i < Cells.Length; ++i)
            Cells[i] = new();
    }



    public GraphGrid Grid { get; }
    internal double SortOrder { get; set; }

    private readonly GraphCell[] Cells;
    public GraphRow RowAbove { get; internal set; }
    public GraphRow RowBelow { get; internal set; }

    public bool IsTopRow => Grid.TopRow == this;
    public bool IsBottomRow => Grid.BottomRow == this;

    public int Count => Cells.Length;
    public int Columns => Cells.Length;



    public GraphCell this[int index] => Cells[index];

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

    public IEnumerator<GraphCell> GetEnumerator() => ((IEnumerable<GraphCell>)Cells).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Cells.GetEnumerator();
}
