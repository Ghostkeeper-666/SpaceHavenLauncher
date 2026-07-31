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
    public GraphNode(IDataNode dataNode)
    {
        Data = dataNode ?? throw new ArgumentNullException(nameof(dataNode));
    }

    public string Id => Data.Id;
    public IDataNode Data { get; }

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
    public GraphNodeGroup Group { get; internal set; }

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
    public IEnumerable<GraphNode> NormalChildren => Children.Where(n => !n.IsLeaf);
    public bool HasTrunkChildren => Children.Any(n => n.IsTrunk);


    public override string ToString() => $@"{Id} [{Size}]";
}
