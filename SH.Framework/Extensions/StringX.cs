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



}
