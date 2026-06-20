namespace SH.Content.Modding;

#pragma warning disable CA1069 // Enums values should not be duplicated
public enum EPatchOperation
{
    None = 000,

    // Node Operations

    AddNodeAsFirst = 001,
    NodeAddAsFirst = 001,
    AddNodeFirst = 001,
    NodeAddFirst = 001,
    AddFirst = 001,

    AddNodeAsLast = 002,
    NodeAddAsLast = 002,
    AddNodeLast = 002,
    NodeAddLast = 002,
    AddLast = 002,
    AddNode = 002,
    NodeAdd = 002,
    Add = 002,

    InsertNodeBefore = 003,
    NodeInsertBefore = 003,
    InsertBefore = 003,

    InsertNodeAfter = 004,
    NodeInsertAfter = 004,
    InsertAfter = 004,
    InsertNode = 004,
    NodeInsert = 004,
    Insert = 004,

    RemoveNode = 005,
    NodeRemove = 005,
    Remove = 005,

    ReplaceNode = 006,
    NodeReplace = 006,
    Replace = 006,

    // Attribute Operations

    SetAttribute = 101,
    AttributeSet = 101,

    AddAttribute = 102,
    AttributeAdd = 102,

    RemoveAttribute = 103,
    AttributeRemove = 103,

    MathAttribute = 104,
    AttributeMath = 104,
}
#pragma warning restore CA1069 // Enums values should not be duplicated

