using SH.Framework.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SH.Framework.Diagrams;

/// <summary>
/// Node dependency group
/// </summary>
public sealed class GraphGroup : IReadOnlyList<GraphNode>
{
    internal GraphGroup(int id)
    {
        Id = id;
    }

    public GraphGrid Grid { get; private set; }

    public int Id { get; internal set; }

    private readonly List<GraphNode> Nodes = [];
    public int Count => Nodes.Count;

    public int MaxTrunkDepth { get; private set; }
    public int MaxLeafDepth { get; private set; }

    public GraphNode this[int index] => Nodes[index];

    public bool Contains(GraphNode node) => Nodes.Contains(node);

    public void Clear() => Nodes.Clear();

    public void Add(GraphNode node) => Nodes.Add(node);
    public void AddRange(IEnumerable<GraphNode> nodes) => Nodes.AddRange(nodes);

    public void Remove(GraphNode node) => Nodes.Remove(node);
    public void RemoveRange(IEnumerable<GraphNode> nodes)
    {
        foreach (GraphNode node in nodes)
            Nodes.AddRange(node);
    }

    public IEnumerator<GraphNode> GetEnumerator() => Nodes.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Nodes.GetEnumerator();

    public int GetColumnMaxLeafDepth(int trunkDepth)
    {
        List<GraphNode> leafNodes = Nodes.Where(n => n.TrunkDepth == trunkDepth && n.IsLeaf).ToList();
        return leafNodes.Count <= 0 ? 0 : leafNodes.Max(n => n.LeafDepth);
    }

    internal void FindLeafNodes(CancellationToken ct)
    {
        List<GraphNode> selected = Nodes.Where(n => n.Children.Count <= 0).ToList(); // childless nodes
        while (selected.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            foreach (GraphNode node in selected)
            {
                node.IsLeaf = node.Parents.Count == 1 && node.Children.All(ch => ch.IsLeaf);
                node.Size = 1 + node.LeafChildren.Sum(ch => ch.Size);
            }

            selected = selected.SelectMany(n => n.Parents).Distinct().ToList();
        }
    }

    internal bool CalculateDepth(CancellationToken ct)
    {
        // Trunk depth (a leaf node has same trunk depth as its closest trunk ancestor):
        List<GraphNode> selected = Nodes.Where(n => n.IsRoot).ToList(); // root nodes
        for (int depth = 0; selected.Count > 0; ++depth)
        {
            ct.ThrowIfCancellationRequested();
            foreach (GraphNode node in selected)
            {
                node.TrunkDepth = node.IsLeaf ? node.FirstParent.TrunkDepth : depth;
                MaxTrunkDepth = Math.Max(MaxTrunkDepth, node.TrunkDepth);
            }
            selected = selected.SelectMany(n => n.Children).Distinct().ToList();
        }

        // Leaf depth (all root/trunk nodes have leaf depth = 0):
        selected = Nodes.Where(n => n.IsTrunk && n.HasLeafChildren).ToList();
        for (int depth = 0; selected.Count > 0; ++depth)
        {
            ct.ThrowIfCancellationRequested();
            foreach (GraphNode node in selected)
            {
                node.LeafDepth = depth;
                MaxLeafDepth = Math.Max(MaxLeafDepth, node.LeafDepth);
            }
            selected = selected.SelectMany(n => n.LeafChildren).ToList();
        }

        // Done.
        return true;
    }


    internal void ComputeLayout(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Grid = new(this);

        List<GraphNode> sortedTrunkNodes;
        List<GraphNode> unsortedTrunkNodes = Nodes.Where(n => n.IsTrunk).OrderBy(n => n.TrunkDepth).ThenBy(n => n.Ancestors.Count).ToList();
        if (unsortedTrunkNodes.Count <= 1)
        {
            sortedTrunkNodes = unsortedTrunkNodes;
        }
        else
        {
            double[,] matrix = new double[unsortedTrunkNodes.Count, unsortedTrunkNodes.Count];
            for (int i = 0; i < unsortedTrunkNodes.Count; ++i)
            {
                for (int j = i + 1; j < unsortedTrunkNodes.Count; ++j)
                {
                    GraphNode a = unsortedTrunkNodes[i];
                    GraphNode b = unsortedTrunkNodes[j];
                    if (a.Descendants.Contains(b) || b.Descendants.Contains(a))
                    {
                        int affinity = 1 + 10 * (MaxTrunkDepth - Math.Abs(a.TrunkDepth - b.TrunkDepth));
                        matrix[i, j] = affinity;
                        matrix[j, i] = affinity;
                    }
                }
            }

            int[] sortedIndices = SpectralSorting.Order(matrix);
            sortedTrunkNodes = [];
            for (int i = 0; i < sortedIndices.Length; ++i)
                sortedTrunkNodes.Add(unsortedTrunkNodes[sortedIndices[i]]);
        }

        int initialRow = Grid.RowCount / 4;
        for (int i = 0; i < sortedTrunkNodes.Count; ++i)
        {
            GraphNode node = sortedTrunkNodes[i];
            initialRow = Grid.NextRowFit(node, initialRow, node.TrunkDepth);
            Grid.SetNode(node, initialRow, node.TrunkDepth);
            ++initialRow;
        }

        // Trim:
        Grid.TrimRows();

        // Place dummies:
        List<GraphCell> dummyCells = [];
        foreach (GraphNode node in Nodes.Where(n => n.HasTrunkChildren))
        {
            int maxDepth = node.GetMaxTrunkChildDepth();
            for (int i = node.Cell.ColNum + 1; i < maxDepth; ++i)
            {
                GraphCell cell = node.Cell.Row[i];
                if (cell.Node != null)
                    throw new Exception("A node is blocking a link");
                cell.Node = GraphNode.Dummy;
                dummyCells.Add(cell);
            }
        }

        // Insert leaves:
        List<GraphNode> parentsWithLeaves = Nodes.Where(n => n.IsTrunk && n.HasLeafChildren).OrderBy(n => n.Cell.ColNum).ToList();
        List<GraphNode> remainingParents = parentsWithLeaves.ToList();
        foreach (GraphNode node in parentsWithLeaves)
        {
            if (!remainingParents.Contains(node))
                continue;

            int r = node.Cell.RowNum;

            List<GraphNode> parents = remainingParents.Where(n => n.Cell.RowNum == r).ToList();
            remainingParents.RemoveAll(parents);


            Grid.WriteDebug();

            int addedRowCount = parents.Max(n => n.Size) - 1;
            Grid.InsertRowsAt(r + 1, addedRowCount);

            Grid.WriteDebug();

            foreach (GraphNode parent in parents)
            {
                int rr = r;
                foreach (GraphNode leaf in parent.LeafChildren)
                    rr = AddLeafNode(leaf, rr);
                Grid.WriteDebug();
            }
        }



        // Remove dummies:
        foreach (GraphCell cell in dummyCells)
            cell.Node = null;



        // Compact grid:
        Grid.WriteDebug();
        int moved;
        do
        {
            moved = 0;

            for (int r = 1; r < Grid.Count; ++r)
            {
                for (int c = 0; c <= MaxTrunkDepth; ++c)
                {
                    GraphCell cell = Grid[r][c];
                    GraphNode node = cell.Node;
                    if (node == null || node == GraphNode.Dummy)
                        continue;

                    int desiredRow = node.IsRoot ? 0 : node.Parents.OrderBy(n => n.Cell.RowNum).First().Cell.RowNum;
                    if (desiredRow >= node.Cell.RowNum)
                        continue;

                    GraphRow targetRow = Grid[r - 1];
                    GraphCell targetCell = targetRow[c];
                    if (targetCell.Node != null)
                        continue;

                    int maxTrunkChildDepth = node.GetMaxTrunkChildDepth();
                    if (targetRow.Any(cell2 => cell2.ColNum > c && cell2.ColNum < maxTrunkChildDepth && cell2.Node != null))
                        continue;

                    if (targetRow.Any(cell2 => cell2.ColNum < c && cell2.Node != null && cell2.Node.NeedsCell(targetCell)))
                        continue;

                    cell.Node = null; // clear current cell
                    Grid.SetNode(node, targetCell); // set to new cell

                    Grid.WriteDebug();

                    ++moved;
                }
            }
        } while (moved > 0);

        // Trim:
        Grid.TrimRows();

        // Done.
        Grid.WriteDebug();
    }

    private int AddLeafNode(GraphNode node, int r)
    {
        ++r;
        int c = node.TrunkDepth;
        Grid.SetNode(node, r, c);
        foreach (GraphNode leaf in node.LeafChildren)
            r = AddLeafNode(leaf, r);
        return r;
    }





    public override string ToString() =>
        $@"{Id}: {{ {Nodes.Select(n => n.Id).JoinToString(", ")} }}";

}
