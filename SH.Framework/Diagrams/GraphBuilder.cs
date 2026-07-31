using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Framework.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace SH.Framework.Diagrams;


public sealed class GraphBuilder
{
    public GraphGrid Grid { get; }

    private readonly ILogger Log;

    public GraphBuilder(ILogger log)
    {
        Log = log ?? new VoidLogger();
        Grid = new();
    }

    public bool CalculateLayout(CancellationToken ct)
    {
        try
        {
            foreach (GraphNodeGroup group in Grid.Groups)
            {
                ct.ThrowIfCancellationRequested();
                for (int depth = 0; depth < Grid.ColumnCount; ++depth)
                {
                    List<GraphNode> selected = group.Where(n => n.TrunkDepth == depth && !n.IsLeaf).ToList();
                    foreach (GraphNode truncNode in selected)
                        AddToGraph(truncNode, depth);
                }
            }

            // Done.
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            return false;
        }
    }

    private void AddToGraph(GraphNode node, int depth)
    {
        GraphCell cell = Grid.AddBottomRow()[depth];
        cell.Node = node;
        node.Data.Cell = cell;
        foreach (GraphNode leafNode in node.LeafChildren)
            AddToGraph(leafNode, depth);
    }


    public bool Populate<T>(IEnumerable<T> dataNodes, CancellationToken ct) where T : class, IDataNode<T>
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dataNodes);

            // Create nodes:
            CreateNodes(dataNodes, ct);

            // Find leaf nodes:
            FindLeafNodes(ct);

            // Calculate node depth:
            CalculateDepth(ct);

            // Create node dependency groups:
            CreateGroups(ct);

            // Done.
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            return false;
        }
    }

    private void CreateGroups(CancellationToken ct)
    {
        int groupId = 0;
        List<GraphNodeGroup> groups = [];
        foreach (GraphNode node in Grid.Nodes.Values.Where(n => n.Parents.Count <= 0))
        {
            node.Group = new GraphNodeGroup(groupId++);
            node.Group.Add(node);
            groups.Add(node.Group);
        }

        List<GraphNodeGroup> merged = [];
        List<GraphNode> remainingNodes = Grid.Nodes.Values.Where(n => n.Group == null).ToList();
        do
        {
            merged.Clear();
            foreach (GraphNodeGroup group in groups)
            {
                ct.ThrowIfCancellationRequested();

                for (int i = 0; i < group.Count; ++i)
                {
                    GraphNode node = group[i];
                    foreach (GraphNode child in node.Children)
                    {
                        if (child.Group == group)
                            continue;

                        if (child.Group == null)
                        {
                            child.Group = group;
                            group.Add(child);
                            remainingNodes.Remove(child);
                            continue;
                        }

                        // Merge:
                        GraphNodeGroup otherGroup = child.Group;
                        group.AddRange(otherGroup);
                        foreach (GraphNode otherNode in otherGroup)
                            otherNode.Group = group;
                        otherGroup.Clear();
                        merged.Add(otherGroup);
                    }
                }
            }
            foreach (GraphNodeGroup group in merged)
                groups.Remove(group);
        }
        while (merged.Count > 0 || remainingNodes.Count > 0);

        groupId = 0;
        groups = groups.OrderBy(g => g.Count).ToList();
        foreach (GraphNodeGroup group in groups)
            group.Id = groupId++;
        Grid.Groups = groups;
    }

    private void FindLeafNodes(CancellationToken ct)
    {
        List<GraphNode> selected = Grid.Nodes.Values.Where(n => n.Children.Count <= 0).ToList(); // childless nodes
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

    private bool CalculateDepth(CancellationToken ct)
    {
        // Trunk depth (a leaf node has same trunk depth as its closest trunk ancestor):
        List<GraphNode> selected = Grid.Nodes.Values.Where(n => n.IsRoot).ToList(); // root nodes
        for (Grid.MaxTrunkDepth = 0; selected.Count > 0; ++Grid.MaxTrunkDepth)
        {
            ct.ThrowIfCancellationRequested();
            foreach (GraphNode node in selected)
                node.TrunkDepth = node.IsLeaf ? node.FirstParent.TrunkDepth : Grid.MaxTrunkDepth;
            selected = selected.SelectMany(n => n.Children).Distinct().ToList();
        }
        Grid.ColumnCount = Grid.MaxTrunkDepth;
        if (Grid.MaxTrunkDepth > 0)
            --Grid.MaxTrunkDepth;

        // Leaf depth (all root/trunk nodes have leaf depth = 0):
        selected = Grid.Nodes.Values.Where(n => n.IsTrunk && n.HasLeafChildren).ToList();
        for (Grid.MaxLeafDepth = 0; selected.Count > 0; ++Grid.MaxLeafDepth)
        {
            ct.ThrowIfCancellationRequested();
            foreach (GraphNode node in selected)
                node.LeafDepth = Grid.MaxLeafDepth;
            selected = selected.SelectMany(n => n.LeafChildren).ToList();
        }
        if (Grid.MaxLeafDepth > 0)
            --Grid.MaxLeafDepth;

        // Done.
        return true;
    }

    private void CreateNodes<T>(IEnumerable<T> dataNodes, CancellationToken ct) where T : class, IDataNode<T>
    {
        Grid.Nodes.Clear();
        List<T> remaining = dataNodes.OrderBy(n => n.Id).ToList();
        List<T> selected = remaining.Where(n => n.Dependencies == null || !n.Dependencies.Any()).ToList();
        while (selected.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            foreach (T dataNode in selected)
            {
                if (dataNode.Id.IsNullOrEmpty())
                    throw new ArgumentException($"Node has null or empty id", nameof(dataNodes));
                GraphNode graphNode = new(dataNode);
                if (!Grid.Nodes.TryAdd(graphNode.Id, graphNode))
                    throw new ArgumentException($"Node '{graphNode.Id}' is a duplicate", nameof(dataNodes));
                graphNode.Parents.AddRange(dataNode.Dependencies.Select(d => d.Id).Select(id => Grid.Nodes[id]));
                foreach (GraphNode parent in graphNode.Parents)
                    parent.Children.Add(graphNode);
                remaining.Remove(dataNode);
            }
            selected = remaining.Where(n => n.Dependencies.All(d => Grid.Nodes.ContainsKey(d.Id))).ToList();
        }
        if (remaining.Count > 0)
            throw new ArgumentException($"Circular references were found, check the following nodes: {remaining.Select(n => n.Id).JoinToString(", ")}", nameof(dataNodes));
    }
}
