using System.Xml.Linq;

namespace SH.Modding.Annotation;

internal sealed class Condition
{
    public XElement XML { get; set; }
    public string ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}