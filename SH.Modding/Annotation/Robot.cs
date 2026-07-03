using System.Xml.Linq;

namespace SH.Modding.Annotation;

internal sealed class Robot
{
    public XElement XML { get; set; }
    public string CID { get; set; }
    public string Name { get; set; }
}