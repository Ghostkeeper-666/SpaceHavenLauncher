using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SH.Framework.IO;

public sealed class JarManifestWriter
{
    private readonly Encoding UTF8 = new UTF8Encoding(false);

    public void Write(Stream stream, IEnumerable<KeyValuePair<string, string>> attributes)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(attributes);

        using BinaryWriter writer = new(stream, UTF8, leaveOpen: true);

        foreach (KeyValuePair<string, string> attribute in attributes)
            WriteAttribute(writer, attribute.Key, attribute.Value);

        // Manifest must end with a blank line!
        writer.Write((byte)'\r');
        writer.Write((byte)'\n');

        // Done.
        writer.Flush();
    }

    private void WriteAttribute(BinaryWriter writer, string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        value ??= string.Empty;

        string header = $"{name}: ";
        Span<byte> utf8 = stackalloc byte[4];

        // Header
        writer.Write(UTF8.GetBytes(header));
        int bytesOnLine = UTF8.GetByteCount(header);
        foreach (Rune rune in value.EnumerateRunes())
        {
            int runeBytes = rune.EncodeToUtf8(utf8);

            // Wrap before writing this rune:
            if (bytesOnLine + runeBytes > 72)
            {
                writer.Write((byte)'\r');
                writer.Write((byte)'\n');
                writer.Write((byte)' ');
                bytesOnLine = 1; // continuation space
            }
            writer.Write(utf8.Slice(0, runeBytes));
            bytesOnLine += runeBytes;
        }
        writer.Write((byte)'\r');
        writer.Write((byte)'\n');
    }
}
