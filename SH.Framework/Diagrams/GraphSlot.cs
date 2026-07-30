namespace SH.Framework.Diagrams;

public readonly struct GraphSlot
{
    public readonly int Row;
    public readonly int Col;
    public GraphSlot() { }
    public GraphSlot(int row, int col)
    {
        Row = row;
        Col = col;
    }
}
