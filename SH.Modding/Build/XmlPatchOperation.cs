using SH.Content.Xml;
using SH.Framework;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SH.Modding.Build;

internal sealed class XmlPatchOperation
{
    internal static readonly string OPERATION = "Operation";
    internal static readonly string VALUE = "value";
    internal static readonly string ATTRIBUTE = "attribute";
    internal static readonly string MATH_OPTYPE = "opType";

    public static IReadOnlyList<EPatchOperation> NodeOperations { get; } =
    [
        EPatchOperation.AddNodeAsFirst,
        EPatchOperation.AddNodeAsLast,
        EPatchOperation.InsertNodeBefore,
        EPatchOperation.InsertNodeAfter,
        EPatchOperation.RemoveNode,
        EPatchOperation.ReplaceNode,
    ];

    public static IReadOnlyList<EPatchOperation> AttributeOperations { get; } =
    [
        EPatchOperation.SetAttribute,
        EPatchOperation.AddAttribute,
        EPatchOperation.RemoveAttribute,
        EPatchOperation.MathAttribute,
    ];

    public static bool TryCreate(XmlFile modXmlFile, IReadOnlyDictionary<string, Var> modVariables, XElement patchNode, out XmlPatchOperation patch, ILogger log)
    {
        try
        {
            patch = new();
            patch.ModXmlFile = modXmlFile ?? throw new ArgumentNullException(nameof(modXmlFile));
            patch.PatchNode = patchNode ?? throw new ArgumentNullException(nameof(patchNode));
            patch.Variables = modVariables ?? throw new ArgumentNullException(nameof(modVariables));
            string operationStr = patchNode.Attribute("Class")?.Value ?? patchNode.Attribute("type")?.Value ?? string.Empty;
            patch.XPath = patchNode.Element("xpath")?.Value?.Trim() ?? string.Empty;
            patch.PrettyName = $@"Patch Operation [op={operationStr} xpath='{patch.XPath}' file=""{modXmlFile.Path.GetFileName()}"" line={patchNode.Line()}]";

            // node name validation:
            string name = patchNode.Name?.LocalName ?? string.Empty;
            if (!name.Equals(OPERATION, StringComparison.Ordinal))
            {
                log?.Warn($@"Ingoring patch node with INVALID NAME = '{name}', it must be '{OPERATION}' => {patch.PrettyName}", modXmlFile.Path);
                patch = null;
                return false;
            }

            // patch operation validation:
            if (!Enum.TryParse(operationStr.Trim(), out EPatchOperation operation) || operation == EPatchOperation.None)
            {
                log?.Error($"Unknown PATCH OPERATION => {patch.PrettyName} \nThe supported patch operations are: \n{PatchOperationNames.JoinToString(", ")}", modXmlFile.Path);
                patch = null;
                return false;
            }
            patch.Operation = operation;

            // xpath validation:
            if (patch.XPath.IsNullOrWhiteSpace())
            {
                log?.Error($@"Missing XPATH in patch operation. {patch.PrettyName}", modXmlFile.Path);
                patch = null;
                return false;
            }

            // attribute operation validation:
            if (AttributeOperations.Contains(patch.Operation))
            {
                patch.Attribute = patchNode.Element(ATTRIBUTE)?.Value;
                if (patch.Attribute.IsNullOrWhiteSpace())
                {
                    log?.Error($@"Missing '{ATTRIBUTE}' information in patch operation. {patch.PrettyName}", modXmlFile.Path);
                    patch = null;
                    return false;
                }
            }

            // enable/disable?
            string enable = (patchNode.Element("enable")?.Value ?? patchNode.Element("enabled")?.Value)?.Trim();
            string disable = (patchNode.Element("disable")?.Value ?? patchNode.Element("disabled")?.Value)?.Trim();
            patch.IsEnabled = IsPatchNodeEnabled(enable, disable);

            // Done.
            return true;
        }
        catch (Exception ex)
        {
            log?.Error($@"Exception occurred while parsing the patch operation, file=""{modXmlFile.Path.GetFileName()}"" line={patchNode.Line()} : {ex}", modXmlFile.Path);
            patch = null;
            return false;
        }
    }

    private static bool IsPatchNodeEnabled(string enable, string disable)
    {
        if (enable != null && MathHelpers.TryConvertToBool(enable, out bool positiveValue))
            return positiveValue;
        if (disable != null && MathHelpers.TryConvertToBool(disable, out bool negativeValue))
            return !negativeValue;
        return true;
    }

    public static IEnumerable<string> PatchOperationNames =>
        Enum.GetNames<EPatchOperation>().Where(n => n != EPatchOperation.None.ToString());


    private XmlPatchOperation() { }

    public XmlFile ModXmlFile { get; private set; }
    public XElement PatchNode { get; private set; }
    public IReadOnlyDictionary<string, Var> Variables { get; private set; }
    public EPatchOperation Operation { get; private set; }
    public bool IsEnabled { get; private set; }
    public string XPath { get; private set; }
    public string Attribute { get; private set; }
    public string PrettyName { get; private set; }
    public bool IsNodePatchOperation => NodeOperations.Contains(Operation);
    public bool IsAttributePatchOperation => AttributeOperations.Contains(Operation);

    public bool TryRun(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        try
        {
            switch (Operation)
            {
                // Node operation:

                case EPatchOperation.AddNodeAsFirst:
                    return AddNodesAsFirstChildren(targetNodes, log);

                case EPatchOperation.AddNodeAsLast:
                    return AddNodesAsLastChildren(targetNodes, log);

                case EPatchOperation.InsertNodeBefore:
                    return InsertNodesBeforeSelf(targetNodes, log);

                case EPatchOperation.InsertNodeAfter:
                    return InsertNodesAfterSelf(targetNodes, log);

                case EPatchOperation.RemoveNode:
                    return RemoveNode(targetNodes, log);

                case EPatchOperation.ReplaceNode:
                    return ReplaceNode(targetNodes, log);

                // Attribute operation:

                case EPatchOperation.SetAttribute:
                    return SetAttribute(targetNodes, log);

                case EPatchOperation.AddAttribute:
                    return AddAttribute(targetNodes, log);

                case EPatchOperation.RemoveAttribute:
                    return RemoveAttribute(targetNodes, log);

                case EPatchOperation.MathAttribute:
                    return MathAttribute(targetNodes, log);

                // Invalid operation:

                default:
                    throw new InvalidCastException($"{nameof(EPatchOperation)} = {Operation}");
            }
        }
        catch (Exception ex)
        {
            log?.Error(ex);
            return false;
        }
    }

    private bool AddNodesAsFirstChildren(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        List<XElement> nodes = PatchNode.Element(VALUE)?.Nodes()?.Where(n => n is XElement).Cast<XElement>().ToList();
        if (nodes.Count <= 0)
        {
            log?.Error($@"Patch operation does not contain nodes. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        foreach (XElement targetNode in targetNodes)
            targetNode.AddFirst(nodes.CloneXElements());
        return true;
    }

    private bool AddNodesAsLastChildren(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        List<XElement> nodes = PatchNode.Element(VALUE)?.Nodes()?.Where(n => n is XElement).Cast<XElement>().ToList();
        if (nodes.Count <= 0)
        {
            log?.Error($@"Patch operation does not contain nodes in its <{VALUE}> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        foreach (XElement targetNode in targetNodes)
            targetNode.Add(nodes.CloneXElements());
        return true;
    }

    private bool InsertNodesBeforeSelf(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        List<XElement> nodes = PatchNode.Element(VALUE)?.Nodes()?.Where(n => n is XElement)?.Cast<XElement>()?.ToList() ?? [];
        if (nodes.Count <= 0)
        {
            log?.Error($@"Patch operation does not contain nodes in its <{VALUE}> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        foreach (XElement targetNode in targetNodes)
            targetNode.AddBeforeSelf(nodes.CloneXElements());
        return true;
    }

    private bool InsertNodesAfterSelf(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        List<XElement> nodes = PatchNode.Element(VALUE)?.Nodes()?.Where(n => n is XElement)?.Cast<XElement>()?.ToList() ?? [];
        if (nodes.Count <= 0)
        {
            log?.Error($@"Patch operation does not contain nodes in its <{VALUE}> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        foreach (XElement targetNode in targetNodes)
            targetNode.AddAfterSelf(nodes.CloneXElements());
        return true;
    }

    private bool RemoveNode(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        foreach (XElement targetNode in targetNodes)
            targetNode.Remove();
        return true;
    }

    private bool ReplaceNode(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        List<XElement> nodes = PatchNode.Element(VALUE)?.Nodes()?.Where(n => n is XElement)?.Cast<XElement>()?.ToList() ?? [];
        if (nodes.Count <= 0)
        {
            log?.Error($@"Patch operation does not contain nodes in its <{VALUE}> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        foreach (XElement targetNode in targetNodes)
        {
            targetNode.AddAfterSelf(nodes.CloneXElements());
            targetNode.Remove();
        }
        return true;
    }

    private bool SetAttribute(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        string value = PatchNode.Element(VALUE)?.Value;
        if (value == null)
        {
            log?.Error($@"Patch operation does not have content in its <{VALUE}> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        foreach (XElement targetNode in targetNodes)
            targetNode.SetAttributeValue(Attribute, value);
        return true;
    }

    private bool AddAttribute(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        string value = PatchNode.Element("value")?.Value;
        if (value == null)
        {
            log?.Error($@"Patch operation does not have content in its <{VALUE}> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        foreach (XElement targetNode in targetNodes)
            targetNode.SetAttributeValue(Attribute, value);
        return true;
    }

    private bool RemoveAttribute(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        foreach (XElement targetNode in targetNodes)
            targetNode.SetAttributeValue(Attribute, null);
        return true;
    }

    private bool MathAttribute(IReadOnlyList<XElement> targetNodes, ILogger log)
    {
        XElement valueNode = PatchNode.Element("value");
        if (valueNode == null)
        {
            log?.Error($@"Patch operation does not have a <value> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        string opStr = valueNode.Attribute(MATH_OPTYPE)?.Value?.Trim();
        if (opStr.IsNullOrWhiteSpace())
        {
            log?.Error($@"Patch operation does not have the required '{MATH_OPTYPE}' attribute in the <{VALUE}> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }
        if (!Enum.TryParse(opStr, true, out EXmlPatchAttributeMathOperator op))
        {
            log?.Error($@"Unknown math operation '{opStr}' defined by attribute '{MATH_OPTYPE}'. {PrettyName}", ModXmlFile.Path);
            return false;
        }

        string strValue = valueNode.Value?.Trim();
        if (strValue.IsNullOrWhiteSpace() || !decimal.TryParse(strValue, out decimal doubleValue))
        {
            log?.Error($@"Patch operation does not have a numeric content in the <{VALUE}> node. {PrettyName}", ModXmlFile.Path);
            return false;
        }

        foreach (XElement targetNode in targetNodes)
        {
            string strTargetValue = targetNode.Attribute(Attribute)?.Value?.Trim();
            if (strTargetValue.IsNullOrWhiteSpace() || !decimal.TryParse(strTargetValue, out decimal doubleTargetValue))
            {
                log?.Warn($@"The target node '{targetNode.GetNodePath()}' does not have a numeric attribute '{Attribute}' for the math operation. This could be an error in the mod. {PrettyName}", ModXmlFile.Path);
                continue;
            }

            decimal result;
            switch (op)
            {
                case EXmlPatchAttributeMathOperator.Add:
                    result = doubleTargetValue + doubleValue;
                    break;
                case EXmlPatchAttributeMathOperator.Subtract:
                    result = doubleTargetValue - doubleValue;
                    break;
                case EXmlPatchAttributeMathOperator.Multiply:
                    result = doubleTargetValue * doubleValue;
                    break;
                case EXmlPatchAttributeMathOperator.Divide:
                    result = doubleTargetValue / doubleValue;
                    break;
                default:
                    throw new NotFiniteNumberException($"{nameof(EXmlPatchAttributeMathOperator)} = {op}");
            }
            string strResult = strTargetValue.Contains('.') ? $"{result.ToString("0.0")}f" : result.ToString("0");
            targetNode.SetAttributeValue(Attribute, strResult);
        }
        return true;
    }

    public override string ToString() => PrettyName;
}
