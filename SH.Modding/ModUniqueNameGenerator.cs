using SH.Framework.Extensions;
using System;
using System.Text;

namespace SH.Modding;

internal static class ModUniqueNameGenerator
{
    private const string ValidChars = "01234567890abcdefghijklmnopqrstuvwxyz()'_-+";

    public static bool Generate(string displayName, out string normalizedName)
    {
        normalizedName = null;

        if (displayName.IsNullOrWhiteSpace())
            return false; // empty name

        StringBuilder sb = new();
        char prevChar = default;

        for (int i = 0; i < displayName.Length; ++i)
        {
            char ch = displayName[i];

            if (ValidChars.Contains(ch, StringComparison.OrdinalIgnoreCase))
                sb.Append(prevChar = ch);
            else if (prevChar != ' ')
                sb.Append(prevChar = ' ');
        }

        // Trim whitespaces:
        normalizedName = sb.ToString().Trim();

        // Done.
        return !normalizedName.IsNullOrWhiteSpace();
    }
}
