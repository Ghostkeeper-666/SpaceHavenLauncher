using System.Collections.Generic;
using System.Xml.Linq;

namespace SH.Content.Modding.Annotation;

internal sealed class Product
{
    public XElement XML { get; set; }
    public EProductType Type { get; set; }
    public string EID { get; set; }
    public string Item { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Inputs { get; set; } = [];
    public List<string> Outputs { get; set; } = [];

    public override string ToString() => Name;
}

internal enum EProductType
{
    Elementary,
    Process,
}
