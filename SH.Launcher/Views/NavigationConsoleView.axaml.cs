using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Extensions;
using SH.Launcher.Core.Models;
using SH.Launcher.ViewModels;
using SH.Launcher.ViewModels.Enums;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SH.Launcher.Views;

public partial class NavigationConsoleView : UserControl
{
    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;

    private readonly Image[] NavigationConsoleControlImages;

    #region Control Areas

    private readonly Point[] Polygon_LeftButtons =
    [
        new(0.128, 0.484),
        new(0.312, 0.484),
        new(0.248, 0.800),
        new(0.027, 0.800)
    ];

    private readonly Point[] Polygon_LeftLever =
    [
        new(0.346, 0.484),
        new(0.468, 0.484),
        new(0.468, 0.888),
        new(0.346, 0.888)
    ];

    private readonly Point[] Polygon_RightLever =
    [
        new(0.532, 0.484),
        new(0.656, 0.484),
        new(0.656, 0.888),
        new(0.532, 0.888)
    ];

    private readonly Point[] Polygon_RightButtons =
    [
        new(0.710, 0.484),
        new(0.898, 0.484),
        new(0.999, 0.800),
        new(0.769, 0.800)
    ];

    #endregion

    public NavigationConsoleView()
    {
        InitializeComponent();

        NavigationConsoleControlImages =
        [
            LeftButtonsImage,
            LeftLeverImage,
            RightLeverImage,
            RightButtonsImage,
        ];

        NavigationConsoleGrid.SizeChanged += NavigationConsoleView_SizeChanged;
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void NavigationConsoleView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;

        RootGrid.RowDefinitions[0].Height = new GridLength(NavigationConsoleGrid.Height, GridUnitType.Pixel);

        if (this.TryFindResource(NavigationConsoleGrid.Bounds.Height > 0 ? "CollapseUp" : "CollapseMiddle", out object resource) && resource is StreamGeometry geometry)
            vm.ToggleLogViewButtonIcon = geometry;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;
        vm.SetBackgroundImage();
        base.OnAttachedToVisualTree(e);
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;
        UpdateControlImages(vm);
        ScrollLogToEnd();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;

        vm.LeftScreen.PropertyChanged -= OnLeftConsolePropertyChanged;
        vm.LeftScreen.PropertyChanged += OnLeftConsolePropertyChanged;

        vm.CentralScreen.PropertyChanged -= OnCentralConsolePropertyChanged;
        vm.CentralScreen.PropertyChanged += OnCentralConsolePropertyChanged;

        vm.RightScreen.PropertyChanged -= OnRightConsolePropertyChanged;
        vm.RightScreen.PropertyChanged += OnRightConsolePropertyChanged;

        State.LogHistory.CollectionChanged -= LogChanged;
        State.LogHistory.CollectionChanged += LogChanged;

        UpdateControlImages(vm);
        ScrollLogToEnd();
    }

    private void UpdateControlImages(NavigationConsoleViewModel vm)
    {
        UpdateNavigationConsoleControlImage(ENavigationConsoleControl.LeftButtons, vm.LeftScreen.LeftButtonsState, vm.LeftScreen.LeftButtonsPressed, vm.LeftScreen.LeftButtonsHovered);
        UpdateNavigationConsoleControlImage(ENavigationConsoleControl.LeftLever, vm.CentralScreen.LeftLeverState, vm.CentralScreen.LeftLeverPressed, vm.CentralScreen.LeftLeverHovered);
        UpdateNavigationConsoleControlImage(ENavigationConsoleControl.RightLever, vm.CentralScreen.RightLeverState, vm.CentralScreen.RightLeverPressed, vm.CentralScreen.RightLeverHovered);
        UpdateNavigationConsoleControlImage(ENavigationConsoleControl.RightButtons, vm.RightScreen.RightButtonsState, vm.RightScreen.RightButtonsPressed, vm.RightScreen.RightButtonsHovered);
    }

    private void OnLeftConsolePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;
        switch (e.PropertyName)
        {
            case nameof(LeftScreen.LeftButtonsState):
            case nameof(LeftScreen.LeftButtonsPressed):
            case nameof(LeftScreen.LeftButtonsHovered):
                UpdateNavigationConsoleControlImage(ENavigationConsoleControl.LeftButtons, vm.LeftScreen.LeftButtonsState, vm.LeftScreen.LeftButtonsPressed, vm.LeftScreen.LeftButtonsHovered);
                return;
            default:
                return;
        }
    }

    private void OnCentralConsolePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;

        switch (e.PropertyName)
        {
            case nameof(CentralScreen.LeftLeverState):
            case nameof(CentralScreen.LeftLeverPressed):
            case nameof(CentralScreen.LeftLeverHovered):
                UpdateNavigationConsoleControlImage(ENavigationConsoleControl.LeftLever, vm.CentralScreen.LeftLeverState, vm.CentralScreen.LeftLeverPressed, vm.CentralScreen.LeftLeverHovered);
                return;
            case nameof(CentralScreen.RightLeverState):
            case nameof(CentralScreen.RightLeverPressed):
            case nameof(CentralScreen.RightLeverHovered):
                UpdateNavigationConsoleControlImage(ENavigationConsoleControl.RightLever, vm.CentralScreen.RightLeverState, vm.CentralScreen.RightLeverPressed, vm.CentralScreen.RightLeverHovered);
                return;
            default:
                return;
        }
    }

    private void OnRightConsolePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;
        switch (e.PropertyName)
        {
            case nameof(RightScreen.RightButtonsState):
            case nameof(RightScreen.RightButtonsPressed):
            case nameof(RightScreen.RightButtonsHovered):
                UpdateNavigationConsoleControlImage(ENavigationConsoleControl.RightButtons, vm.RightScreen.RightButtonsState, vm.RightScreen.RightButtonsPressed, vm.RightScreen.RightButtonsHovered);
                return;
            default:
                return;
        }
    }

    private void UpdateNavigationConsoleControlImage(ENavigationConsoleControl control, EControlState state, bool isPressed, bool isHovered)
    {
        if (isHovered && !isPressed && state != EControlState.Running)
            NavigationConsoleControlImages[(int)control].Set(
                $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/NavigationConsole/CONTROL-Hovered.jpg"
                .Replace("CONTROL", control.ToString())
            );

        else if (isPressed && state != EControlState.Running)
            NavigationConsoleControlImages[(int)control].Set(
                $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/NavigationConsole/CONTROL-Running.jpg"
                .Replace("CONTROL", control.ToString())
            );

        else
            NavigationConsoleControlImages[(int)control].Set(
                $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/NavigationConsole/CONTROL-STATE.jpg"
                .Replace("CONTROL", control.ToString())
                .Replace("STATE", state.ToString())
            );
    }

    private void LogChanged(object sender, NotifyCollectionChangedEventArgs e) =>
        ScrollLogToEnd();

    private void ScrollLogToEnd()
    {
        try
        {
            State.DispatchQueue.TryEnqueue(() =>
            {
                if (!IsLoaded || LogListBox.ItemCount <= 0)
                    return;
                ScrollViewer scrollViewer = LogListBox?.GetVisualDescendants()?.OfType<ScrollViewer>()?.FirstOrDefault();
                scrollViewer?.ScrollToEnd();
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }

    public static bool IsPolygonClicked(Point pos, Point size, IReadOnlyList<Point> normalizedPolygon) =>
        normalizedPolygon.Contains(pos.Normalize(size));

    private async void OnPointerMovedAsync(object sender, PointerEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;
        if (sender is not Grid grid)
            return;

        bool isPressed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;

        vm.LeftScreen.LeftButtonsHovered = IsPolygonClicked(e.GetPosition(grid), new(grid.Bounds.Width, grid.Bounds.Height), Polygon_LeftButtons);
        vm.CentralScreen.LeftLeverHovered = IsPolygonClicked(e.GetPosition(grid), new(grid.Bounds.Width, grid.Bounds.Height), Polygon_LeftLever);
        vm.CentralScreen.RightLeverHovered = IsPolygonClicked(e.GetPosition(grid), new(grid.Bounds.Width, grid.Bounds.Height), Polygon_RightLever);
        vm.RightScreen.RightButtonsHovered = IsPolygonClicked(e.GetPosition(grid), new(grid.Bounds.Width, grid.Bounds.Height), Polygon_RightButtons);

        if (vm.LeftScreen.LeftButtonsHovered)
        {
            if (!isPressed)
            {
                vm.LeftScreen.LeftButtonsHovered = true;
                vm.CentralScreen.LeftLeverHovered = false;
                vm.CentralScreen.RightLeverHovered = false;
                vm.RightScreen.RightButtonsHovered = false;
                if (!State.IsProcessing)
                    State.StatusBarText = @"Re-initialize the mod system";
            }
        }
        else if (vm.CentralScreen.LeftLeverHovered)
        {
            if (!isPressed)
            {
                vm.LeftScreen.LeftButtonsHovered = false;
                vm.CentralScreen.LeftLeverHovered = true;
                vm.CentralScreen.RightLeverHovered = false;
                vm.RightScreen.RightButtonsHovered = false;
                if (!State.IsProcessing)
                {
                    State.StatusBarText = @"Launch the original 'vanilla' game";
                    vm.CentralScreen.ShowLaunchOriginalTitleOnMonitor();
                }
            }
        }
        else if (vm.CentralScreen.RightLeverHovered)
        {
            if (!isPressed)
            {
                vm.LeftScreen.LeftButtonsHovered = false;
                vm.CentralScreen.LeftLeverHovered = false;
                vm.CentralScreen.RightLeverHovered = true;
                vm.RightScreen.RightButtonsHovered = false;
                if (!State.IsProcessing)
                {
                    State.StatusBarText = @"Launch the modified game";
                    vm.CentralScreen.ShowLaunchModifiedTitleOnMonitor();
                }
            }
        }
        else if (vm.RightScreen.RightButtonsHovered)
        {
            if (!isPressed)
            {
                vm.LeftScreen.LeftButtonsHovered = false;
                vm.CentralScreen.LeftLeverHovered = false;
                vm.CentralScreen.RightLeverHovered = false;
                vm.RightScreen.RightButtonsHovered = true;
                if (!State.IsProcessing)
                    State.StatusBarText = "Extract game library files - you must comply with BugByte EULA!";
            }
        }
        else
        {
            if (!isPressed)
            {
                State.StatusBarText = string.Empty;
                if (!State.IsProcessing)
                    vm.CentralScreen.ShowEmptyOnMonitor();
            }
        }
    }

    private async void OnMouseClickPressedAsync(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;

        if (vm.LeftScreen.LeftButtonsHovered)
        {
            vm.LeftScreen.LeftButtonsPressed = true;
            vm.CentralScreen.LeftLeverPressed = false;
            vm.CentralScreen.RightLeverPressed = false;
            vm.RightScreen.RightButtonsPressed = false;
            return;
        }

        if (vm.CentralScreen.LeftLeverHovered)
        {
            vm.LeftScreen.LeftButtonsPressed = false;
            vm.CentralScreen.LeftLeverPressed = true;
            vm.CentralScreen.RightLeverPressed = false;
            vm.RightScreen.RightButtonsPressed = false;
            return;
        }

        if (vm.CentralScreen.RightLeverHovered)
        {
            vm.LeftScreen.LeftButtonsPressed = false;
            vm.CentralScreen.LeftLeverPressed = false;
            vm.CentralScreen.RightLeverPressed = true;
            vm.RightScreen.RightButtonsPressed = false;
            return;
        }

        if (vm.RightScreen.RightButtonsHovered)
        {
            vm.LeftScreen.LeftButtonsPressed = false;
            vm.CentralScreen.LeftLeverPressed = false;
            vm.CentralScreen.RightLeverPressed = false;
            vm.RightScreen.RightButtonsPressed = true;
            return;
        }
    }

    private async void OnMouseClickReleasedAsync(object sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not NavigationConsoleViewModel vm)
            return;

        if (vm.LeftScreen.LeftButtonsPressed)
        {
            vm.LeftScreen.LeftButtonsPressed = false;
            if (vm.LeftScreen.LeftButtonsHovered)
                await vm.OnLeftButtons();
            return;
        }

        if (vm.CentralScreen.LeftLeverPressed)
        {
            vm.CentralScreen.LeftLeverPressed = false;
            if (vm.CentralScreen.LeftLeverHovered)
                await vm.OnLeftLever();
            return;
        }

        if (vm.CentralScreen.RightLeverPressed)
        {
            vm.CentralScreen.RightLeverPressed = false;
            if (vm.CentralScreen.RightLeverHovered)
                await vm.OnRightLever();
            return;
        }

        if (vm.RightScreen.RightButtonsPressed)
        {
            vm.RightScreen.RightButtonsPressed = false;
            if (vm.RightScreen.RightButtonsHovered)
                await vm.OnRightButtons();
            return;
        }
    }

    private void OnToggleLogView(object sender, PointerPressedEventArgs e)
    {
        NavigationConsoleGrid.Width = NavigationConsoleGrid.Width == 0.0 ? double.NaN : 0.0;
        if (NavigationConsoleGrid.Width != 0.0)
            ScrollLogToEnd();
    }

    private async void LogLineClickedAsync(object sender, PointerPressedEventArgs e)
    {
        try
        {
            if (sender is not Visual visual)
                return;
            PointerPoint point = e.GetCurrentPoint(visual);

            if (visual.DataContext is not LogMessage logMessage)
                return;

            if (point.Properties.IsLeftButtonPressed)
                State.DispatchQueue.TryEnqueue(() => State.CopyToClipboardAsync(logMessage.Text));

            else if (point.Properties.IsRightButtonPressed)
                State.DispatchQueue.TryEnqueue(() => OS.OpenLinkAsync(SpaceHavenLauncher.Directory, logMessage.Link, Log));
        }
        catch (Exception ex)
        {
            Log.Debug(ex);
        }
    }

    private async void LogLinePointerEnteredAsync(object sender, PointerEventArgs e)
    {
        try
        {
            if (sender is not Visual visual)
                return;
            if (visual.DataContext is not LogMessage logMessage)
                return;
            if (logMessage.Link != null)
                State.StatusBarText = $@"Right click to open    {logMessage.Link}";
        }
        catch (Exception ex)
        {
            Log.Debug(ex);
        }
    }

    private void LogLinePointerExitedAsync(object sender, PointerEventArgs e)
    {
        try
        {
            State.StatusBarText = string.Empty;
        }
        catch (Exception ex)
        {
            Log.Debug(ex);
        }
    }

    private async void CopyLogStartAsync(object sender, PointerPressedEventArgs e)
    {
        CopyLogIcon.Foreground = Brushes.Cyan;
        string fullLog = State.LogHistory.Select(msg => msg.ToString()).JoinToString(Environment.NewLine);
        IClipboard clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        await clipboard?.SetTextAsync(fullLog);
    }

    private async void CopyLogEndAsync(object sender, PointerReleasedEventArgs e)
    {
        await Task.Delay(100);
        CopyLogIcon.Foreground = Brushes.LimeGreen;
    }

    private async void ClearLogStartAsync(object sender, PointerPressedEventArgs e)
    {
        ClearLogIcon.Foreground = Brushes.Cyan;
        Log.Debug(new string('#', 200));
        await Task.Yield();
        await Task.Delay(100);
        await Task.Yield();
        State.LogHistory.Clear();
    }

    private async void ClearLogEndAsync(object sender, PointerReleasedEventArgs e)
    {
        await Task.Delay(100);
        Log.Success("The log history has been cleared");
        ClearLogIcon.Foreground = Brushes.Red;
    }


}