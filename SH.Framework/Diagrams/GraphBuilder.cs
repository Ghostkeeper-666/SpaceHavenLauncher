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
    public SortedDictionary<string, GraphNode> AllNodes { get; internal set; } = [];
    public List<GraphGroup> Groups { get; internal set; } = [];


    private readonly ILogger Log;

    public GraphBuilder(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    public bool CalculateLayout(CancellationToken ct)
    {
        try
        {
            foreach (GraphGroup group in Groups.OrderByDescending(g => g.Count))
            {
                ct.ThrowIfCancellationRequested();
                group.ComputeLayout(ct);
            }

            // Trim Empty Rows:
            // TODO

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

            // Create node dependency groups:
            CreateGroups(ct);

            foreach (GraphGroup group in Groups)
            {
                // Find leaf nodes:
                group.FindLeafNodes(ct);

                // Calculate node depth:
                group.CalculateDepth(ct);
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

    private void CreateGroups(CancellationToken ct)
    {
        int groupId = 0;
        List<GraphGroup> groups = [];
        foreach (GraphNode node in AllNodes.Values.Where(n => n.Parents.Count <= 0))
        {
            node.Group = new GraphGroup(groupId++);
            node.Group.Add(node);
            groups.Add(node.Group);
        }

        List<GraphGroup> merged = [];
        List<GraphNode> remainingNodes = AllNodes.Values.Where(n => n.Group == null).ToList();
        do
        {
            merged.Clear();
            foreach (GraphGroup group in groups)
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
                        GraphGroup otherGroup = child.Group;
                        group.AddRange(otherGroup);
                        foreach (GraphNode otherNode in otherGroup)
                            otherNode.Group = group;
                        otherGroup.Clear();
                        merged.Add(otherGroup);
                    }
                }
            }
            foreach (GraphGroup group in merged)
                groups.Remove(group);
        }
        while (merged.Count > 0 || remainingNodes.Count > 0);

        groupId = 0;
        groups = groups.OrderBy(g => g.Count).ToList();
        foreach (GraphGroup group in groups)
            group.Id = groupId++;
        Groups = groups;
    }



    private void CreateNodes<T>(IEnumerable<T> dataNodes, CancellationToken ct) where T : class, IDataNode<T>
    {
        AllNodes.Clear();
        List<T> remainingDataNodes = dataNodes.OrderBy(n => n.Id).ToList();
        List<T> selectedDataNodes = remainingDataNodes.Where(n => n.Dependencies == null || !n.Dependencies.Any()).ToList();
        while (selectedDataNodes.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            foreach (T dataNode in selectedDataNodes)
            {
                if (dataNode.Id.IsNullOrEmpty())
                    throw new ArgumentException($"Node has null or empty id", nameof(dataNodes));
                GraphNode graphNode = new(dataNode);
                dataNode.GraphNode = graphNode;
                if (!AllNodes.TryAdd(graphNode.Id, graphNode))
                    throw new ArgumentException($"Node '{graphNode.Id}' is a duplicate", nameof(dataNodes));
                graphNode.Parents.AddRange(dataNode.Dependencies.Select(d => d.Id).Select(id => AllNodes[id]));
                foreach (GraphNode parent in graphNode.Parents)
                    parent.Children.Add(graphNode);
                remainingDataNodes.Remove(dataNode);
            }
            selectedDataNodes = remainingDataNodes.Where(n => n.Dependencies.All(d => AllNodes.ContainsKey(d.Id))).ToList();
        }
        if (remainingDataNodes.Count > 0)
            throw new ArgumentException($"Circular references were found, check the following nodes: {remainingDataNodes.Select(n => n.Id).JoinToString(", ")}", nameof(dataNodes));

        List<GraphNode> selected = AllNodes.Values.Where(n => n.IsRoot).ToList();
        selected = selected.SelectMany(n => n.Children).Distinct().ToList();
        while (selected.Count > 0)
        {
            foreach (GraphNode node in selected)
                node.Ancestors.AddRange(node.Parents.Where(n => !node.Ancestors.Contains(n)));
            selected = selected.SelectMany(n => n.Children).Distinct().ToList();
        }

        selected = AllNodes.Values.Where(n => !n.HasChildren).ToList();
        selected = selected.SelectMany(n => n.Parents).Distinct().ToList();
        while (selected.Count > 0)
        {
            foreach (GraphNode node in selected)
                node.Descendants.AddRange(node.Children.Where(n => !node.Descendants.Contains(n)));
            selected = selected.SelectMany(n => n.Parents).Distinct().ToList();
        }
    }


}
