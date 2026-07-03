using System.Collections.Generic;
using System.Xml.Linq;

namespace SH.Modding.Annotation;

internal sealed class Element
{
    public XElement XML { get; set; }
    public string MID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public SortedDictionary<string, Element> LinksTo { get; set; } = [];
    public SortedDictionary<string, Element> LinkedBy { get; set; } = [];
    public SortedDictionary<string, Element> LinksToAll { get; set; } = [];
    public SortedDictionary<string, Element> LinkedByAll { get; set; } = [];

    public void MapAllLinks(Element current = null)
    {
        if (current == this)
            return; // cyclic link
        if (current == null)
            current = this; // first call to this method
        else if (!LinksToAll.TryAdd(current.MID, current))
            return; // already added
        foreach (Element child in current.LinksTo.Values)
        {
            if (child == null)
                continue; // should never happen!
            MapAllLinks(child);
        }
    }

    public void MapAllLinkedBy(Element current = null)
    {
        if (current == this)
            return; // cyclic link
        if (current == null)
            current = this; // first call to this method
        else if (!LinkedByAll.TryAdd(current.MID, current))
            return; // already added
        foreach (Element child in current.LinkedBy.Values)
        {
            if (child == null)
                continue; // should never happen!
            MapAllLinkedBy(child);
        }
    }

    public override string ToString() => Name;
}
