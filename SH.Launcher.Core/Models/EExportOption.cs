using System;

namespace SH.Launcher.Core.Models;

[Flags]
public enum EExportOption
{
    Original = 1,
    Modified = 2,
    Both = 3,
}