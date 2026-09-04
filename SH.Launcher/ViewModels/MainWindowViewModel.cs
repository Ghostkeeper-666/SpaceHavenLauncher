using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Launcher.Extensions;
using SH.Launcher.ViewModels.Enums;
using SH.Launcher.Views;
using SH.Modding.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    public bool IsNewInstall { get; private set; }

    [ObservableProperty]
    private string _Title;

    [ObservableProperty]
    private string _SearchText;

    [ObservableProperty]
    private string _ToolTipText_EnableModsButton = "HOW TO ENABLE MODS: \n\nThis button ENABLES the mods which are visible in the list below \n\nTo individually ENABLE or DISABLE a mod, click on the mod's ★ STAR icon, OR click on the mod's TITLE in the mod page \n\nYou may also restrict the visible mods using the SEARCH BOX and then clicking on THIS BUTTON to ENABLE them all";

    [ObservableProperty]
    private string _ToolTipText_DisableModsButton = "HOW TO DISABLE MODS: \n\nThis button DISABLES the mods which are visible in the list below \n\nTo individually ENABLE or DISABLE a mod, click on the mod's ★ STAR icon, OR click on the mod's TITLE in the mod page \n\nYou may also restrict the visible mods using the SEARCH BOX and then clicking on THIS BUTTON to DISABLE them all";



    public MainWindowViewModel()
    {
        Title = string.Empty;

        State.LearningComputerPage = new(this);
        State.NavigationConsolePage = new(this);
        State.SystemCorePage = new(this);
        State.AirlockPage = new(this);

        State.LeftPaneItems.Clear();
        State.FilteredLeftPaneItems.Clear();

        LeftPaneItemViewModel navigationConsole = new(EPageType.NavigationConsole, null);
        State.LeftPaneItems.Add(navigationConsole);
        State.FilteredLeftPaneItems.Add(navigationConsole);

        LeftPaneItemViewModel systemCore = new(EPageType.SystemCore, null);
        State.LeftPaneItems.Add(systemCore);
        State.FilteredLeftPaneItems.Add(systemCore);

        LeftPaneItemViewModel learningComputer = new(EPageType.LearningComputer, null);
        State.LeftPaneItems.Add(learningComputer);
        State.FilteredLeftPaneItems.Add(learningComputer);

        LeftPaneItemViewModel airlock = new(EPageType.Airlock, null);
        State.LeftPaneItems.Add(airlock);
        State.FilteredLeftPaneItems.Add(airlock);
    }



    public async Task OnViewLoadedAsync(CancellationToken ct)
    {
        if (await InitializeSettings(ct))
        {
            bool isNewAppVersion = State.AppSettings.PreviousAppVersion != SpaceHavenLauncher.Version;
            if (isNewAppVersion)
                Log.Warn($"NEW VERSION DETECTED: Space Haven Launcher {SpaceHavenLauncher.Version}  (previously: {State.AppSettings.PreviousAppVersion})");

            State.CurrentPage = IsNewInstall ? State.LearningComputerPage : State.NavigationConsolePage;

            // Move app to the previously used monitor:
            try
            {
                int currentMonitor = MainWindow.Window.GetMonitorIndex();
                if (currentMonitor != State.AppSettings.MonitorIndex)
                    MainWindow.Window.RestoreToMonitor(State.AppSettings.MonitorIndex);
            }
            catch (Exception ex) { Log?.Debug(ex); }

            // Also automatically initialize:
            await State.InitializeAsync(forceReset: isNewAppVersion);

            // Reset JAVA arguments for each new version:
            if (isNewAppVersion)
            {
                State.AppSettings.JavaMainClass = State.DefaultJavaMainClass;
                State.AppSettings.JavaVMArgs = State.DefaultJavaVMArgs;
            }
        }
        await FadeOutLogo();
    }

    private async Task<bool> InitializeSettings(CancellationToken ct)
    {
        try
        {
            bool success = true;
            success &= await InitializePathSettings(ct);
            State.FileLogger.SetPath(IOUtils.CombineAsOSPath(Paths.Data.AppLogPath));

            success &= await InitializeAppSettings(ct);
            return success;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
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
                Log.Error("Please set the DIRECTORIES on SYSTEM CORE tab and re-initialize the NAVIGATION CONSOLE afterwards. If you need some help, go to LEARNING COMPUTER tab", "tab://LearningComputer");
            if (!await repo.TrySaveAsync(pathData, ct))
                Log.Error($@"Unable to load/save path settings file, please make sure you have set write permissions for ""{SpaceHavenLauncher.WorkDir}""", SpaceHavenLauncher.WorkDir);
            State.Paths = new PathViewModel(pathData);
            State.PropertyChanged += State_PropertyChanged;
            Title = $"{SpaceHavenLauncher.Name} {SpaceHavenLauncher.Version.Major}.{SpaceHavenLauncher.Version.Minor}.{SpaceHavenLauncher.Version.Build}";
            return success;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }

    private void State_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(State.SpaceHavenVersion):
                Title = $"{SpaceHavenLauncher.Name} {SpaceHavenLauncher.Version.Major}.{SpaceHavenLauncher.Version.Minor}.{SpaceHavenLauncher.Version.Build}";
                if (State.SpaceHavenVersion != null)
                    Title += $"  -  {SpaceHavenConstants.SpaceHavenName} {State.SpaceHavenVersion}";
                return;

            default:
                return;
        }
    }




    private async Task<bool> InitializeAppSettings(CancellationToken ct)
    {
        try
        {
            IsNewInstall = !IOUtils.FileExists(Paths.Data.ApplicationSettingsPath);
            AppSettingsRepositoryService repo = new(Paths.Data, Log);
            AppSettingsData data = await repo.TryLoadOrCreateAsync(ct);
            if (data == null)
            {
                Log.Error($@"Unable to load/save application settings file, please make sure you set have write permissions for ""{SpaceHavenLauncher.WorkDir}""");
                data = AppSettingsData.GetDefault();
            }
            State.Log.SetLogLevel(data.LogVerbosity.ToLogLevel());
            State.AppSettings.SetData(data);
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
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
            await repo.TrySaveModSortingAsync(State.Mods.Select(m => m.Data), default);
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
            await repo.TrySaveModSortingAsync(State.Mods.Select(m => m.Data), default);
        }
        catch (Exception ex) { Log.Error(ex); }
    }

}
