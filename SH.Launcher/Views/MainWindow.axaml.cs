using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Launcher.Extensions;
using SH.Launcher.ViewModels;
using SH.Launcher.ViewModels.Enums;
using SH.Modding.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Views;

public partial class MainWindow : Window
{
    internal static Window Window; // disgusting workaround for message box and to set current monitor

    public AppViewModel State => AppViewModel.State;
    public new DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private Task SearchTask;
    private SemaphoreSlim SearchSignal;
    private volatile string SearchText;

    public MainWindow()
    {
        AppViewModel.State.Log.SetLogLevel(ELogLevel.Info);

        InitializeComponent();

        Log.OnMessage += OnLog;
        Loaded += OnLoadedAsync;
        Closing += OnClosing;

        BackgroundDarknessSlider.PointerEntered += OnSliderPointerEntered;
        BackgroundDarknessSlider.PointerExited += OnSliderPointerExited;
        BackgroundDarknessSlider.PointerPressed += OnSliderPointerPressed;
        BackgroundDarknessSlider.PointerReleased += OnSliderPointerReleased;
        BackgroundDarknessSlider.PropertyChanged += OnSliderPropertyChangedAsync;

        Window = this;

        State.AppSettings.PropertyChanged += State_PropertyChanged;
    }

    private void State_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppSettingsViewModel.IsLeftPaneCollapsed):
                if (State.AppSettings.IsLeftPaneCollapsed)
                {
                    SearchTextbox.IsVisible = false;
                    SearchTextbox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                    SearchTextbox.Width = 0.0;
                    SearchTextbox.Text = string.Empty;
                }
                else
                {
                    SearchTextbox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                    SearchTextbox.Width = double.NaN;
                    SearchTextbox.IsVisible = true;
                }
                break;

            default:
                return;
        }
    }

    private bool CloseActionsPerformed = false;
    private async void OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (CloseActionsPerformed)
            return;

        // Cancel this closing:
        e.Cancel = true;

        // Save PATH Settings:
        try
        {
            PathSettingsRepositoryService repo = new(Log);
            await repo.TrySaveAsync(Paths.Data, default);
        }
        catch { }

        // Save UI Settings:
        try
        {
            int airlockIndex = GetLeftPaneIndexOf(EPageType.Airlock);
            if (airlockIndex >= 0 && LeftPaneListBox.SelectedIndex == airlockIndex)
            {
                State.AirlockPage.Stop();
            }

            AppSettingsData data = AppSettings.GetData();
            try { data.MonitorIndex = this.GetMonitorIndex(); } catch { }

            AppSettingsRepositoryService repo = new(Paths.Data, Log);
            await repo.TrySaveAsync(data, default);
        }
        catch { }

        // Call Close() again:
        CloseActionsPerformed = true;
        Close();
    }

    private void OnLog(object sender, LogMessage message)
    {
        foreach ((string value, string replacement) in State?.LogReplacements ?? [])
            if (message.RawText.Contains(value, StringComparison.OrdinalIgnoreCase))
                message.RawText = message.RawText.Replace(value, replacement, StringComparison.OrdinalIgnoreCase);
        Dispatcher.Run(() => State.LogHistory.Add(message));
    }

    private int GetLeftPaneIndexOf(EPageType paneItem)
    {
        for (int i = 0; i < LeftPaneListBox.ItemCount; ++i)
            if (LeftPaneListBox.Items[i] is LeftPaneItemViewModel leftPaneItem && leftPaneItem.Type == paneItem)
                return i;
        return -1;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        StartBackgroundSearchTask();
        _ = CycleBackgroundImagesAsync();
        await vm.OnViewLoadedAsync(default);
    }

    private void StartBackgroundSearchTask()
    {
        SearchSignal = new SemaphoreSlim(0);
        SearchTask = Task.Run(async () => { try { await SearchLoopAsync(); } catch { } });
    }

    private async Task SearchLoopAsync()
    {
        string previousSearchText = string.Empty;

        while (true)
        {
            try
            {
                await SearchSignal?.WaitAsync();
                if (SearchSignal == null)
                    return;

                if (SearchText.IsNullOrWhiteSpace())
                {
                    Dispatcher.Run(() => State.FilteredLeftPaneItems = State.LeftPaneItems);
                    continue;
                }

                string searchText = SearchText;

                await Task.Delay(200);

                if (searchText == previousSearchText)
                    continue;

                if (searchText != SearchText)
                    continue;

                if (State.IsProcessing)
                    continue;

                List<LeftPaneItemViewModel> filteredMods = new();

                foreach (LeftPaneItemViewModel item in State.LeftPaneItems)
                {
                    if (searchText != SearchText)
                    {
                        filteredMods = null;
                        break;
                    }

                    if (item.Type != EPageType.Mod || item.Mod.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        filteredMods.Add(item);
                }

                if (filteredMods == null || filteredMods.Count == 0)
                    continue;

                Dispatcher.Run(() => State.FilteredLeftPaneItems = new(filteredMods));

                previousSearchText = searchText;
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Background mod search task has stopped.");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }

    private async Task CycleBackgroundImagesAsync()
    {
        Image[] backgroundControls = new Image[]
        {
            BackgroundA,
            BackgroundB,
        };

        string[] backgroundUris = new string[]
        {
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game01.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt01.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game02.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt02.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game03.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt03.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game04.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt04.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game05.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt05.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game06.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt06.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game07.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt07.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game08.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt08.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game09.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/FanArt09.jpg",
            $"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/Game10.jpg",
        };

        Bitmap forcedBackground = null;

        Bitmap[] backgroundImages =
            backgroundUris.Select(LoadBitmap).Where(bg => bg != null).ToArray();

        Image curr = BackgroundA;
        Image next = BackgroundB;
        curr.Opacity = 0.0;
        next.Opacity = 1.0;
        next.Source = AppSettings.IsBackgroundEnabled ? backgroundImages[0] : null;

        // So we don't get the starting image as the nest one, we
        // subtract the index with -1, and increment it later on:
        int backgroundIdx =
            new Random((int)DateTime.UtcNow.Ticks).Next(backgroundImages.Length - 1);

        // Cycle though background images:
        while (true)
        {
            // Switch:
            curr.Source = next.Source;
            curr.Opacity = 1.0;
            next.Opacity = 0.0;

            backgroundIdx += State.MoveToPrevBackgroundImage ? -1 : 1;
            backgroundIdx =
                backgroundIdx < 0 ? backgroundImages.Length - 1 :
                backgroundIdx >= backgroundImages.Length ? 0 :
                backgroundIdx;

            next.Source = backgroundImages[backgroundIdx];

            State.MoveToNextBackgroundImage = false;
            State.MoveToPrevBackgroundImage = false;

            // Wait:
            for (int i = 0; i < 80 && forcedBackground == State.ForcedBackground && AppSettings.IsBackgroundEnabled && !State.MoveToNextBackgroundImage && !State.MoveToPrevBackgroundImage; ++i)
            {
                await Task.Yield();
                await Task.Delay(100);
                await Task.Yield();
            }

            // Pause:
            while (State.IsProcessing || !AppSettings.IsBackgroundEnabled || BackgroundDarknessSlider.Value >= 0.999 || State.ForcedBackground != null)
            {
                if (!AppSettings.IsBackgroundEnabled)
                    curr.Source = forcedBackground = null;
                else if (forcedBackground != State.ForcedBackground)
                    curr.Source = forcedBackground = State.ForcedBackground;
                await Task.Yield();
                await Task.Delay(200);
                await Task.Yield();
                if (State.MoveToNextBackgroundImage)
                    break;
            }
            forcedBackground = null;

            // Cross fade (2 second):
            int steps = State.MoveToNextBackgroundImage || State.MoveToPrevBackgroundImage ? 5 : 20;
            for (int i = 0; i < steps && forcedBackground == State.ForcedBackground && AppSettings.IsBackgroundEnabled; ++i)
            {
                double progress = i / (double)steps;
                curr.Opacity = 1.0 - progress; // fade out
                next.Opacity = progress; // fade in
                await Task.Yield();
                await Task.Delay(50);
                await Task.Yield();
            }

            // Final value: avoids floating point precision issues
            curr.Opacity = 0.0;
            next.Opacity = 1.0;
        }
    }

    private Bitmap LoadBitmap(string uri)
    {
        try
        {
            using (Stream stream = AssetLoader.Open(new(uri)))
                return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }

    private void OnEmptyAreaClicked(object sender, TappedEventArgs e)
    {
        if (sender is not Grid grid)
            return;

        Point pos = e.GetPosition(grid);
        if (pos.Y <= ButtonsGrid.RowDefinitions[0].Height.Value)
            return;

        // Select the "Airlock" left pane item:
        LeftPaneListBox.SelectedIndex = GetLeftPaneIndexOf(EPageType.Airlock);
    }

    private void ToggleCollapse(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        int airlockIndex = GetLeftPaneIndexOf(EPageType.Airlock);
        if (AppSettings.IsLeftPaneCollapsed && airlockIndex >= 0 && LeftPaneListBox.SelectedIndex == airlockIndex)
        {
            int navigationConsoleIndex = GetLeftPaneIndexOf(EPageType.NavigationConsole);
            if (navigationConsoleIndex >= 0)
                LeftPaneListBox.SelectedIndex = navigationConsoleIndex;
        }
        vm.ToggleCollapseLeftPane();
    }


    private async void MoveModUp(object sender, PointerPressedEventArgs e)
    {
        if (State.IsProcessing || !SearchText.IsNullOrEmpty())
            return;
        if (DataContext is not MainWindowViewModel vm)
            return;
        LeftPaneItemViewModel selected = State.SelectedLeftPaneItem;
        if (selected?.Type != EPageType.Mod)
            return;
        State.LeftPaneItems.MoveUp(selected, (int)Enum.GetValues<EPageType>().Max());
        State.FilteredLeftPaneItems.MoveUp(selected, (int)Enum.GetValues<EPageType>().Max());
        State.SelectedLeftPaneItem = selected;
        await vm.MoveModUpAsync(selected.Mod);
    }

    private async void MoveModDown(object sender, PointerPressedEventArgs e)
    {
        if (State.IsProcessing || !SearchText.IsNullOrEmpty())
            return;
        if (DataContext is not MainWindowViewModel vm)
            return;
        LeftPaneItemViewModel selected = State.SelectedLeftPaneItem;
        if (selected?.Type != EPageType.Mod)
            return;
        State.LeftPaneItems.MoveDown(selected);
        State.FilteredLeftPaneItems.MoveDown(selected);
        State.SelectedLeftPaneItem = selected;
        await vm.MoveModDownAsync(selected.Mod);
    }





    private void OnSliderPointerEntered(object sender, PointerEventArgs e) => Dispatcher.Run(async () =>
    {
        if (ToolTip.GetIsOpen(BackgroundDarknessSlider))
            return;
        ToolTip.SetIsOpen(BackgroundDarknessSlider, BackgroundDarknessSlider.IsPointerOver);
    });

    private void OnSliderPointerExited(object sender, PointerEventArgs e) => Dispatcher.Run(async () =>
    {
        ToolTip.SetIsOpen(BackgroundDarknessSlider, false);
    });

    private void OnSliderPointerPressed(object sender, PointerPressedEventArgs e) => Dispatcher.Run(async () =>
    {
        if (ToolTip.GetIsOpen(BackgroundDarknessSlider))
            return;
        ToolTip.SetIsOpen(BackgroundDarknessSlider, BackgroundDarknessSlider.IsPointerOver);
    });

    private void OnSliderPointerReleased(object sender, PointerReleasedEventArgs e) => Dispatcher.Run(async () =>
    {
        ToolTip.SetIsOpen(BackgroundDarknessSlider, false);
    });

    private async void OnSliderPropertyChangedAsync(object sender, AvaloniaPropertyChangedEventArgs e) => Dispatcher.Run(() =>
    {
        if (e.Property != Slider.ValueProperty)
            return;
        if (ToolTip.GetIsOpen(BackgroundDarknessSlider))
            return;
        ToolTip.SetIsOpen(BackgroundDarknessSlider, BackgroundDarknessSlider.IsPointerOver);
    });

    private async void EnabledListedModsAsync(object sender, PointerPressedEventArgs e)
    {
        if (State.IsProcessing)
            return;
        foreach (LeftPaneItemViewModel item in State.FilteredLeftPaneItems)
        {
            if (item.Type != EPageType.Mod)
                continue;
            item.Mod.IsEnabled = true;
            ModValuesRepositoryService repo = new(Paths.Data, Log);
            await repo.TrySaveModValuesAsync(item.Mod.Data, true, default);
        }
        State.UpdateModIds();
        State.UpdateModConflicts();
        State.UpdateModDependencies();
    }

    private async void DisabledListedModsAsync(object sender, PointerPressedEventArgs e)
    {
        if (State.IsProcessing)
            return;
        foreach (LeftPaneItemViewModel item in State.FilteredLeftPaneItems)
        {
            if (item.Type != EPageType.Mod)
                continue;
            item.Mod.IsEnabled = false;
            ModValuesRepositoryService repo = new(Paths.Data, Log);
            await repo.TrySaveModValuesAsync(item.Mod.Data, true, default);
        }
        State.UpdateModIds();
        State.UpdateModConflicts();
        State.UpdateModDependencies();
    }

    private void ResetSearch(object sender, PointerPressedEventArgs e) =>
        SearchTextbox.Text = string.Empty;

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb)
            return;
        SearchText = tb.Text;
        SearchSignal?.Release();
    }

}
