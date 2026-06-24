using System;
using System.Runtime.CompilerServices;

namespace SH.Framework.Extensions;

public static class StringX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSuffix(this string s, string suffix, StringComparison sc = StringComparison.Ordinal) =>
        s.IsNullOrEmpty() || suffix.IsNullOrEmpty() ? s :
        s.EndsWith(suffix, sc) ? s.Substring(0, s.Length - suffix.Length) : s;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemovePrefix(this string s, string prefix, StringComparison sc = StringComparison.Ordinal) =>
        s.IsNullOrEmpty() || prefix.IsNullOrEmpty() ? s :
        s.StartsWith(prefix, sc) ? s.Substring(prefix.Length) : s;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, string value, StringComparison sc = StringComparison.Ordinal)
    {
        if(text.IsNullOrEmpty() || value.IsNullOrEmpty())
            return 0;
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, sc)) >= 0)
        {
            ++count;
            index += value.Length;
        }
        return count;
    }


    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse<TEnum>(this string str, out TEnum value) where TEnum : struct, Enum =>
        Enum.TryParse(str ?? string.Empty, true, out value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseFromNumericValue<TEnum>(this string str, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        if(!str.TryParse(out int number))
            return false;
        value = (TEnum)Enum.ToObject(typeof(TEnum), number);
        if(!Enum.IsDefined(value))
            return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out bool value) =>
        bool.TryParse(str ?? string.Empty, out value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out short value) =>
        short.TryParse(str ?? string.Empty, out value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out ushort value) =>
        ushort.TryParse(str ?? string.Empty, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out int value) =>
        int.TryParse(str ?? string.Empty, out value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out uint value) =>
        uint.TryParse(str ?? string.Empty, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out long value) =>
        long.TryParse(str ?? string.Empty, out value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out ulong value) =>
        ulong.TryParse(str ?? string.Empty, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out float value) =>
        float.TryParse(str ?? string.Empty, out value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out double value) =>
        double.TryParse(str ?? string.Empty, out value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(this string str, out decimal value) =>
        decimal.TryParse(str ?? string.Empty, out value);
}
