using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SH.Content.Modding.Build;

internal sealed class VarBuildData
{
    public VarBuildData(VarData data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Name = Data?.Name?.Trim()?.Trim('{', '}') ?? throw new ArgumentException(nameof(Name));
        BracedName = $"{{{Name}}}";
        StrValue = Data.CurrentValue ?? string.Empty;
        Description = Data.Description ?? string.Empty;
    }

    private VarData Data { get; }
    public string Name { get; }
    public EVariableType Type { get; } = EVariableType.Unknown; // TODO
    public string BracedName { get; }
    public string StrValue { get; }
    public string Description { get; }

    public bool TryGetIntValue(out int value) =>
        int.TryParse(StrValue, out value);

    public bool TryGetDoubleValue(out double value) =>
        double.TryParse(StrValue, out value);

    public bool TryGetBoolValue(out bool value) =>
        MathHelpers.TryConvertToBool(StrValue, out value);

    public bool TryGetRestrictedStringValue(IEnumerable<string> validValues, out string value) =>
        (value = validValues.FirstOrDefault(str => str.Equals(StrValue, StringComparison.OrdinalIgnoreCase))) != null;

    public override string ToString() => $@"{Name} = ""{StrValue}""";
}
