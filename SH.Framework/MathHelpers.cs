using SH.Framework.Extensions;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SH.Framework;

public static class MathHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOfTwo(int n)
    {
        if (n < 1)
            return 1;
        --n;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n + 1;
    }

    private static readonly string[] TRUE_VALUES = ["yes", "y", "ok", "positive", "afirmative", "continue", "proceed", "do", "do it", "go on", "always"];
    private static readonly string[] FALSE_VALUES = ["no", "n", "not", "negative", "zero", "null", "stop", "abort", "cancel", "do not", "don't", "nope", "never"];

    /// <summary>
    /// A high-tolerance string to boolean conversion method. Output value always defaults to false when parsing is not successful.
    /// </summary>
    public static bool TryConvertToBool(string strValue, out bool boolValue)
    {
        boolValue = false;

        // No way to parse 'null', but it could be interpreted as "false":
        if (strValue == null)
            return false;

        // Empty or whitespace is interpreted as "false"
        if (strValue.IsNullOrWhiteSpace())
            return true;

        // Traditional String to Boolean conversion:
        if (bool.TryParse(strValue, out boolValue))
            return true;

        // Integer to Boolean:
        if (int.TryParse(strValue, out int i))
        {
            boolValue = i != 0;
            return true;
        }

        // Double to Boolean:
        if (double.TryParse(strValue, out double d))
        {
            boolValue = d != 0;
            return true;
        }

        // Non-conventional String to Boolean conversions:
        string strValueLowercase = strValue.ToLowerInvariant();

        if (FALSE_VALUES.Any(v => v == strValueLowercase))
            return true;

        if (TRUE_VALUES.Any(v => v == strValueLowercase))
        {
            boolValue = true;
            return true;
        }

        // Everythng else:
        return false;
    }


}
