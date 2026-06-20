using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SH.Framework.Extensions;

public static class EnumerableX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string JoinToString(this IEnumerable items, string separator = null)
    {
        if (items == null)
            return string.Empty;
        separator ??= string.Empty;
        StringBuilder sb = new();
        foreach (object o in items)
        {
            if (o != null) sb.Append(o.ToString());
            sb.Append(separator);
        }
        if (sb.Length >= separator.Length)
            sb.Length -= separator.Length;
        return sb.ToString();
    }

    public static string JoinToString<T>(this IEnumerable<T> items, Func<T, string> objToStringSelector, string separator = null)
    {
        if (items == null)
            return string.Empty;
        separator ??= string.Empty;
        StringBuilder sb = new();
        foreach (T o in items)
        {
            sb.Append(objToStringSelector(o));
            sb.Append(separator);
        }
        if (sb.Length >= separator.Length)
            sb.Length -= separator.Length;
        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IList<T> CopyFrom<T>(this IList<T> target, IEnumerable<T> source, int start = 0, int count = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        if (count <= 0)
            return target;
        if (start + count > target.Count)
            count = target.Count - start;
        int i = 0;
        foreach (T item in source)
        {
            if (i >= count) return target;
            target[start + i++] = item;
        }
        if (i >= count) return target;
        throw new IndexOutOfRangeException(nameof(source));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IList<T> SetAllTo<T>(this IList<T> target, T value)
    {
        ArgumentNullException.ThrowIfNull(target);
        for (int i = 0; i < target.Count; ++i)
            target[i] = value;
        return target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OrderedDictionary<TKey, TValue> ToOrderedDictionary<T, TKey, TValue>(this IEnumerable<T> items, Func<T, TKey> keyFactory, Func<T, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(items);
        OrderedDictionary<TKey, TValue> dict = [];
        foreach (T item in items)
            dict.TryAdd(keyFactory(item), valueFactory(item));
        return dict;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        ObservableCollection<T> list = new();
        foreach (T item in items)
            list.Add(item);
        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<(T1, T2)> ToTuples<T1, T2>(this IEnumerable<KeyValuePair<T1, T2>> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        foreach (KeyValuePair<T1, T2> kvp in source)
            yield return (kvp.Key, kvp.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<T> AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
    {
        foreach (T item in items)
            set.Add(item);
        return set;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SortedSet<T> AddRange<T>(this SortedSet<T> set, IEnumerable<T> items)
    {
        foreach (T item in items)
            set.Add(item);
        return set;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAll<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
    {
        // Empty values => vacuously true
        if (values.Length == 0)
            return true;

        // For each value to check, see if it exists in span.
        // Uses MemoryExtensions.Contains(ReadOnlySpan<T>, T).
        for (int i = 0; i < values.Length; i++)
        {
            if (!span.Contains(values[i]))
                return false;
        }

        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OrderedDictionary<TKey, TValue> AddRange<TKey, TValue, T>(this OrderedDictionary<TKey, TValue> dict, IEnumerable<T> items, Func<T, TKey> keySelector, Func<T, TValue> valueSelector, EDuplicateEntryPolicy duplicateEntryPolicy = EDuplicateEntryPolicy.Throw)
    {
        ArgumentNullException.ThrowIfNull(dict);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(valueSelector);

        switch (duplicateEntryPolicy)
        {
            case EDuplicateEntryPolicy.Throw:
                foreach (T item in items)
                    dict.Add(keySelector(item), valueSelector(item));
                return dict;

            case EDuplicateEntryPolicy.Replace:
                foreach (T item in items)
                    dict[keySelector(item)] = valueSelector(item);
                return dict;

            case EDuplicateEntryPolicy.Skip:
                foreach (T item in items)
                    dict.TryAdd(keySelector(item), valueSelector(item));
                return dict;

            default:
                throw new NotImplementedException($"{nameof(EDuplicateEntryPolicy)} = {duplicateEntryPolicy}");
        }
    }

    public static SortedSet<T> ToSortedSet<T>(this IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        SortedSet<T> set = new();
        foreach (T item in items)
            set.Add(item);
        return set;
    }

}
