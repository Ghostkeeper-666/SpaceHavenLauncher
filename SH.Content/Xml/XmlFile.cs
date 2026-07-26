using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
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

    public static EXmlFileType GetLibraryXmlFileType(string path) =>
        IOUtils.TextFileContains(path, "<Patch>", 4096, true, StringComparison.OrdinalIgnoreCase) ? EXmlFileType.Patch : // ignore case is OK
        IOUtils.TextFileContains(path, "<AllTexturesAndRegions>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Textures :
        IOUtils.TextFileContains(path, "<AllAnimations>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Animations :
        IOUtils.TextFileContains(path, "<data>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Haven :
        IOUtils.TextFileContains(path, "<audio>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Audio :
        IOUtils.TextFileContains(path, "<settings>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.SpaceHavenSettings :
        IOUtils.TextFileContains(path, "<t>", 4096, true, StringComparison.Ordinal) ? EXmlFileType.Texts :
        EXmlFileType.Unknown;

    public static EXmlFileType GetPatchXmlFileType(string path)
    {
        string filename = IOUtils.GetFileName(path);

        // Detect by filename:
        StringComparison ic = StringComparison.OrdinalIgnoreCase;

        EXmlFileType type =
            filename.StartsWith("haven", ic) ? EXmlFileType.Haven :
            filename.StartsWith("texts", ic) ? EXmlFileType.Texts :
            filename.StartsWith("audio", ic) ? EXmlFileType.Audio :
            filename.StartsWith("textures", ic) ? EXmlFileType.Textures :
            filename.StartsWith("animations", ic) ? EXmlFileType.Animations :
            filename.StartsWith("spacehavensettings", ic) ? EXmlFileType.SpaceHavenSettings :
            filename.StartsWith("settings", ic) ? EXmlFileType.SpaceHavenSettings :
            EXmlFileType.Unknown;

        if (type != EXmlFileType.Unknown)
            return type;

        // Detect by xpath content:
        ic = StringComparison.Ordinal;
        return
            IOUtils.TextFileContains(path, @"""/AllTexturesAndRegions", 4096, true, ic) ? EXmlFileType.Textures :
            IOUtils.TextFileContains(path, @"""/AllAnimations", 4096, true, ic) ? EXmlFileType.Animations :
            IOUtils.TextFileContains(path, @"""/data", 4096, true, ic) ? EXmlFileType.Haven :
            IOUtils.TextFileContains(path, @"""/audio", 4096, true, ic) ? EXmlFileType.Audio :
            IOUtils.TextFileContains(path, @"""/settings", 4096, true, ic) ? EXmlFileType.SpaceHavenSettings :
            IOUtils.TextFileContains(path, @"""/t", 4096, true, ic) ? EXmlFileType.Texts :
            EXmlFileType.Unknown;
    }



    public XmlFile(EXmlFileType type, string baseDir, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Type = type;
        Path = path.AsOSPath();
        BaseDir = baseDir.AsOSPath();
    }

    public EXmlFileType Type { get; }
    public EXmlFileType PatchType { get; set; } = EXmlFileType.Unknown;
    public string Path { get; set; }
    public string BaseDir { get; set; }
    public string RelativePath => Path.RemovePrefix(BaseDir).TrimStart('/', '\\');

    public string FileName => Path.GetFileName();

    public XDocument Xml { get; internal set; }

    public XElement Root => Xml.Root;

    public bool IsIgnored =>
        Xml.Root?.Attribute(ATTRIBUTE_IGNORE)?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    public XElement GetParentNode(NodeType nodeType) =>
        Xml.XPathSelectElements(nodeType.ParentXPath)?.FirstOrDefault();
    public IEnumerable<XElement> GetNodes(NodeType nodeType) =>
        Xml.XPathSelectElements(nodeType.XPath) ?? [];

    public bool TrySetXmlContent(string xml, ILogger log)
    {
        xml = IOUtils.EraseXmlDeclaration(xml);
        XDocument x = XDocument.Parse(xml, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
        if (x == null)
        {
            log?.Error($@"Unable to parse new XML content");
            return false;
        }
        Xml = x;
        return true;
    }

    public async Task<bool> TryReparse(ILogger log, CancellationToken ct)
    {
        XDocument reparsed = await IOUtils.TryReparseAsync(Xml, null, log, ct);
        if (reparsed == null)
            return false;
        Xml = reparsed;
        return true;
    }

    public async Task<bool> TrySaveAsync(ILogger log, CancellationToken ct) =>
        await IOUtils.TrySaveXDocumentAsync(Path, Xml, null, log, ct);

    public async Task<bool> TrySaveToAsync(string path, ILogger log, CancellationToken ct) =>
        await IOUtils.TrySaveXDocumentAsync(path, Xml, null, log, ct);

    public bool TryRunXPath(string xpath, out List<XElement> targetNodes, ILogger log)
    {
        targetNodes = [];
        try
        {
            targetNodes.AddRange(Xml.XPathSelectElements(xpath) ?? []);
            return true;
        }
        catch (Exception ex)
        {
            log?.Error(ex, Path);
            return false;
        }
    }

    public async Task<bool> TryLoadAsync(ILogger log, CancellationToken ct)
    {
        try
        {
            log?.Debug($@"Loading XML document: ""{Path}""", Path);

            // Workaround for removing syntax errors from XML documents:
            if (Type == EXmlFileType.Texts)
                await TrySanitizeXmlDocument(log, ct);

            Xml = await IOUtils.TryLoadXDocumentAsync(Path, log, ct);
            return Xml != null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex, Path);
            return false;
        }
    }

    private async Task<bool> TrySanitizeXmlDocument(ILogger log, CancellationToken ct)
    {
        try
        {
            log?.Debug($@"Sanitizing XML document: ""{Path}""");

            string dirty = await IOUtils.TryReadAllTextAsync(Path, log, ct);
            string sanitized = FixAmpersandAndInvalidCharacters(dirty, new char[] { (char)0x1B });
            return await IOUtils.TryWriteAllTextAsync(Path, sanitized, log, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error(ex, Path);
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
