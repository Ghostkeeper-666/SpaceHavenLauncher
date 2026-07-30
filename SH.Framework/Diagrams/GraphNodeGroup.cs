using SH.Framework.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SH.Framework.Diagrams;

/// <summary>
/// Node dependency group
/// </summary>
public sealed class GraphNodeGroup : IReadOnlyList<GraphNode>
{
    internal GraphNodeGroup(int id)
    {
        Id = id;
    }

    public int Id { get; internal set; }

    private readonly List<GraphNode> Nodes = [];
    public int Count => Nodes.Count;

    public GraphNode this[int index] => Nodes[index];

    public bool Contains(GraphNode node) => Nodes.Contains(node);

    public void Clear() => Nodes.Clear();

    public void Add(GraphNode node) => Nodes.Add(node);
    public void AddRange(IEnumerable<GraphNode> nodes) => Nodes.AddRange(nodes);

    public void Remove(GraphNode node) => Nodes.Remove(node);
    public void RemoveRange(IEnumerable<GraphNode> nodes)
    {
        foreach(GraphNode node in nodes)
            Nodes.AddRange(node);
    }

    public IEnumerator<GraphNode> GetEnumerator() => Nodes.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Nodes.GetEnumerator();

    public override string ToString() =>
        $@"{Id}: {{ {Nodes.Select(n => n.Id).JoinToString(", ")} }}";
}
