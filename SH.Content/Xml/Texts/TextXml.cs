using SH.Content.Enums;
using System.Collections.Generic;
using System.Linq;

namespace SH.Content.Xml.Texts;

public sealed class TextXml
{
    public int Id { get; set; }
    public int PID { get; set; }
    public List<string> Value { get; } = [];

    public string GetValue(ELanguage lang) =>
        Value[(int)lang] ??
        Value.FirstOrDefault(v => v != null) ??
        string.Empty;
}
