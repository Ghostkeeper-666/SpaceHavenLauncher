using System.Collections.Generic;

namespace SH.Framework.Extensions;

public static class ListX
{
    public static bool ContentEquals<T>(this List<T> left, List<T> right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (left.Count != right.Count)
            return false;

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int i = 0; i < left.Count; i++)
            if (!comparer.Equals(left[i], right[i]))
                return false;

        return true;
    }

    public static void RemoveAll<T>(this List<T> list, IEnumerable<T> items)
    {
        foreach (T item in items)
            while (list.Remove(item)) ;
    }


}
