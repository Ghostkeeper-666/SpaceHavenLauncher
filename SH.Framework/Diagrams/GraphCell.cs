using System;

namespace SH.Framework.Diagrams;

public sealed class GraphCell
{
    public GraphCell(GraphRow row, int colNum)
    {
        Row = row ?? throw new ArgumentNullException(nameof(row));
        ColNum = colNum;
    }

    public GraphRow Row { get; }
    public int RowNum => Row.RowNum;
    public int ColNum { get; }

    public bool IsEmpty => Node == null;
    public GraphNode Node { get; internal set; }

    public override string ToString() => $"[{RowNum.ToString("000")}, {ColNum}] = {Node?.Id ?? "null"}";
}
