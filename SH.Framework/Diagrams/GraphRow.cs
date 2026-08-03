using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SH.Framework.Diagrams;

public sealed class GraphRow : IReadOnlyList<GraphCell>
{
    internal GraphRow(GraphGrid grid, int r)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        RowNum = r;
        GraphCell[] cells = new GraphCell[Grid.ColCount];
        for (int c = 0; c < cells.Length; ++c)
            cells[c] = new(this, c);
        Cells = cells;
    }

    public GraphGrid Grid { get; }
    public int RowNum { get; internal set; }
    public int Count => Cells.Count;
    public int ColCount => Cells.Count;
    public IReadOnlyList<GraphCell> Cells { get; }


    public GraphCell this[int index] => Cells[index];


    public bool IsEmpty() => Cells.All(c => c.IsEmpty);

    internal void Clear()
    {
        foreach (GraphCell cell in Cells)
            cell.Node = null;
    }


    public IEnumerator<GraphCell> GetEnumerator() => ((IEnumerable<GraphCell>)Cells).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Cells.GetEnumerator();

    public override string ToString() => RowNum.ToString("000");
}
