namespace SH.Content.Modding;

public enum ENodePatchOperation
{
    AddNodeAsFirst = 001,
    AddNodeAsLast = 002,
    InsertNodeBefore = 003,
    InsertNodeAfter = 004,
    RemoveNode = 005,
    ReplaceNode = 006,
}

public enum EAttributePatchOperation
{
    SetAttribute = 101,
    AddAttribute = 102,
    RemoveAttribute = 103,
    MathAttribute = 104,
}

#pragma warning disable CA1069 // Enums values should not be duplicated
public enum EPatchOperation
{
    None = 000,

    // Node Operations Aliases

    AddNodeAsFirst = ENodePatchOperation.AddNodeAsFirst,
    NodeAddAsFirst = ENodePatchOperation.AddNodeAsFirst,
    AddNodeFirst = ENodePatchOperation.AddNodeAsFirst,
    NodeAddFirst = ENodePatchOperation.AddNodeAsFirst,
    AddFirst = ENodePatchOperation.AddNodeAsFirst,

    AddNodeAsLast = ENodePatchOperation.AddNodeAsLast,
    NodeAddAsLast = ENodePatchOperation.AddNodeAsLast,
    AddNodeLast = ENodePatchOperation.AddNodeAsLast,
    NodeAddLast = ENodePatchOperation.AddNodeAsLast,
    AddLast = ENodePatchOperation.AddNodeAsLast,
    AddNode = ENodePatchOperation.AddNodeAsLast,
    NodeAdd = ENodePatchOperation.AddNodeAsLast,
    Add = ENodePatchOperation.AddNodeAsLast,

    InsertNodeBefore = ENodePatchOperation.InsertNodeBefore,
    NodeInsertBefore = ENodePatchOperation.InsertNodeBefore,
    InsertBefore = ENodePatchOperation.InsertNodeBefore,

    InsertNodeAfter = ENodePatchOperation.InsertNodeAfter,
    NodeInsertAfter = ENodePatchOperation.InsertNodeAfter,
    InsertAfter = ENodePatchOperation.InsertNodeAfter,
    InsertNode = ENodePatchOperation.InsertNodeAfter,
    NodeInsert = ENodePatchOperation.InsertNodeAfter,
    Insert = ENodePatchOperation.InsertNodeAfter,

    RemoveNode = ENodePatchOperation.RemoveNode,
    NodeRemove = ENodePatchOperation.RemoveNode,
    Remove = ENodePatchOperation.RemoveNode,

    ReplaceNode = ENodePatchOperation.ReplaceNode,
    NodeReplace = ENodePatchOperation.ReplaceNode,
    Replace = ENodePatchOperation.ReplaceNode,

    // Attribute Operations Aliases

    SetAttribute = EAttributePatchOperation.SetAttribute,
    AttributeSet = EAttributePatchOperation.SetAttribute,

    AddAttribute = EAttributePatchOperation.AddAttribute,
    AttributeAdd = EAttributePatchOperation.AddAttribute,

    RemoveAttribute = EAttributePatchOperation.RemoveAttribute,
    AttributeRemove = EAttributePatchOperation.RemoveAttribute,

    MathAttribute = EAttributePatchOperation.MathAttribute,
    AttributeMath = EAttributePatchOperation.MathAttribute,
}
#pragma warning restore CA1069 // Enums values should not be duplicated

