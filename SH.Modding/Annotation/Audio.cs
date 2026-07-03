using System.Xml.Linq;

namespace SH.Modding.Annotation;

internal sealed class Audio
{
    public XElement XML { get; set; }
    public string ID { get; set; }
    public string Name { get; set; }
}