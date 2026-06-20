#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace SH.Launcher.ViewModels;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public enum EControlState
{
    Standby, // Gray
    Hovered, // Gold
    Ready, // Green
    Error, // OrangeRed
    Running, // Cyan
}
