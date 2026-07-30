//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace SH.Framework.Diagrams;

//public sealed class GraphLayoutCalculator<T> where T : class, IGraphNode<T>
//{
//    private sealed class GroupLayout
//    {
//        public int GroupId { get; init; }
//        public List<T> Nodes { get; init; }
//        public int MinRow { get; set; }
//        public int MaxRow { get; set; }
//    }

//    public int Iterations { get; set; } = 50;
//    public int GroupGap { get; set; } = 10;
//    public double ParentForce { get; set; } = 40.0;
//    public double ChildForce { get; set; } = 4.0;
//    public double LongEdgeWeight { get; set; } = 100.0;
//    public double CrossingPenalty { get; set; } = 100.0;

//    private sealed class GraphNode
//    {
//        public T Source { get; init; }
//        public List<GraphNode> Parents { get; init; }
//        public List<GraphNode> Children { get; init; }
//        public int Column { get; set; }
//        public int Row { get; set; }
//        // For real nodes >= 1; for dummy nodes also 1 (so they mark a row as “edge lane”).
//        public int RowSpan { get; set; } = 1;
//        public double Y { get; set; }
//        public double DesiredY { get; set; }
//        public bool IsDummy { get; init; }
//        public double ForceY { get; set; }
//        public GraphNode LockedParent { get; set; }
//        public bool IsLockedChild => LockedParent != null;
//    }

//    private sealed class Component
//    {
//        public List<GraphNode> Nodes { get; } = [];
//        public Dictionary<int, List<GraphNode>> Columns { get; } = [];
//        public int MaxColumn { get; set; }

//        public void Add(GraphNode node)
//        {
//            Nodes.Add(node);
//            if (!Columns.TryGetValue(node.Column, out List<GraphNode> column))
//            {
//                column = [];
//                Columns.Add(node.Column, column);
//            }
//            column.Add(node);
//            MaxColumn = Math.Max(MaxColumn, node.Column);
//        }
//    }

//    public void CalculateLayout(IEnumerable<T> nodes)
//    {
//        List<T> allNodes = nodes.ToList();
//        if (allNodes.Count == 0)
//            return;

//        foreach (T node in allNodes)
//        {
//            node.IsLockedChild = false;
//            node.LinkVerticalLine ??= new List<(T child, int afterColumn)>();
//            node.LinkVerticalLine.Clear();
//        }

//        List<List<T>> groups = FindGroups(allNodes);
//        List<GroupLayout> layouts = [];
//        int groupId = 0;

//        foreach (List<T> group in groups)
//        {
//            foreach (T node in group)
//                node.GroupId = groupId;

//            CalculateColumns(group);
//            List<GraphNode> expanded = ExpandGraph(group);
//            DetectLockedChildren(expanded);
//            Component component = CreateComponent(expanded);
//            LayoutComponent(component);

//            foreach (GraphNode node in component.Nodes)
//            {
//                if (node.Source == null)
//                    continue; // dummies have no Source
//                node.Source.Column = node.Column;
//                node.Source.Row = node.Row;
//            }

//            layouts.Add(new GroupLayout
//            {
//                GroupId = groupId,
//                Nodes = group,
//                MinRow = group.Min(n => n.Row),
//                MaxRow = group.Max(n => n.Row)
//            });

//            groupId++;
//        }

//        ApplyGroupOffsets(layouts);

//        RemoveEmptyRows(allNodes);

//        ComputeVerticalLineRouting(allNodes);
//    }

//    private void LayoutComponent(Component component)
//    {
//        InitializePositions(component);

//        for (int iteration = 0; iteration < Iterations; iteration++)
//        {
//            ClearForces(component);
//            CalculateDesiredPositions(component);
//            RelaxPositions(component);
//            CalculateCrossingForces(component);
//            ApplyForces(component);
//            SeparateNodes(component);
//        }

//        AssignRows(component);              // final integer Row for all nodes
//        ReserveLockedChildSpace(component); // make parents cover locked chains
//        SeparateNodes(component);           // final separation with reserved spans
//        AssignRows(component);              // sync Row to Y

//        // NEW: final pass to keep real nodes off dummy rows
//        EnforceDummyRowExclusion(component);
//        AssignRows(component);              // rows reflect the final Y
//    }

//    private static void ClearForces(Component component)
//    {
//        foreach (GraphNode node in component.Nodes)
//            node.ForceY = 0;
//    }

//    private void ApplyForces(Component component)
//    {
//        foreach (GraphNode node in component.Nodes)
//        {
//            if (node.IsDummy || node.IsLockedChild)
//                continue;
//            node.Y += node.ForceY * 0.001;
//        }
//    }

//    private static void InitializePositions(Component component)
//    {
//        foreach (List<GraphNode> columnNodes in component.Columns.Values)
//        {
//            List<GraphNode> allNodes = columnNodes.ToList();
//            allNodes.Sort((a, b) => a.Row.CompareTo(b.Row));

//            double position = 0;
//            foreach (GraphNode node in allNodes)
//            {
//                if (node.IsLockedChild)
//                    continue;
//                node.Y = position;
//                position += node.RowSpan;
//            }

//            foreach (GraphNode node in allNodes)
//            {
//                if (node.IsLockedChild && node.LockedParent != null)
//                    node.Y = node.LockedParent.Y + 1;
//            }
//        }
//    }

//    private static void AssignRows(Component component)
//    {
//        foreach (GraphNode node in component.Nodes)
//        {
//            if (!node.IsLockedChild)
//                node.Row = (int)Math.Round(node.Y);
//        }

//        foreach (GraphNode node in component.Nodes)
//        {
//            if (!node.IsLockedChild || node.LockedParent == null)
//                continue;

//            node.Column = node.LockedParent.Column;
//            node.Row = node.LockedParent.Row + 1;
//        }
//    }

//    /// Parents reserve vertical space for their locked chains.
//    private static void ReserveLockedChildSpace(Component component)
//    {
//        foreach (GraphNode parent in component.Nodes.Where(n => !n.IsDummy && !n.IsLockedChild))
//        {
//            int maxRow = parent.Row;

//            foreach (GraphNode child in parent.Children)
//            {
//                if (!child.IsLockedChild || child.Column != parent.Column)
//                    continue;

//                maxRow = Math.Max(maxRow, child.Row);

//                GraphNode current = child;
//                while (true)
//                {
//                    GraphNode next = current.Children.FirstOrDefault(
//                        c => c.IsLockedChild && c.LockedParent == current && c.Column == parent.Column);
//                    if (next == null)
//                        break;

//                    maxRow = Math.Max(maxRow, next.Row);
//                    current = next;
//                }
//            }

//            int span = maxRow - parent.Row + 1;
//            parent.RowSpan = Math.Max(parent.RowSpan, span);
//        }
//    }

//    private void ApplyGroupOffsets(List<GroupLayout> groups)
//    {
//        int currentRow = 0;
//        foreach (GroupLayout group in groups)
//        {
//            int height = group.MaxRow - group.MinRow + 1;
//            int offset = currentRow - group.MinRow;

//            foreach (T node in group.Nodes)
//                node.Row += offset;

//            currentRow += height + GroupGap;
//        }
//    }

//    /// Nodes with exactly 1 parent AND no children are locked children.
//    /// Locked nodes always share the parent's column.
//    private static void DetectLockedChildren(List<GraphNode> nodes)
//    {
//        foreach (GraphNode node in nodes)
//        {
//            if (node.IsDummy)
//                continue;

//            if (node.Parents.Count == 1 && node.Children.Count == 0)
//            {
//                GraphNode parent = node.Parents[0];
//                node.LockedParent = parent;
//                node.Column = parent.Column;

//                node.Source!.IsLockedChild = true;
//            }
//        }

//        foreach (GraphNode node in nodes)
//        {
//            if (!node.IsLockedChild)
//                continue;

//            GraphNode parent = node.LockedParent;
//            while (parent != null)
//            {
//                node.Column = parent.Column;
//                parent = parent.LockedParent;
//            }
//        }
//    }

//    private void CalculateDesiredPositions(Component component)
//    {
//        foreach (GraphNode node in component.Nodes)
//        {
//            if (node.IsDummy || node.IsLockedChild)
//                continue;

//            double sum = 0;
//            double weight = 0;

//            foreach (GraphNode parent in node.Parents)
//            {
//                if (parent.IsDummy || parent.IsLockedChild)
//                    continue;

//                int distance = Math.Abs(node.Column - parent.Column);
//                double force = ParentForce * Math.Max(1, distance * distance * LongEdgeWeight);
//                sum += parent.Y * force;
//                weight += force;
//            }

//            foreach (GraphNode child in node.Children)
//            {
//                if (child.IsDummy || child.IsLockedChild)
//                    continue;

//                int distance = Math.Abs(node.Column - child.Column);
//                double force = ChildForce * Math.Max(1, distance * distance * LongEdgeWeight);
//                sum += child.Y * force;
//                weight += force;
//            }

//            node.DesiredY = weight == 0 ? node.Y : sum / weight;
//        }
//    }

//    private void RelaxPositions(Component component)
//    {
//        foreach (GraphNode node in component.Nodes)
//        {
//            if (node.IsDummy || node.IsLockedChild)
//                continue;
//            node.Y += (node.DesiredY - node.Y) * 0.5;
//        }
//    }

//    private void CalculateCrossingForces(Component component)
//    {
//        for (int column = 0; column < component.MaxColumn; column++)
//        {
//            if (!component.Columns.TryGetValue(column, out List<GraphNode> leftColumn))
//                continue;

//            List<GraphNode> left = leftColumn.Where(n => !n.IsDummy).ToList();

//            if (!component.Columns.TryGetValue(column + 1, out List<GraphNode> rightColumn))
//                continue;

//            List<GraphNode> right = rightColumn.Where(n => !n.IsDummy).ToList();
//            if (left.Count == 0 || right.Count == 0)
//                continue;

//            foreach (GraphNode parentA in left)
//            {
//                if (parentA.IsLockedChild)
//                    continue;

//                foreach (GraphNode parentB in left)
//                {
//                    if (parentB.IsLockedChild)
//                        continue;
//                    if (ReferenceEquals(parentA, parentB) || parentA.Y >= parentB.Y)
//                        continue;

//                    foreach (GraphNode childA in parentA.Children.Where(c => !c.IsDummy))
//                    {
//                        if (childA.IsLockedChild)
//                            continue;

//                        foreach (GraphNode childB in parentB.Children.Where(c => !c.IsDummy))
//                        {
//                            if (childB.IsLockedChild)
//                                continue;
//                            if (childA.Column != childB.Column)
//                                continue;
//                            if (childA.Y <= childB.Y)
//                                continue;

//                            double force = CrossingPenalty *
//                                           Math.Max(1, Math.Abs(childA.Column - parentA.Column));
//                            childA.ForceY -= force;
//                            childB.ForceY += force;
//                        }
//                    }
//                }
//            }
//        }
//    }

//    /// Separation that respects RowSpan (real + dummies)
//    /// and keeps locked children immediately above their parent.
//    private void SeparateNodes(Component component)
//    {
//        foreach (List<GraphNode> columnNodes in component.Columns.Values)
//        {
//            List<GraphNode> allNodes = columnNodes.ToList();
//            allNodes.Sort((a, b) => a.Y.CompareTo(b.Y));

//            double current = 0;
//            foreach (GraphNode node in allNodes)
//            {
//                if (node.IsLockedChild)
//                    continue;

//                if (node.Y < current)
//                    node.Y = current;

//                current = node.Y + node.RowSpan;
//            }

//            foreach (GraphNode node in allNodes)
//            {
//                if (node.IsLockedChild && node.LockedParent != null)
//                {
//                    node.Column = node.LockedParent.Column;
//                    node.Y = node.LockedParent.Y + 1;
//                }
//            }
//        }
//    }

//    /// FINAL constraint: in each column, no real node may share a row with a dummy.
//    /// If it does, push the real node down to the next free row.
//    private static void EnforceDummyRowExclusion(Component component)
//    {
//        foreach (List<GraphNode> columnNodes in component.Columns.Values)
//        {
//            // Determine which rows are used by dummies (edge lanes).
//            HashSet<int> dummyRows = new();
//            foreach (GraphNode node in columnNodes)
//            {
//                if (node.IsDummy)
//                    dummyRows.Add((int)Math.Round(node.Y));
//            }

//            if (dummyRows.Count == 0)
//                continue;

//            // Move real nodes off dummy rows
//            List<GraphNode> realNodes = columnNodes.Where(n => !n.IsDummy).ToList();
//            realNodes.Sort((a, b) => a.Y.CompareTo(b.Y));

//            double current = 0;
//            foreach (GraphNode node in realNodes)
//            {
//                if (node.IsLockedChild)
//                    continue;

//                if (node.Y < current)
//                    node.Y = current;

//                int candidateRow = (int)Math.Round(node.Y);

//                // if candidateRow is a dummy row, push below until free
//                while (dummyRows.Contains(candidateRow))
//                {
//                    candidateRow++;
//                }

//                node.Y = candidateRow;
//                current = node.Y + node.RowSpan;
//            }

//            // Adjust locked children after pushing parents/free nodes
//            foreach (GraphNode node in realNodes)
//            {
//                if (node.IsLockedChild && node.LockedParent != null)
//                    node.Y = node.LockedParent.Y + 1;
//            }
//        }
//    }

//    private List<List<T>> FindGroups(List<T> nodes)
//    {
//        List<List<T>> groups = [];
//        HashSet<T> visited = [];

//        foreach (T node in nodes)
//        {
//            if (!visited.Add(node))
//                continue;

//            List<T> group = [];
//            Queue<T> queue = new();
//            queue.Enqueue(node);

//            while (queue.Count > 0)
//            {
//                T current = queue.Dequeue();
//                group.Add(current);

//                foreach (T related in current.Parents.Concat(current.Children))
//                    if (visited.Add(related))
//                        queue.Enqueue(related);
//            }

//            groups.Add(group);
//        }

//        return groups;
//    }

//    private Component CreateComponent(List<GraphNode> nodes)
//    {
//        Component component = new();
//        foreach (GraphNode node in nodes)
//            component.Add(node);
//        return component;
//    }

//    private List<GraphNode> ExpandGraph(List<T> nodes)
//    {
//        Dictionary<T, GraphNode> map = nodes.ToDictionary(
//            n => n,
//            n => new GraphNode
//            {
//                Source = n,
//                Parents = [],
//                Children = [],
//                Column = n.Column,
//                Row = n.Row,
//                RowSpan = 1,
//                IsDummy = false
//            });

//        List<GraphNode> result = map.Values.ToList();

//        foreach (T node in nodes)
//        {
//            GraphNode parent = map[node];

//            foreach (T childNode in node.Children)
//            {
//                GraphNode child = map[childNode];
//                GraphNode previous = parent;

//                for (int column = parent.Column + 1; column < child.Column; column++)
//                {
//                    GraphNode dummy = new()
//                    {
//                        Source = null,
//                        Parents = [],
//                        Children = [],
//                        Column = column,
//                        Row = parent.Row,
//                        RowSpan = 1,   // dummy marks a reserved horizontal lane cell
//                        IsDummy = true
//                    };
//                    previous.Children.Add(dummy);
//                    dummy.Parents.Add(previous);
//                    result.Add(dummy);
//                    previous = dummy;
//                }

//                previous.Children.Add(child);
//                child.Parents.Add(previous);
//            }
//        }

//        return result;
//    }

//    private void CalculateColumns(List<T> nodes)
//    {
//        Dictionary<T, int> depths = [];
//        HashSet<T> visiting = [];

//        foreach (T node in nodes)
//            node.Column = CalculateDepth(node, depths, visiting);
//    }

//    private int CalculateDepth(T node, Dictionary<T, int> depths, HashSet<T> visiting)
//    {
//        if (depths.TryGetValue(node, out int depth))
//            return depth;

//        if (!visiting.Add(node))
//            throw new InvalidOperationException("Circular dependency detected.");

//        depth = 0;
//        foreach (T parent in node.Parents)
//            depth = Math.Max(depth, CalculateDepth(parent, depths, visiting) + 1);

//        visiting.Remove(node);
//        depths[node] = depth;
//        return depth;
//    }

//    private void ComputeVerticalLineRouting(IEnumerable<T> nodes)
//    {
//        foreach (T node in nodes)
//        {
//            node.LinkVerticalLine ??= new List<(T child, int afterColumn)>();
//            node.LinkVerticalLine.Clear();
//        }

//        foreach (T parent in nodes)
//        {
//            foreach (T child in parent.Children)
//            {
//                if (child.IsLockedChild)
//                    continue;

//                int parentCol = parent.Column;
//                int parentRow = parent.Row;
//                int childCol = child.Column;
//                int childRow = child.Row;

//                // Same row: direct horizontal line, no S-shape routing info
//                if (parentRow == childRow)
//                    continue;

//                // Same column: pure vertical line; no S-shape routing info
//                if (parentCol == childCol)
//                    continue;

//                int afterColumn = parentCol; // vertical at parent's right edge

//                parent.LinkVerticalLine.Add((child, afterColumn));
//            }
//        }
//    }

//    private void RemoveEmptyRows(IEnumerable<T> nodes)
//    {
//        HashSet<int> usedRows = new();
//        foreach (T node in nodes)
//            usedRows.Add(node.Row);

//        List<int> sortedRows = usedRows.OrderBy(r => r).ToList();
//        Dictionary<int, int> rowMap = new();
//        int newRow = 0;

//        foreach (int oldRow in sortedRows)
//        {
//            rowMap[oldRow] = newRow;
//            newRow++;
//        }

//        foreach (T node in nodes)
//            node.Row = rowMap[node.Row];
//    }
//}