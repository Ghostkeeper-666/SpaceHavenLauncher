using System;

namespace SH.Content.Extensions;

public static class NullableEnum
{
    public static T? Parse<T>(string str) where T : struct => Enum.TryParse(str, out T v) ? v : null;
}
public static class NullableBool
{
    public static bool? Parse(string str) => bool.TryParse(str, out bool v) ? v : null;
}
public static class NullableChar
{
    public static char? Parse(string str) => char.TryParse(str, out char v) ? v : null;
}
public static class NullableByte
{
    public static byte? Parse(string str) => byte.TryParse(str, out byte v) ? v : null;
}

public static class NullableShort
{
    public static short? Parse(string str) => short.TryParse(str, out short v) ? v : null;
}
public static class NullableUShort
{
    public static ushort? Parse(string str) => ushort.TryParse(str, out ushort v) ? v : null;
}

public static class NullableInt
{
    public static int? Parse(string str) => int.TryParse(str, out int v) ? v : null;
}
public static class NullableUInt
{
    public static uint? Parse(string str) => uint.TryParse(str, out uint v) ? v : null;
}

public static class NullableLong
{
    public static long? Parse(string str) => long.TryParse(str, out long v) ? v : null;
}
public static class NullableULong
{
    public static ulong? Parse(string str) => ulong.TryParse(str, out ulong v) ? v : null;
}

public static class NullableFloat
{
    public static float? Parse(string str) => float.TryParse(str, out float v) ? v : null;
}
public static class NullableDouble
{
    public static double? Parse(string str) => double.TryParse(str, out double v) ? v : null;
}
public static class NullableDecimal
{
    public static decimal? Parse(string str) => decimal.TryParse(str, out decimal v) ? v : null;
}
