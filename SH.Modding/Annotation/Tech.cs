using System.Xml.Linq;

namespace SH.Modding.Annotation;

internal sealed class Tech
{
    public XElement XML { get; set; }
    public string ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public override string ToString() => Name;
}
