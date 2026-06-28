using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SH.Content.Xml;

public sealed class XmlFile
{
    public static readonly string ATTRIBUTE_IGNORE = "ignore";

    public static bool TryGetLibraryXmlFileType(string path, out EXmlFileType type) =>
        EXmlFileType.Unknown != (type =
        IOUtils.TextFileContains(path, "<Patch>", 4096, true, StringComparison.OrdinalIgnoreCase) ? EXmlFileType.Patch : // ignore case is OK
        IOUtils.TextFileContains(path, "<AllTexturesAndRegions>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Textures :
        IOUtils.TextFileContains(path, "<AllAnimations>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Animations :
        IOUtils.TextFileContains(path, "<data>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Haven :
        IOUtils.TextFileContains(path, "<audio>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Audio :
        IOUtils.TextFileContains(path, "<settings>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.SpaceHavenSettings :
        IOUtils.TextFileContains(path, "<t>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Texts :
        EXmlFileType.Unknown);

    public static bool TryGetPatchXmlFileType(XmlFile patchXmlFile, out EXmlFileType type)
    {
        type = EXmlFileType.Unknown;

        // Detect by filename:
        StringComparison ic = StringComparison.OrdinalIgnoreCase;
        type =
            patchXmlFile.FileName.StartsWith("haven", ic) ? EXmlFileType.Haven :
            patchXmlFile.FileName.StartsWith("texts", ic) ? EXmlFileType.Texts :
            patchXmlFile.FileName.StartsWith("audio", ic) ? EXmlFileType.Audio :
            patchXmlFile.FileName.StartsWith("textures", ic) ? EXmlFileType.Textures :
            patchXmlFile.FileName.StartsWith("animations", ic) ? EXmlFileType.Animations :
            patchXmlFile.FileName.StartsWith("spacehavensettings", ic) ? EXmlFileType.SpaceHavenSettings :
            patchXmlFile.FileName.StartsWith("settings", ic) ? EXmlFileType.SpaceHavenSettings :
            EXmlFileType.Unknown;

        if (type != EXmlFileType.Unknown)
            return true;

        // Detect by xpath content:
        IEnumerable<XElement> elements = patchXmlFile?.Root?.Nodes()?.Select(n => n as XElement);
        foreach (XElement patchNode in elements)
        {
            string xpath = patchNode?.Element("xpath")?.Value;
            if (xpath.IsNullOrWhiteSpace())
                continue;

            type =
                xpath.StartsWith("/AllTexturesAndRegions") ? EXmlFileType.Textures :
                xpath.StartsWith("/AllAnimations") ? EXmlFileType.Animations :
                xpath.StartsWith("/data") ? EXmlFileType.Haven :
                xpath.StartsWith("/audio") ? EXmlFileType.Audio :
                xpath.StartsWith("/settings") ? EXmlFileType.SpaceHavenSettings :
                xpath.StartsWith("/t") ? EXmlFileType.Texts :
                EXmlFileType.Unknown;

            if (type != EXmlFileType.Unknown)
                return true;
        }

        // Everything failed...
        return false;
    }



    public XmlFile(EXmlFileType type, string baseDir, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Type = type;
        Path = path.AsOSPath();
        BaseDir = baseDir.AsOSPath();
    }

    public EXmlFileType Type { get; }
    public string Path { get; set; }
    public string BaseDir { get; set; }
    public string RelativePath => Path.RemovePrefix(BaseDir).TrimStart('/' ,'\\');

    public string FileName => System.IO.Path.GetFileName(Path);

    public XDocument Xml { get; internal set; }

    public XElement Root => Xml.Root;

    public bool IsIgnored =>
        Xml.Root?.Attribute(ATTRIBUTE_IGNORE)?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    public XElement GetParentNode(NodeType nodeType) =>
        Xml.XPathSelectElements(nodeType.ParentXPath)?.FirstOrDefault();
    public IEnumerable<XElement> GetNodes(NodeType nodeType) =>
        Xml.XPathSelectElements(nodeType.XPath) ?? [];

    public async Task<bool> TrySaveAsync(ILogger logger, CancellationToken ct) =>
        await IOUtils.TrySaveXDocumentAsync(Path, Xml, logger, ct);

    public async Task<bool> TrySaveToAsync(string alternativePath, ILogger logger, CancellationToken ct) =>
        await IOUtils.TrySaveXDocumentAsync(alternativePath, Xml, logger, ct);

    public bool TryRunXPath(string xpath, out List<XElement> targetNodes, ILogger logger)
    {
        targetNodes = [];
        try
        {
            targetNodes.AddRange(Xml.XPathSelectElements(xpath) ?? []);
            return true;
        }
        catch (Exception ex)
        {
            logger?.Error(ex, Path);
            return false;
        }
    }

    public async Task<bool> TryLoadAsync(ILogger logger, CancellationToken ct)
    {
        try
        {
            logger?.Debug($@"Loading XML document: ""{Path}""", Path);

            // Workaround for removing syntax errors from XML documents:
            if (Type == EXmlFileType.Texts)
                await TrySanitizeXmlDocument(logger, ct);

            Xml = await IOUtils.TryLoadXDocumentAsync(Path, logger, ct);
            return Xml != null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error(ex, Path);
            return false;
        }
    }

    private async Task<bool> TrySanitizeXmlDocument(ILogger logger, CancellationToken ct)
    {
        try
        {
            logger?.Debug($@"Sanitizing XML document: ""{Path}""");

            string dirty = File.ReadAllText(Path);
            string sanitized = FixAmpersandAndInvalidCharacters(dirty, new char[] { (char)0x1B });
            return await IOUtils.TryWriteAllTextAsync(Path, sanitized, logger, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error(ex, Path);
            return false;
        }
    }
    private static string FixAmpersandAndInvalidCharacters(string input, IEnumerable<char> charsToSkip)
    {
        int begin = 0;
        StringBuilder sb = new();
        HashSet<char> skipSet = new(charsToSkip);
        ReadOnlySpan<char> span = input.AsSpan();

        for (int i = 0; i < span.Length; i++)
        {
            char ch = span[i];
            if (ch == '&')
            {
                if (!IsInvalidAmpersand(span, i))
                    continue;

                // Replace with valid ampersand
                sb.Append(span.Slice(begin, i - begin));
                sb.Append("&amp;");
                begin = i + 1;
                continue;
            }
            else if (skipSet.Contains(ch))
            {
                // Skip character
                sb.Append(span.Slice(begin, i - begin));
                begin = i + 1;
                continue;
            }
        }

        // Nothing invalid found
        if (begin == 0)
            return input;

        // Copy remaining part
        sb.Append(span.Slice(begin));

        // Done.
        return sb.ToString();
    }

    private static bool IsInvalidAmpersand(ReadOnlySpan<char> xml, int index)
    {
        ReadOnlySpan<char> rest = xml[(index + 1)..];
        return
            rest.IsEmpty ||
            !rest.StartsWith("amp;") &&
            !rest.StartsWith("lt;") &&
            !rest.StartsWith("gt;") &&
            !rest.StartsWith("quot;") &&
            !rest.StartsWith("apos;") &&
            !rest.StartsWith("#");
    }


    public override string ToString() => Path;

}
