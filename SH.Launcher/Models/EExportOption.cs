using System;

namespace SH.Launcher.Models;

[Flags]
public enum EExportOption
{
    Original = 1,
    Modified = 2,
    Both = 3,
}