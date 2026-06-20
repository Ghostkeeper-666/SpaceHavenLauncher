using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SH.Framework.Progress;

internal sealed class ProgressArgumentException
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNotFiniteAndPositive<T>(T value, [CallerArgumentExpression(nameof(value))] string paramName = null) where T : INumberBase<T>
    {
        if (T.IsNegative(value) || T.IsZero(value) || T.IsNaN(value) || T.IsInfinity(value))
            ThrowNotFiniteAndPositive(value, paramName);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowNotFiniteAndPositive<T>(T value, string paramName) =>
        throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a finite positive number");

}
