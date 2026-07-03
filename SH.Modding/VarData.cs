namespace SH.Modding;

public sealed class VarData
{
    public bool IsModified { get; set; }

    public bool IsSeparator { get; set; }
    public string Name { get; set; }
    public string CurrentValue { get; set; }
    public string OriginalValue { get; set; }
    public string SuggestedValue { get; set; }
    public string PreviousValue { get; set; }
    public string Description { get; set; }
    public int Line { get; set; }

    public override string ToString() => $@"{Name} = ""{CurrentValue}""";
}
