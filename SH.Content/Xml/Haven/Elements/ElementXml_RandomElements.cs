using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_RandomElements
{
    public double ChanceToPickOne { get; }
    public bool IsIndependentObject { get; }
    public List<ElementXml_ElementReference> RandomElements { get; } = [];
}
