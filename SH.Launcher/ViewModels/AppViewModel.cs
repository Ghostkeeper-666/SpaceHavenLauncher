using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Content;
using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Launcher.ViewModels.Enums;
using SH.Modding;
using SH.Modding.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class AppViewModel : ObservableObject
{
    static AppViewModel()
    {
        State = new();
        State.SetLogReplacements(null);
    }

    public static AppViewModel State { get; } // Singleton
    public static DispatchQueue Dispatcher { get; } = new(); // Singleton

    // LOG:
    public FileLogger Log { get; } = new FileLogger(SpaceHavenLauncher.LogPath);

    [ObservableProperty]
    private ObservableCollection<LogMessage> _LogHistory = [];
    public IReadOnlyList<(string, string)> LogReplacements { get; set; } = [];

    // PATH Settings:
    [ObservableProperty]
    private PathViewModel _Paths;

    // App Settings (variables to be persisted):
    [ObservableProperty]
    private AppSettingsViewModel _AppSettings = new();

    // Space Haven:
    [ObservableProperty]
    private VersionInfo _SpaceHavenVersion;

    // BACKGROUND:
    [ObservableProperty]
    private Bitmap _ForcedBackground = null;

    public bool MoveToNextBackgroundImage { get; set; }
    public bool MoveToPrevBackgroundImage { get; set; }

    // EXECUTION STATE:
    public readonly SemaphoreSlim Semaphore = new(1, 1);
    public bool IsProcessing => Semaphore.CurrentCount <= 0;

    public bool IsInitializing => InitializationCTS != null;
    public CancellationTokenSource InitializationCTS { get; set; }

    public bool IsExporting => ExportCTS != null;
    public CancellationTokenSource ExportCTS { get; set; }

    public bool IsLaunching => LaunchCTS != null;
    public CancellationTokenSource LaunchCTS { get; set; }

    public bool IsSpaceHavenRunning { get; set; }


    // PAGES:
    [ObservableProperty]
    private ViewModelBase _CurrentPage;

    [ObservableProperty]
    private LearningComputerViewModel _LearningComputerPage;

    [ObservableProperty]
    private NavigationConsoleViewModel _NavigationConsolePage;

    [ObservableProperty]
    private SystemCoreViewModel _SystemCorePage;

    [ObservableProperty]
    private AirlockViewModel _AirlockPage;

    [ObservableProperty]
    private Dictionary<string, ModPageViewModel> _ModPages = [];

    [ObservableProperty]
    private ObservableCollection<ModViewModel> _Mods = [];

    // STATUS BAR:
    [ObservableProperty]
    private string _StatusBarText;

    [ObservableProperty]
    private IBrush _StatusBarForecolor = Brushes.LightCyan;

    // SPLASH LOGO:
    [ObservableProperty]
    private bool _LogoIsVisible = true;

    [ObservableProperty]
    private double _LogoOpacity = 1.0;

    // LEFT PANE:
    [ObservableProperty]
    private GridLength _LeftPaneWidth;

    [ObservableProperty]
    private IBrush _LeftPaneBackgroundColor = new SolidColorBrush(Color.Parse("#3F000000"));

    [ObservableProperty]
    private int _CollapseIconRotation;

    [ObservableProperty]
    private LeftPaneItemViewModel _SelectedLeftPaneItem;

    [ObservableProperty]
    private ObservableCollection<LeftPaneItemViewModel> _LeftPaneItems = [];

    [ObservableProperty]
    private ObservableCollection<LeftPaneItemViewModel> _FilteredLeftPaneItems = [];

    // TEMPLATE INFO:
    [ObservableProperty]
    private EGamePlatform _GamePlatform;
    [ObservableProperty]
    private string _DefaultJavaVMArgs;
    [ObservableProperty]
    private string _DefaultJavaMainClass;

    public IProgressInfo BackupProgress { get; } = new ProgressInfo(nameof(BackupProgress));
    public IProgressInfo TemplateProgress { get; } = new ProgressInfo(nameof(TemplateProgress));
    public IProgressInfo CacheProgress { get; } = new ProgressInfo(nameof(CacheProgress));
    public IProgressInfo LoadModsProgress { get; } = new ProgressInfo(nameof(LoadModsProgress));
    public IProgressInfo InitializeProgress { get; }

    [ObservableProperty]

    private EControlState _InitializationState = EControlState.Standby;



    public AppViewModel()
    {
        AppSettings.PropertyChanged -= PersistentSettings_PropertyChanged;
        AppSettings.PropertyChanged += PersistentSettings_PropertyChanged;
        UpdateLeftPanelIsCollapsed();
        InitializeProgress = new ProgressInfo("Initialization",
        [
            (BackupProgress, 10),
            (TemplateProgress, 50),
            (CacheProgress, 10),
            (LoadModsProgress, 30),
        ]);
        BackupProgress.Max = 10;
        TemplateProgress.Max = 10;
        CacheProgress.Max = 10;
        LoadModsProgress.Max = 10;
    }

    public void SetLogReplacements(PathData data) =>
        Log?.Replacements = data?.GetLogReplacements(); // obfuscates absolute directories in log entries


    private void PersistentSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettingsViewModel.IsLeftPaneCollapsed))
            UpdateLeftPanelIsCollapsed();
    }

    private void UpdateLeftPanelIsCollapsed()
    {
        CollapseIconRotation = AppSettings.IsLeftPaneCollapsed ? 180 : 0;
        LeftPaneWidth = AppSettings.IsLeftPaneCollapsed ? new GridLength(48) : new GridLength(432);
    }

    partial void OnSelectedLeftPaneItemChanged(LeftPaneItemViewModel value)
    {
        switch (value?.Type)
        {
            case null:
                return;

            case EPageType.LearningComputer:
                CurrentPage = LearningComputerPage;
                break;

            case EPageType.NavigationConsole:
                CurrentPage = NavigationConsolePage;
                break;

            case EPageType.SystemCore:
                CurrentPage = SystemCorePage;
                break;

            case EPageType.Airlock:
                CurrentPage = AirlockPage;
                AppSettings.IsLeftPaneCollapsed = true;
                break;

            case EPageType.Mod:
                CurrentPage = ModPages[value.Mod.UniqueName];
                break;

            default:
                throw new NotImplementedException($"{nameof(EPageType)} = {value.Type}");
        }
    }

    public void CopyToClipboardAsync(string text)
    {
        if (text.IsNullOrWhiteSpace())
            return;

        // Fire and forget:
        Dispatcher.Run(async () =>
        {
            try
            {
                TopLevel topLevel = TopLevel.GetTopLevel(
                    (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow ??
                    (Application.Current?.ApplicationLifetime as ISingleViewApplicationLifetime)?.MainView
                );

                IClipboard clipboard = topLevel?.Clipboard;
                if (clipboard is null)
                    return;

                await clipboard.SetTextAsync(text);
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }
        });
    }

    public void UpdateModIds()
    {
        // Clear all:
        foreach (ModViewModel mod in Mods)
            mod.ModIdErrors.Clear();

        // Set:
        foreach (ModViewModel sourceMod in Mods.Where(m => m.IsEnabled))
        {
            foreach (ModViewModel targetMod in Mods.Where(m => m.IsEnabled && m != sourceMod))
            {
                if (sourceMod.FinalId != targetMod.FinalId)
                    continue;
                sourceMod.ModIdErrors.Add(targetMod);
                targetMod.ModIdErrors.Add(sourceMod);
            }
        }

        // Update observable properties:
        foreach (ModViewModel mod in Mods)
        {
            if (mod.ModIdErrors.Count > 0)
            {
                mod.HasModIdError = mod.ModIdErrors.Count > 0;
                mod.ModIdErrorText = $"MOD ID CONFLICT WITH:\n\n{mod.ModIdErrors.Select(m => $"- {m.DisplayName}").JoinToString("\n")}";
            }
            else
            {
                mod.HasModIdError = false;
                mod.ModIdErrorText = null;
            }
        }
    }

    public void UpdateModConflicts()
    {
        // Clear all:
        foreach (ModViewModel mod in Mods)
            mod.ModConflictsErrors.Clear();

        // Set:
        foreach (ModViewModel sourceMod in Mods)
        {
            foreach (ModViewModel targetMod in Mods.Where(m => m.IsEnabled && m != sourceMod))
            {
                if (!sourceMod.Data.ModConflicts.MatchAny(targetMod.UniqueName, targetMod.Version) && !sourceMod.Data.ModConflicts.MatchAny(targetMod.DisplayName, targetMod.Version))
                    continue;
                sourceMod.ModConflictsErrors.Add(targetMod);
                targetMod.ModConflictsErrors.Add(sourceMod);
            }
        }

        // Update observable properties:
        foreach (ModViewModel mod in Mods)
        {
            if (mod.ModConflictsErrors.Count > 0)
            {
                mod.HasModConflictsError = mod.ModConflictsErrors.Count > 0;
                mod.ModConflictsErrorText = $"INCOMPATIBLE MODS:\n\n{mod.ModConflictsErrors.Select(m => $"- {m.DisplayName}").JoinToString("\n")}";
            }
            else
            {
                mod.HasModConflictsError = false;
                mod.ModConflictsErrorText = null;
            }
        }
    }

    public void UpdateModDependencies()
    {
        // Clear all:
        foreach (ModViewModel mod in Mods)
        {
            mod.MissingDependencies.Clear();
            mod.CircularDependencyChain.Clear();
            mod.AllDependencies.Clear();
            mod.AllReferences.Clear();
            mod.DirectDependencies.Clear();
            mod.DirectReferences.Clear();
        }

        // Set direct dependencies and references:
        foreach (ModViewModel sourceMod in Mods)
        {
            foreach (VersionCompatibility dependency in sourceMod.Data.ModDependencies.Items)
            {
                ModViewModel targetMod =
                    Mods.FirstOrDefault(t => dependency.Match(t.DisplayName, t.Version)) ??
                    Mods.FirstOrDefault(t => dependency.Match(t.UniqueName, t.Version));

                if (targetMod == null)
                {
                    sourceMod.MissingDependencies.Add(dependency);
                    continue;
                }
                if (!targetMod.IsEnabled)
                    sourceMod.MissingDependencies.Add(dependency);
                sourceMod.DirectDependencies.Add(targetMod);
                targetMod.DirectReferences.Add(sourceMod);
            }
        }

        // Set indirect dependencies:
        foreach (ModViewModel mod in Mods)
            foreach (ModViewModel other in mod.DirectDependencies)
                AddIndirectDependencies(mod, other);

        // Set indirect references:
        foreach (ModViewModel mod in Mods)
            foreach (ModViewModel other in mod.DirectReferences)
                AddIndirectReferences(mod, other);

        // Set circular references:
        foreach (ModViewModel mod in Mods)
            mod.CircularDependencyChain.AddRange(mod.AllDependencies.Where(mod.AllReferences.Contains));

        // Update observable properties:
        foreach (ModViewModel mod in Mods)
        {
            StringBuilder sb = new();

            // Dependencies:
            if (mod.AllDependencies.Count > 0)
            {
                string directDependencies = mod.DirectDependencies.Select(d => $"- {d.DisplayName} {d.Version}\n").JoinToString();
                sb.AppendLine($"DIRECTLY REQUIRES \n{directDependencies}");
                string indirectDependencies = mod.AllDependencies.Where(d => !mod.DirectDependencies.Contains(d)).Select(d => $"- {d.DisplayName} {d.Version}\n").JoinToString();
                if (!indirectDependencies.IsNullOrEmpty())
                    sb.AppendLine($"INDIRECTLY REQUIRES \n{indirectDependencies}");
            }

            // References:
            if (mod.AllReferences.Count > 0)
            {
                string directReferences = mod.DirectReferences.Select(d => $"- {d.DisplayName} {d.Version}\n").JoinToString();
                sb.AppendLine($"DIRECTLY REQUIRED BY \n{directReferences}");
                string indirectReferences = mod.AllReferences.Where(d => !mod.DirectReferences.Contains(d)).Select(d => $"- {d.DisplayName} {d.Version}\n").JoinToString();
                if (!indirectReferences.IsNullOrEmpty())
                    sb.AppendLine($"INDIRECTLY REQUIRED BY \n{indirectReferences}");
            }

            // Missing:
            string allMissing = mod.
                AllDependencies.Where(d => !d.IsEnabled).Select(d => $"- {d.DisplayName} {d.Version}\n")
                .Concat(
                    mod.AllDependencies
                    .SelectMany(d => d.MissingDependencies)
                    .Where(missing =>
                        !Mods.Any(m => m.DisplayName == missing.Name) &&
                        !Mods.Any(m => m.UniqueName == missing.Name))
                    .Select(missing => $"- {missing}\n"))
                .OrderBy(str => str).Distinct().JoinToString();

            if (!allMissing.IsNullOrEmpty())
                sb.AppendLine($"NOT Found / NOT Enabled \n{allMissing}");

            // Circular:
            string circular = mod.CircularDependencyChain.Select(d => $"- {d.DisplayName} {d.Version}\n").JoinToString();
            if (!circular.IsNullOrEmpty())
                sb.AppendLine($"CIRCULAR REFERENCES \n{circular}");

            // Summary:
            if (sb.Length > 0)
            {
                mod.ModDependenciesSummaryText = sb.ToString();
                mod.HasModDependenciesError = !allMissing.IsNullOrEmpty() || !circular.IsNullOrEmpty();
            }
            else
            {
                mod.ModDependenciesSummaryText = null;
                mod.HasModDependenciesError = false;
            }
        }
    }

    private void AddIndirectDependencies(ModViewModel mod, ModViewModel current)
    {
        if (current == mod)
            return; // circular dependency
        if (!mod.AllDependencies.Add(current))
            return; // already added before
        foreach (ModViewModel child in current.DirectDependencies)
            AddIndirectDependencies(mod, child); // add children
    }

    private void AddIndirectReferences(ModViewModel mod, ModViewModel current)
    {
        if (current == mod)
            return; // circular reference
        if (!mod.AllReferences.Add(current))
            return; // already added before
        foreach (ModViewModel child in current.DirectReferences)
            AddIndirectReferences(mod, child); // add children
    }

    public async Task<bool> InitializeAsync(bool forceReset)
    {
        // Skip while processing other stuff:
        if (IsProcessing && !IsInitializing)
            return true;

        // Semaphore:
        await Semaphore.WaitAsync();
        try
        {
            Log.Warn($"Starting a {(forceReset ? "FULL" : "QUICK")} initialization...");

            using CancellationTokenSource cts = new();
            InitializationCTS = cts;
            CancellationToken ct = cts.Token;

            InitializationState = EControlState.Standby; // required to trigger events on the next line:
            InitializationState = EControlState.Running;
            if (!await InitializeInternalAsync(forceReset, ct))
            {
                if (forceReset)
                {
                    InitializationState = EControlState.Error;
                    return false;
                }

                // Try to force a reset first:
                Log.Warn("Retrying a FULL initialization...");
                if (!await InitializeInternalAsync(true, ct))
                {
                    Log.Error("Initialization has failed");
                    InitializationState = EControlState.Error;
                    return false;
                }
            }

            // LOAD MODS:
            if (!await TryReloadModsAsync(ct))
                return false;

            // Done.
            Log.Success($"Initialization is complete", Paths.WorkDir);
            InitializationState = EControlState.Ready;
            return true;
        }
        catch (OperationCanceledException)
        {
            Log.Warn($"Initialization was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            InitializationState = EControlState.Error;
            return false;
        }
        finally
        {
            InitializationCTS = null;
            Semaphore.Release();
        }
    }

    private async Task<bool> InitializeInternalAsync(bool forceReset, CancellationToken ct)
    {
        try
        {
            InitializeProgress.Reset();
            InitializeProgress.Start();

            InitializationService svc = new(Paths.Data, Log, BackupProgress, TemplateProgress, CacheProgress);
            InitializationData initializationData = await svc.InitializeAsync(forceReset, ct);
            if (initializationData == null)
                return false;

            GamePlatform = initializationData.GamePlatform;

            DefaultJavaVMArgs = initializationData.JavaVMArgs;
            if (forceReset || AppSettings.JavaVMArgs.IsNullOrWhiteSpace())
                AppSettings.JavaVMArgs = initializationData.JavaVMArgs;

            DefaultJavaMainClass = initializationData.JavaMainClass;
            if (forceReset || AppSettings.JavaMainClass.IsNullOrWhiteSpace())
                AppSettings.JavaMainClass = initializationData.JavaMainClass;

            SpaceHavenVersion = initializationData.SpaceHavenVersion;

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            return false;
        }
    }

    private async Task<bool> TryReloadModsAsync(CancellationToken ct)
    {
        try
        {
            LoadModsProgress.Reset();
            LoadModsProgress.Start();


            // Clear:
            Log.Debug("Resetting mods...");
            Mods.Clear();
            ModPages.Clear();
            List<LeftPaneItemViewModel> leftPaneItemsToRemove = LeftPaneItems.Where(item => item.Type == EPageType.Mod).ToList();
            foreach (LeftPaneItemViewModel item in leftPaneItemsToRemove)
            {
                LeftPaneItems.Remove(item);
                FilteredLeftPaneItems.Remove(item);
            }


            // Load mods, then sort them:
            Log.Debug("Loading mods...");
            ModRepositoryService modRepoSvc = new(Paths.Data, Log);
            ModValuesRepositoryService valuesRepoSvc = new(Paths.Data, Log);
            OrderedDictionary<string, ModData> mods = await modRepoSvc.TryLoadMods(ct, LoadModsProgress);
            if (mods == null)
                return false;
            mods = await valuesRepoSvc.TryLoadModSortingAsync(mods, ct);


            // Load mod values:
            Log.Debug("Reading mod variable values...");
            foreach (ModData mod in mods.Values)
            {
                if (await valuesRepoSvc.TryLoadCurrentModValuesAsync(mod, ct))
                {
                    // Try to also read previous version values:
                    await valuesRepoSvc.TryLoadPreviousModValuesAsync(mod, false, ct);
                }
                else
                {
                    // Try to read previous values for using them as current values:
                    // (defaults to 'suggested value' if no previous value is defined)
                    await valuesRepoSvc.TryLoadPreviousModValuesAsync(mod, true, ct);

                    // Save current version values:
                    await valuesRepoSvc.TrySaveModValuesAsync(mod, false, ct);
                }
            }


            // Now add the loaded mods:
            Log.Debug("Adding loaded mods...");
            foreach (ModData modData in mods.Values)
            {
                ct.ThrowIfCancellationRequested();

                ModViewModel mod = new(modData, mods.Values);
                Mods.Add(mod);
                ModPages.Add(mod.UniqueName, new ModPageViewModel(mod));
                LeftPaneItems.Add(new LeftPaneItemViewModel(EPageType.Mod, mod));
                await Task.Yield();
            }
            FilteredLeftPaneItems = new(LeftPaneItems);


            // Update mod conflicts:
            Log.Debug("Refreshing mod page data...");
            UpdateModIds();
            UpdateModConflicts();
            UpdateModDependencies();


            // Done.
            Log.Debug("Mods loaded");
            LoadModsProgress.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Info(ex);
            return false;
        }
    }

    public void OpenLink(string link, ILogger log)
    {
        Dispatcher.Run(async () =>
        {
            // Cleanup:
            link = link?.Trim('"').Trim('\'').Trim();
            if (link.IsNullOrWhiteSpace())
                return;

            // Normal links (dirs, files, internet links):
            if (!link.StartsWith("app://"))
            {
                await OS.OpenLinkAsync(link, log);
                return;
            }

            // Links to something within this application:
            string[] parts = link.Substring(6).Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (!Enum.TryParse(parts.FirstOrDefault() ?? string.Empty, true, out EPageType page))
                return;

            switch (page)
            {
                case EPageType.LearningComputer:
                case EPageType.NavigationConsole:
                case EPageType.SystemCore:
                case EPageType.Airlock:
                    LeftPaneItemViewModel item = State.LeftPaneItems.FirstOrDefault(item => item.Type == page);
                    if (item != null) State.SelectedLeftPaneItem = item;
                    return;

                case EPageType.Mod:
                    if (parts.Length < 2)
                        return;
                    LeftPaneItemViewModel mod = State.LeftPaneItems.FirstOrDefault(item => item.Type == EPageType.Mod && (item?.Mod?.UniqueName?.Replace(" ", string.Empty).Equals(parts[1].Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase) ?? false));
                    if (mod != null) State.SelectedLeftPaneItem = mod;
                    return;

                default:
                    return;
            }
        });
    }




}
