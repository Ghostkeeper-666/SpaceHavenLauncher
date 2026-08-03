using SH.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SH.Framework.Diagrams;

/// <summary>
/// A graphic node representing a data node.
/// - It may have locked descendants, which MUST be contiguous row neighbors
/// - It may have 
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class GraphNode
{
    internal static GraphNode Dummy = new();

    internal GraphNode()
    {
        Id = string.Empty;
        TrunkDepth = -1;
        LeafDepth = 0;
        Size = 1;
    }

    public GraphNode(IDataNode dataNode)
    {
        Data = dataNode ?? throw new ArgumentNullException(nameof(dataNode));
        Id = !Data.Id.IsNullOrEmpty() ? Data.Id : throw new ArgumentNullException(nameof(IDataNode.Id));
    }

    public string Id { get; }
    public IDataNode Data { get; }
    public GraphCell Cell { get; set; }

    public int TrunkDepth { get; internal set; }
    public int LeafDepth { get; internal set; }

    /// <summary>
    /// Leaf nodes must have exactly 1 parent, and may have only leaf nodes, but never have trunk children
    /// </summary>
    public bool IsLeaf { get; internal set; }

    /// <summary>
    /// Every node which is not a leaf node is considered a trunk node
    /// </summary>
    public bool IsTrunk => !IsLeaf;

    /// <summary>
    /// Root nodes have no parent, therfore they are always trunk nodes too
    /// </summary>
    public bool IsRoot => Parents.Count <= 0;

    /// <summary>
    /// Node dependency group
    /// </summary>
    public GraphGroup Group { get; internal set; }

    public List<GraphNode> Ancestors { get; } = [];
    public List<GraphNode> Parents { get; } = [];
    public GraphNode FirstParent => Parents.Count > 0 ? Parents[0] : null;

    public GraphNode FirstRoot
    {
        get
        {
            GraphNode root = this;
            while (root.Parents.Count > 0)
                root = root.FirstParent;
            return root;
        }
    }

    /// <summary>
    /// How many rows this node requires, accounting for all locked descendants and self.
    /// </summary>
    public int Size { get; internal set; }

    public List<GraphNode> Descendants { get; } = [];

    /// <summary>
    /// List of ALL children
    /// </summary>
    public List<GraphNode> Children = [];
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Lists locked children, which:
    /// - have only 1 parent
    /// - have no children, or have only locked children
    /// - locked children necessarily need 
    /// </summary>
    public IEnumerable<GraphNode> LeafChildren => Children.Where(n => n.IsLeaf);
    public bool HasLeafChildren => Children.Any(n => n.IsLeaf);

    /// <summary>
    /// Lists children nodes which are NOT locked children!
    /// </summary>
    public IEnumerable<GraphNode> TrunkChildren => Children.Where(n => n.IsTrunk);
    public bool HasTrunkChildren => Children.Any(n => n.IsTrunk);

    //public double GetDesiredRowDueToAncestors()
    //{
    //    if (IsRoot)
    //        return Cell.RowNum;
    //    return Parents.OrderBy(p => p.Cell.RowNum).Skip(Parents.Count >> 1).FirstOrDefault().Cell.RowNum;
        
    //    //return Ancestors.Count <= 0 ? Cell.RowNum :
    //    //     0.8 * Cell.RowNum + 0.2 * (0.75 * Ancestors.Sum(p => p.Cell.RowNum) / (double)Ancestors.Count + 0.25 * Parents.Sum(p => p.Cell.RowNum) / (double)Parents.Count);
    //}

    internal int GetMaxLeafChildDepth() =>
        !HasLeafChildren ? LeafDepth : LeafChildren.Max(n => n.GetMaxLeafChildDepth());

    internal int GetMaxTrunkChildDepth() =>
        !IsTrunk || !HasTrunkChildren ? TrunkDepth : TrunkChildren.Max(n => n.TrunkDepth);

    internal bool NeedsCell(GraphCell cell)
    {
        if (cell.RowNum != Cell.RowNum)
            return false;
        if (cell.ColNum < Cell.ColNum)
            return false;
        if (cell.ColNum == Cell.ColNum)
            return true;
        return cell.ColNum < GetMaxTrunkChildDepth();
    }

    //public double GetDesiredRowDueToDescendants()
    //{
    //    if (!HasTrunkChildren)
    //        return Cell.RowNum;
    //    return TrunkChildren.OrderBy(p => p.Cell.RowNum).Skip((TrunkChildren.Count() - 1) >> 1).FirstOrDefault().Cell.RowNum;

    //    //List <GraphNode> trunkDesc = Descendants.Where(d => d.IsTrunk).ToList();
    //    //double desired = trunkDesc.Count <= 0 ? Cell.RowNum :
    //    //    1.0 * (0.5 * trunkDesc.Sum(p => p.Cell.RowNum) / (double)trunkDesc.Count + 0.5 * TrunkChildren.Sum(p => p.Cell.RowNum) / (double)TrunkChildren.Count());
    //    //return desired >= 0.0 ? desired : 0.0;
    //}

    public override string ToString() => Id;
}
