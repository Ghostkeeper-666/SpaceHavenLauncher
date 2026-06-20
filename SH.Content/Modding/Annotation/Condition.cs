using System.Xml.Linq;

namespace SH.Content.Modding.Annotation;

internal sealed class Craft
{
    public XElement XML { get; set; }
    public string CID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}