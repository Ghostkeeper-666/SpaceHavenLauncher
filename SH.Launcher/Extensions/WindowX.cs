using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System.Collections.Generic;

namespace SH.Launcher.Extensions;

internal static class WindowX
{
    public static int GetMonitorIndex(this Window window)
    {
        Screen current = window.Screens.ScreenFromWindow(window);
        for (int i = 0; i < window.Screens.All.Count; i++)
            if (window.Screens.All[i] == current)
                return i;
        return 0;
    }

    public static void RestoreToMonitor(this Window window, int index)
    {
        IReadOnlyList<Screen> screens = window.Screens.All;
        if (index < 0 || index >= screens.Count)
            return;
        Screen screen = screens[index];
        PixelRect bounds = screen.WorkingArea;
        WindowState state = window.WindowState;
        window.WindowState = WindowState.Normal;
        window.Position = new PixelPoint(bounds.X, bounds.Y);
        window.WindowState = state;
    }
}
