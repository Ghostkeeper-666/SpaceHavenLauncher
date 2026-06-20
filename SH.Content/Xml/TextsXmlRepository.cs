using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Content.Enums;
using SH.Content.Xml.Texts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Content.Xml;

public sealed class TextsXmlRepository
{
    public OrderedDictionary<int, TextXml> ById { get; } = [];

    private readonly ELanguage[] Languages = Enum.GetValues<ELanguage>().ToArray();

    private readonly ILogger Log;

    public TextsXmlRepository(ILogger logger)
    {
        Log = logger ?? new VoidLogger();
    }

    public async Task<bool> TryReadAsync(string textsXmlPath, CancellationToken ct, IProgressInfo progress)
    {
        ById.Clear();
        try
        {
            string dirty = await IOUtils.TryReadAllTextAsync(textsXmlPath, Log, ct);
            if(dirty == null)
                return false;
            string sanitized = FixAmpersandAndInvalidCharacters(dirty, new char[] { (char)0x1B });
            XDocument doc = XDocument.Parse(sanitized);
            XElement root = doc?.Element("t") ?? throw new Exception("Invalid XML root");
            List<XElement> texts = root?.Elements("t")?.ToList() ?? [];

            // Generate a progress update only 100 times:
            int pi = 0;
            double p = 0.0;
            double delta = 100.0 / texts.Count;

            foreach (XElement t in texts)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    TextXml text = new()
                    {
                        Id = Convert.ToInt32(t.Attribute("id").Value),
                        PID = Convert.ToInt32(t.Attribute("pid")?.Value ?? "0"), // null should be an error!
                    };

                    foreach (ELanguage lang in Languages)
                        text.Value.Add(t.Element(lang.ToString())?.Value);

                    ById[text.Id] = text;
                }
                finally
                {
                    // Generate a progress update only 100 times:
                    if (pi < (int)(p += delta)) progress?.SetNormalized((pi = (int)p) / 100.0);
                }
            }

            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
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
}
