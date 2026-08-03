using System.Collections.Generic;

namespace SH.Framework.Extensions;

public static class ArrayX
{
    public static bool ContentEquals<T>(this T[] left, T[] right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (left.Length != right.Length)
            return false;

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int i = 0; i < left.Length; i++)
            if (!comparer.Equals(left[i], right[i]))
                return false;

        return true;
    }
}
