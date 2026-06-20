using System;

namespace SH.Content.Xml.Haven.Elements;

[Flags]
public enum EWorkConsoleEventType
{
    Jump = 1,
    Hail = 2,
    Motor = 4,
    Turret = 8,
    Shield = 16,
}
