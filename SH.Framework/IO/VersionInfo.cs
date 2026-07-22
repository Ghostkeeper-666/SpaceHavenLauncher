using SH.Framework.Extensions;
using System;
using System.Collections.Generic;

namespace SH.Framework.IO;

public sealed class VersionInfo : IComparable<VersionInfo>, IComparable
{
    public string RawValue { get; }
    private string Value { get; }

    private readonly List<string> Items = new();

    public string Major => Items.Count > 0 ? Items[0] ?? "0" : "0";
    public string Minor => Items.Count > 1 ? Items[1] ?? "0" : "0";
    public string Build => Items.Count > 2 ? Items[2] ?? "0" : "0";
    public string Release => Items.Count > 3 ? Items[3] ?? "0" : "0";

    public VersionInfo(string rawVersion)
    {
        rawVersion = rawVersion?.Trim()?.TrimStart('v')?.Trim('.', ' ', '-');
        if (rawVersion.IsNullOrWhiteSpace())
            rawVersion = "0.0.0";
        RawValue = rawVersion;

        string[] splitted = rawVersion.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (splitted.Length <= 0)
        {
            Items.Add("0");
            Items.Add("0");
            Items.Add("0");
            return;
        }
        for (int i = 0; i < splitted.Length; ++i)
        {
            string v = splitted[i].Trim().Replace(" ", "-").Trim('-');
            if (v.Length <= 0)
                v = "0";
            else if (!char.IsDigit(v[0]))
                v = $"0-{v}";
            Items.Add(v);
        }
        Value = Items.JoinToString(".");
    }

    private static List<string> GetSplittedVersion(VersionInfo version, int precision)
    {
        if (precision <= 0)
            throw new ArgumentException($"Parameter '{nameof(precision)}' must be greater than zero", nameof(precision));
        List<string> splitted = new();
        for (int i = 0; i < precision; ++i)
            splitted.Add(i < version.Items.Count ? version.Items[i]?.ToLowerInvariant() ?? "0" : "0");
        return splitted;
    }

    public static bool operator <(VersionInfo left, VersionInfo right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(VersionInfo left, VersionInfo right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(VersionInfo left, VersionInfo right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(VersionInfo left, VersionInfo right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) >= 0;
    }

    public static bool operator ==(VersionInfo left, VersionInfo right)
    {
        if(left is null)
        {
             if(right is null)
                return true;
             else
                return false;
        }
        else // left is not null
        {
             if(right is null)
                return false;
             else
                return left.CompareTo(right) == 0;
        }
    }

    public static bool operator !=(VersionInfo left, VersionInfo right)
    {
        if(left is null)
        {
             if(right is null)
                return false;
             else
                return true;
        }
        else // left is not null
        {
             if(right is null)
                return true;
             else
                return left.CompareTo(right) != 0;
        }
    }

    public bool IsEqualTo(VersionInfo other) =>
        CompareTo(other) == 0;

    public bool IsLessThan(VersionInfo other) =>
        CompareTo(other) < 0;

    public bool IsLessThanOrEqualTo(VersionInfo other) =>
        CompareTo(other) <= 0;

    public bool IsGreaterThan(VersionInfo other) =>
        CompareTo(other) > 0;

    public bool IsGreaterThanOrEqualTo(VersionInfo other) =>
        CompareTo(other) >= 0;




    public override bool Equals(object obj) =>
        obj is VersionInfo other && CompareTo(other) == 0;

    public override int GetHashCode() =>
        Value.GetHashCode();

    public int CompareTo(VersionInfo other)
    {
        ArgumentNullException.ThrowIfNull(other);

        int size = Items.Count > other.Items.Count ? Items.Count : other.Items.Count;
        List<string> items1 = GetSplittedVersion(this, size);
        List<string> items2 = GetSplittedVersion(other, size);

        for (int i = 0; i < size; ++i)
        {
            string v1 = items1[i];
            string v2 = items2[i];
            bool isNum1 = uint.TryParse(v1, out uint n1);
            bool isNum2 = uint.TryParse(v2, out uint n2);

            if (isNum1 && !isNum2)
                return -1;

            if (!isNum1 && isNum2)
                return +1;

            // num comparison
            if (isNum1 && isNum2)
            {
                if (n1 < n2)
                    return -1;
                else if (n1 == n2)
                    continue;
                else
                    return +1;
            }

            int compare = v1.CompareTo(v2);
            if (compare < 0)
                return -1;
            else if (compare == 0)
                continue;
            else // compare > 0
                return +1;
        }

        // Same version:
        return 0;
    }

    int IComparable.CompareTo(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        if (obj is not VersionInfo other)
            throw new ArgumentException($"{nameof(VersionInfo)}.{nameof(CompareTo)}({nameof(obj)})", nameof(obj));
        return CompareTo(other);
    }

    public override string ToString() => RawValue;
}
