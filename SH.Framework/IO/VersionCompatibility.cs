using System;

namespace SH.Framework.IO;

public sealed class VersionCompatibility : IComparable<VersionCompatibility>
{
    public VersionCompatibility(string name, VersionInfo version, EVersionOperator op)
    {
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Operator = op;
    }

    public string Name { get; }
    public VersionInfo Version { get; }
    public EVersionOperator Operator { get; }

    public int CompareTo(VersionCompatibility other)
    {
        int nameComp = Name.CompareTo(other?.Name);
        if(nameComp != 0) return nameComp;

        int versionComp = Version?.CompareTo(other?.Version) ?? 0;
        if(versionComp != 0) return versionComp;

        int opComp = Operator.CompareTo(other.Operator);
        return opComp;
    }



    /// <summary>
    /// TODO: Improve this later. Currently considering simple inputs only.
    /// </summary>
    public bool Match(string name, VersionInfo otherVersion) =>
        Name.Equals(name, StringComparison.OrdinalIgnoreCase) && Operator switch
        {
            EVersionOperator.any => true,
            EVersionOperator.eq => otherVersion == Version,
            EVersionOperator.lt => otherVersion < Version,
            EVersionOperator.lte => otherVersion <= Version,
            EVersionOperator.gt => otherVersion > Version,
            EVersionOperator.gte => otherVersion >= Version,
            _ => throw new NotImplementedException($"{nameof(EVersionOperator)} = {Operator}"),
        };

    public override string ToString() => $"{Name} {Operator.ToDisplayString()} {Version}";
}
