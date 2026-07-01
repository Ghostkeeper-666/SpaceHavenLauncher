using SH.Framework.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SH.Framework.IO;

public sealed class VersionCompatibilityList
{
    public VersionCompatibilityList() { }

    public IReadOnlyList<VersionCompatibility> Items => ItemList;
    private readonly List<VersionCompatibility> ItemList = [];

    public void AddRange(IEnumerable<VersionCompatibility> items)
    {
        foreach (VersionCompatibility item in items ?? [])
            ItemList.Add(item);
    }

    public void Add(VersionCompatibility item) =>
        ItemList.Add(item);

    public string ToDisplayString(string separator = null)
    {
        separator ??= Environment.NewLine;

        if (ItemList.Count <= 0)
            return null;

        StringBuilder sb = new();
        foreach (VersionCompatibility item in ItemList.OrderBy(item => item.Name).ThenByDescending(item => item.Version))
        {
            if (sb.Length > 0)
                sb.Append(separator);
            if (item.Operator == EVersionOperator.any)
                sb.Append($"{item.Name} (any version)".Trim());
            else
                sb.Append($"{item.Name} {item.Operator.ToDisplayString()} {item.Version}".Trim());
        }
        return sb.ToString();
    }

    public bool MatchAll(string name, VersionInfo version) =>
        ItemList.Count <= 0 || ItemList.All(item => item.Match(name, version));

    public bool MatchAny(string name, VersionInfo version) =>
        ItemList.Count > 0 && ItemList.Any(item => item.Match(name, version));
}
