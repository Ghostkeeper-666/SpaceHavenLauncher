using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SH.Framework.Logging;
using SH.Launcher.Extensions;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Repositories;
using SH.Launcher.Views;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;

    [ObservableProperty]
    private string _Title;

    [ObservableProperty]
    private string _SearchText;

    public MainWindowViewModel()
    {
        Title = string.Empty;

        State.LearningComputerPage = new();
        State.NavigationConsolePage = new();
        State.SystemCorePage = new();
        State.AirlockPage = new();

        State.CurrentPage = State.NavigationConsolePage;
        State.LeftPaneItems.Clear();
        State.FilteredLeftPaneItems.Clear();

        LeftPaneItem navigationConsole = new(EPageType.NavigationConsole, null);
        State.LeftPaneItems.Add(navigationConsole);
        State.FilteredLeftPaneItems.Add(navigationConsole);

        LeftPaneItem systemCore = new(EPageType.SystemCore, null);
        State.LeftPaneItems.Add(systemCore);
        State.FilteredLeftPaneItems.Add(systemCore);

        LeftPaneItem learningComputer = new(EPageType.LearningComputer, null);
        State.LeftPaneItems.Add(learningComputer);
        State.FilteredLeftPaneItems.Add(learningComputer);

        LeftPaneItem airlock = new(EPageType.Airlock, null);
        State.LeftPaneItems.Add(airlock);
        State.FilteredLeftPaneItems.Add(airlock);
    }

    public async Task OnViewLoadedAsync(CancellationToken ct)
    {
        if (await InitializeSettings(ct))
        {
            // Move app to the previously used monitor:
            try
            {
                int currentMonitor = MainWindow.Window.GetMonitorIndex();
                if (currentMonitor != State.AppSettings.MonitorIndex)
                    MainWindow.Window.RestoreToMonitor(State.AppSettings.MonitorIndex);
            }
            catch (Exception ex) { Log?.Debug(ex); }

            // Try to maximize the window:
            try { MainWindow.Window.WindowState = Avalonia.Controls.WindowState.Maximized; }
            catch (Exception ex) { Log?.Debug(ex); }

            // Also automatically initialize build system:
            State.DispatchQueue.TryEnqueue(() => State.NavigationConsolePage.InitializeBuildSystemAsync(false));
        }
        await FadeOutLogo();
    }

    private async Task<bool> InitializeSettings(CancellationToken ct)
    {
        try
        {
            bool success = true;
            success &= await InitializePathSettings(ct);
            success &= await InitializeAppSettings(ct);
            return success;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }

    private async Task<bool> InitializePathSettings(CancellationToken ct)
    {
        try
        {
            PathSettingsRepositoryService repo = new(Log);
            PathData pathData = repo.TryLoad() ?? new();
            bool success = repo.ResolveAll(pathData);
            if (!success)
                Log.Error("Unable to locate all required paths. Please set them on System Core. Afterwards, re-initialize the Navigation Console");
            if (!await repo.TrySave(pathData, ct))
                Log.Error($@"Unable to save file, please check filesystem write permissions for ""{pathData.PathSettingsPath}""");
            State.Paths = new PathViewModel(pathData);
            Paths.PropertyChanged += Paths_PropertyChanged;
            Title = $"{SpaceHavenLauncher.Name}  {SpaceHavenLauncher.Version}";
            return success;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }

    private void Paths_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PathViewModel.SpaceHavenName):
            case nameof(PathViewModel.SpaceHavenVersion):
                Title = $"{SpaceHavenLauncher.Name} {SpaceHavenLauncher.Version}";
                if (Paths.SpaceHavenVersion != null)
                    Title += $"  -  {Paths.SpaceHavenName} {Paths.SpaceHavenVersion}";
                return;

            default:
                return;
        }
    }

    private async Task<bool> InitializeAppSettings(CancellationToken ct)
    {
        try
        {
            AppSettingsRepositoryService repo = new(Paths.Data, Log);
            
            AppSettingsData data = await repo.TryLoadOrCreateAsync(ct);
            if (data != null)
                State.Log.SetLogLevel(data.LogVerbosity.ToLogLevel());
            
            if(data == null)
            {
                Log.Error($@"Unable to load or create application settings => please check for write permissions in ""{Paths.WorkDir}""");
                data = AppSettingsData.GetDefault(); // continue anyway
            }
            State.AppSettings.SetData(data);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }

    private async Task FadeOutLogo()
    {
        for (int i = 0; i < 20; ++i)
        {
            State.LogoOpacity = State.LogoOpacity * 0.666;
            await Task.Delay(50);
        }
        State.LogoOpacity = 0.0;
        State.LogoIsVisible = false;
    }

    [RelayCommand]
    internal void ToggleCollapseLeftPane()
    {
        AppSettings.IsLeftPaneCollapsed = !AppSettings.IsLeftPaneCollapsed;
    }

    public async Task MoveModUpAsync(ModViewModel mod)
    {
        try
        {
            if (State.IsProcessing)
                return;
            if (State.SelectedLeftPaneItem.Mod == mod)
                State.Mods.MoveUp(State.SelectedLeftPaneItem.Mod);
            ModValuesRepositoryService repo = new(Paths.Data, Log);
            await repo.TrySaveModSorting(State.Mods.Select(m => m.Data), default);
        }
        catch (Exception ex) { Log.Error(ex); }
    }

    public async Task MoveModDownAsync(ModViewModel mod)
    {
        try
        {
            if (State.IsProcessing)
                return;
            if (State.SelectedLeftPaneItem.Mod == mod)
                State.Mods.MoveDown(State.SelectedLeftPaneItem.Mod);
            ModValuesRepositoryService repo = new(Paths.Data, Log);
            await repo.TrySaveModSorting(State.Mods.Select(m => m.Data), default);
        }
        catch (Exception ex) { Log.Error(ex); }
    }


}
