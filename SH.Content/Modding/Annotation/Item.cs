using System.Xml.Linq;

namespace SH.Content.Modding.Annotation;

internal sealed class Item
{
    public XElement XML { get; set; }
    public string MID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string DuplicateMID { get; set; }
    public string Quality { get; set; }
    public Item Duplicate { get; set; }

    public override string ToString() => Name;
}
