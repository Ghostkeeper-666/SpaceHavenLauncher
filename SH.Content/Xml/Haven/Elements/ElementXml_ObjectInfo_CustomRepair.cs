using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_ObjectInfo_CustomRepair
{
    public int RepairGroupId { get; }
    public int BuildTools { get; }
    public OrderedDictionary<int, int> Materials { get; } // Id to QTY
}