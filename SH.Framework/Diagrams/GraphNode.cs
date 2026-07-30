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

    public int Depth =>
        Parents.Count <= 0 ? 1 : 1 + Parents.Max(p => p.Depth);

    /// <summary>
    /// Is this node a locked child?
    /// </summary>
    public bool IsLocked { get; internal set; }

    /// <summary>
    /// The topmost cell
    /// </summary>
    public GraphCell RootCell { get; internal set; }
    public List<GraphCell> AllCells { get; } = [];

    /// <summary>
    /// Node dependency group
    /// </summary>
    public GraphNodeGroup Group { get; internal set; }

    /// <summary>
    /// Parent nodes
    /// </summary>
    public List<GraphNode> Parents = [];

    public GraphNode LockedParent => Parents[0];

    /// <summary>
    /// Root ancestor of a locked child node
    /// </summary>
    public GraphNode LockedRoot
    {
        get
        {
            if (!IsLocked)
                throw new ArgumentException();
            GraphNode rootAncestor = this;
            while (rootAncestor.LockedParent != null)
                rootAncestor = rootAncestor.LockedParent;
            return rootAncestor;
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
    public IEnumerable<GraphNode> LockedChildren => Children.Where(n => n.IsLocked);

    /// <summary>
    /// Lists children nodes which are NOT locked children!
    /// </summary>
    public IEnumerable<GraphNode> NormalChildren => Children.Where(n => !n.IsLocked);


    public override string ToString() => $@"{Id} [{Height}]";
}
