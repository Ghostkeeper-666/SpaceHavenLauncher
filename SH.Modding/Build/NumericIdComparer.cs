using System;
using System.Collections.Generic;

namespace SH.Modding.Build;

internal sealed class NumericIdComparer : IComparer<string>
{
    public int Compare(string x, string y) => NumericIdComparerImpl(x, y);

    private static int NumericIdComparerImpl(string sid1, string sid2)
    {
        if (ReferenceEquals(sid1, sid2))
            return 0;
        if (sid1 is null)
            return -1;
        if (sid2 is null)
            return 1;

        bool isNum1 = long.TryParse(sid1, out long id1);
        bool isNum2 = long.TryParse(sid2, out long id2);

        if (isNum1 && isNum2)
            return id1.CompareTo(id2);
        if (!isNum1 && !isNum2)
            return string.Compare(sid1, sid2, StringComparison.Ordinal);
        return isNum1 ? -1 : 1;
    }
}