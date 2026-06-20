using System;

namespace SH.Framework.FastZip;

internal static class JarDateTime
{
    internal static DateTime ToDateTime(ushort date, ushort time)
    {
        try
        {
            int day = date & 0x1F;
            int month = (date >> 5) & 0x0F;
            int year = ((date >> 9) & 0x7F) + 1980;

            int second = (time & 0x1F) * 2;
            int minute = (time >> 5) & 0x3F;
            int hour = (time >> 11) & 0x1F;

            if (day == 0 || month == 0)
                return DateTime.MinValue;

            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    internal static void FromDateTime(DateTime dt, out ushort date, out ushort time)
    {
        try
        {
            if (dt < new DateTime(1980, 1, 1))
                dt = new DateTime(1980, 1, 1);

            dt = dt.ToLocalTime();

            int year = dt.Year - 1980;
            int month = dt.Month;
            int day = dt.Day;
            int hour = dt.Hour;
            int minute = dt.Minute;
            int second = dt.Second / 2; // 2-second granularity

            date = (ushort)((year << 9) | (month << 5) | day);
            time = (ushort)((hour << 11) | (minute << 5) | second);
        }
        catch
        {
            date = 0;
            time = 0;
        }
    }
}