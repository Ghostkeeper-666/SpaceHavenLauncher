using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SH.Framework.Extensions;

public static class EnumX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MaxValue<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Max(e => (int)(object)e);

}
