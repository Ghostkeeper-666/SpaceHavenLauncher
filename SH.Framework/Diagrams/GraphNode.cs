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

    public int Depth { get; internal set; }

    public bool IsLeaf { get; internal set; }
    public bool IsTrunk => !IsLeaf;
    public bool IsRoot => Parents.Count <= 0;

    /// <summary>
    /// Node dependency group
    /// </summary>
    public GraphNodeGroup Group { get; internal set; }

    public List<GraphNode> Parents { get; } = [];
    public GraphNode FirstParent => Parents[0];
    public GraphNode FirstRoot
    {
        get
        {
            GraphNode root = this;
            while (root.FirstParent != null)
                root = root.FirstParent;
            return root;
        }
    }

    /// <summary>
    /// How many rows this node requires, accounting for all locked descendants and self.
    /// </summary>
    public int Height { get; internal set; }

    /// <summary>
    /// List of ALL children
    /// </summary>
    public List<GraphNode> Children = [];

    /// <summary>
    /// Lists locked children, which:
    /// - have only 1 parent
    /// - have no children, or have only locked children
    /// - locked children necessarily need 
    /// </summary>
    public IEnumerable<GraphNode> LeafChildren => Children.Where(n => n.IsLeaf);

    /// <summary>
    /// Lists children nodes which are NOT locked children!
    /// </summary>
    public IEnumerable<GraphNode> NormalChildren => Children.Where(n => !n.IsLeaf);


    public override string ToString() => $@"{Id} [{Height}]";
}
