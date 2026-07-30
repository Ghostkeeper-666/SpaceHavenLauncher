using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Framework.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
                    List<GraphNode> selected = group.Where(n => n.Depth == depth && !n.IsLeaf).ToList();
                    foreach (GraphNode truncNode in selected)
                    {
                        Grid.AddBottomRow()[depth].Node = truncNode;
                        foreach (GraphNode leafNode in truncNode.LeafChildren)
                            Grid.AddBottomRow()[depth].Node = leafNode;
                    }
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
                        // Merge groups:
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
        List<GraphNode> parents;
        List<GraphNode> selected = Grid.Nodes.Values.Where(n => n.Children.Count <= 0).ToList();
        while (selected.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            parents = [];

            foreach (GraphNode node in selected)
            {
                if (node.Parents.Count == 1 && node.Children.All(ch => ch.IsLeaf))
                {
                    node.IsLeaf = true;
                    parents.Add(node.FirstParent);
                }
            }

            // Update height of parents:
            foreach (GraphNode parent in parents)
                parent.Height = 1 + parent.LeafChildren.Sum(ch => ch.Height);

            // Check whether parents are locked children of their parents:
            selected = parents;
        }
    }

    private List<GraphNode> CalculateDepth(CancellationToken ct)
    {
        foreach (GraphNode node in Grid.Nodes.Values)
            node.Depth = -1;

        int depth = 0;
        List<GraphNode> selected = Grid.Nodes.Values.Where(n => n.IsRoot).ToList();
        while (selected.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            foreach (GraphNode node in selected)
                node.Depth = node.IsLeaf ? depth - 1 : depth;
            selected = Grid.Nodes.Values.Where(n => n.Depth < 0 && n.Parents.All(p => p.Depth >= 0)).ToList();
            ++depth;
        }
        Grid.ColumnCount = depth;
        return selected;
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
