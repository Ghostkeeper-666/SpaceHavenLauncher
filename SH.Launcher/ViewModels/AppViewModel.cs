using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Services;
using SH.Launcher.ViewModels.Enums;
using SH.Modding;
using SH.Modding.ConfigJson;
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
    public static AppViewModel State { get; } = new(); // Singleton
    public static DispatchQueue Dispatcher { get; } = new(); // Singleton

    // LOG:
    public FileLogger Log { get; } = new FileLogger(null);

    [ObservableProperty]
    private ObservableCollection<LogMessage> _LogHistory = [];

    // PATH Settings:
    [ObservableProperty]
    private PathViewModel _Paths;

    // App Settings (variables to be persisted):
    [ObservableProperty]
    private AppSettingsViewModel _AppSettings = new();

    // BACKGROUND:
    [ObservableProperty]
    private Bitmap _ForcedBackground = null;

    public bool MoveToNextBackgroundImage { get; set; }
    public bool MoveToPrevBackgroundImage { get; set; }

    // EXECUTION STATE:
    public bool IsProcessing => IsInitializing || IsLaunching || IsExporting || IsSpaceHavenRunning;

    public bool IsInitializing => InitializeCTS != null;
    public bool IsLaunching => LaunchCTS != null;
    public bool IsExporting => ExportCTS != null;
    public bool IsSpaceHavenRunning { get; set; }

    public CancellationTokenSource InitializeCTS { get; set; }
    public CancellationTokenSource LaunchCTS { get; set; }
    public CancellationTokenSource ExportCTS { get; set; }

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
    private string _TemplateJavaVMArgs;
    [ObservableProperty]
    private string _TemplateJavaMainClass;

    public IProgressInfo BackupProgress { get; } = new ProgressInfo(nameof(BackupProgress));
    public IProgressInfo TemplateProgress { get; } = new ProgressInfo(nameof(TemplateProgress));
    public IProgressInfo CacheProgress { get; } = new ProgressInfo(nameof(CacheProgress));
    public IProgressInfo LoadModsProgress { get; } = new ProgressInfo(nameof(LoadModsProgress));
    public IProgressInfo InitializeProgress { get; }



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
                CurrentPage = ModPages[value.Mod.Name];
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
                mod.ModIdErrorText = $"MOD ID CONFLICT WITH:\n\n{mod.ModIdErrors.Select(m => $"- {m.Name}").JoinToString("\n")}";
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
                if (!sourceMod.Data.ModConflicts.MatchAny(targetMod.Name, targetMod.Version))
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
                mod.ModConflictsErrorText = $"INCOMPATIBLE MODS:\n\n{mod.ModConflictsErrors.Select(m => $"- {m.Name}").JoinToString("\n")}";
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
                ModViewModel targetMod = Mods.FirstOrDefault(t => dependency.Match(t.Name, t.Version));
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
                string directDependencies = mod.DirectDependencies.Select(d => $"- {d.Name} {d.Version}\n").JoinToString();
                sb.AppendLine($"DIRECTLY REQUIRES \n{directDependencies}");
                string indirectDependencies = mod.AllDependencies.Where(d => !mod.DirectDependencies.Contains(d)).Select(d => $"- {d.Name} {d.Version}\n").JoinToString();
                if (!indirectDependencies.IsNullOrEmpty())
                    sb.AppendLine($"INDIRECTLY REQUIRES \n{indirectDependencies}");
            }

            // References:
            if (mod.AllReferences.Count > 0)
            {
                string directReferences = mod.DirectReferences.Select(d => $"- {d.Name} {d.Version}\n").JoinToString();
                sb.AppendLine($"DIRECTLY REQUIRED BY \n{directReferences}");
                string indirectReferences = mod.AllReferences.Where(d => !mod.DirectReferences.Contains(d)).Select(d => $"- {d.Name} {d.Version}\n").JoinToString();
                if (!indirectReferences.IsNullOrEmpty())
                    sb.AppendLine($"INDIRECTLY REQUIRED BY \n{indirectReferences}");
            }

            // Missing:
            string allMissing = mod.
                AllDependencies.Where(d => !d.IsEnabled).Select(d => $"- {d.Name} {d.Version}\n")
                .Concat(mod.AllDependencies.SelectMany(d => d.MissingDependencies).Where(missing => !Mods.Any(m => m.Name == missing.Name)).Select(missing => $"- {missing}\n"))
                .OrderBy(str => str).Distinct().JoinToString();

            if (!allMissing.IsNullOrEmpty())
                sb.AppendLine($"NOT Found / NOT Enabled \n{allMissing}");

            // Circular:
            string circular = mod.CircularDependencyChain.Select(d => $"- {d.Name} {d.Version}\n").JoinToString();
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

    private readonly SemaphoreSlim InitializationSemaphore = new(1, 1);

    public async Task<bool> InitializeAsync(bool forceReset)
    {
        if (IsProcessing && !IsInitializing)
            return false;

        if (IsInitializing)
        {
            Log.Warn("Restarting initialization...");
            try { InitializeCTS?.Cancel(); } catch { }
        }

        await InitializationSemaphore.WaitAsync();

        try
        {
            using CancellationTokenSource cts = new();
            InitializeCTS = cts;
            CancellationToken ct = cts.Token;

            InitializeProgress.Reset();

            if (forceReset)
            {
                Log.Info($"Resetting {SpaceHavenLauncher.Name} files...");
                await IOUtils.TryDeleteDirectoryContentAsync(Paths.Data.TemplateDir, Log, ct);
                await IOUtils.TryDeleteDirectoryContentAsync(Paths.Data.BuildDir, Log, ct);
                await IOUtils.TryDeleteDirectoryContentAsync(Paths.Data.CacheDir, Log, ct);
            }

            DeploymentService svc = new(Paths.Data, Log);

            // BACKUP:
            Log.Debug($@"Performing backup of original files...", Paths.Data.BackupDir);
            if (!await svc.TryBackupOriginalAsync(InitializeCTS.Token, BackupProgress))
            {
                Log.Error("Unable to backup original JAR file", Paths.Data.BackupDir);
                return false;
            }
            BackupProgress?.Complete();
            Log.Success($"Original files backup is complete");

            // TEMPLATE:
            Log.Debug($@"Preparing template files...", Paths.Data.TemplateDir);
            if (!await Task.Run(() => svc.TryPrepareTemplateAsync(InitializeCTS.Token, TemplateProgress)))
            {
                Log.Error("Unable to prepare template JAR file", Paths.Data.TemplateDir);
                return false;
            }

            Log.Debug($@"Reading game platform...", Paths.Data.TemplateDir);
            JarRepositoryService jarSvc = new(Log);
            EGamePlatform? gamePlatform = await Task.Run(() => jarSvc.TryReadGamePlatformAsync(Paths.Data.BackupJarPath));
            if (gamePlatform == null || !gamePlatform.HasValue)
            {
                Log.Error("Unable to read game platform from template JAR file", Paths.Data.TemplateDir);
                return false;
            }
            GamePlatform = gamePlatform.Value;

            TemplateProgress?.Complete();
            Log.Success($"Template files are ready");

            Log.Debug($@"Reading java arguments from config.json file...", Paths.Data.TemplateDir);
            ConfigJsonFile configJson = await ConfigJsonFile.TryLoadAsync(Paths.Data.TemplateConfigJsonPath, Log, ct);
            if (configJson == null)
            {
                Log.Error("Unable to read config.json", Paths.Data.TemplateDir);
                return false;
            }

            TemplateJavaVMArgs = configJson.VMArgs.JoinToString(" ");
            AppSettings.JavaVMArgs = TemplateJavaVMArgs; // force update

            TemplateJavaMainClass = configJson.MainClass;
            AppSettings.JavaMainClass = TemplateJavaMainClass; // force update

            TemplateProgress?.Complete();
            Log.Success($"Template files are ready");

            // CACHE:
            Log.Debug($@"Reading version...", Paths.Data.TemplateDir);
            VersionParserService versionParser = new();
            if (!await Paths.TryReadSpaceHavenVersion(Log, InitializeCTS.Token))
            {
                Log.Error($"Unable to read {Paths.SpaceHavenName} version from template JAR file", Paths.Data.TemplateDir);
                return false;

            }
            Log.Success($"Detected {Paths.SpaceHavenName} version {Paths.SpaceHavenVersion}");

            Log.Debug($@"Validating mod cache...", Paths.Data.CacheDir);
            if (!await Task.Run(() => svc.TryValidateModifiedCacheAsync(InitializeCTS.Token, CacheProgress)))
            {
                Log.Error("Validation of mod cache has failed", Paths.Data.CacheDir);
                return false;
            }
            CacheProgress?.Complete();
            Log.Success($"Cached files are validated");

            // LOAD MODS:
            Log.Debug($@"Loading mods...");
            if (!await TryReloadModsAsync(ct, LoadModsProgress))
            {
                Log.Error("Unable to all load mods");
                return false;
            }
            LoadModsProgress?.Complete();

            // Done.
            Log.Success($"{SpaceHavenLauncher.Name} initialization is complete", Paths.WorkDir);
            return true;
        }
        catch (OperationCanceledException)
        {
            Log.Error($"{SpaceHavenLauncher.Name} initialization was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            return false;
        }
        finally
        {
            InitializationSemaphore.Release();
            InitializeCTS = null;
        }
    }

    private async Task<bool> TryReloadModsAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress?.Start();

            // Clear mod items:
            Mods.Clear();
            ModPages.Clear();

            // Prepare the list of items to remove, then remove, otherwise we get an exception:
            List<LeftPaneItemViewModel> leftPaneItemsToRemove = LeftPaneItems.Where(item => item.Type == EPageType.Mod).ToList();
            foreach (LeftPaneItemViewModel item in leftPaneItemsToRemove)
            {
                LeftPaneItems.Remove(item);
                FilteredLeftPaneItems.Remove(item);
            }

            // Load mods, then sort them:
            ModRepositoryService modRepoSvc = new(Paths.Data, Log);
            ModValuesRepositoryService valuesRepoSvc = new(Paths.Data, Log);
            OrderedDictionary<string, ModData> mods = await modRepoSvc.TryLoadMods(ct, progress);
            if (mods == null)
                return false;
            mods = await valuesRepoSvc.TryLoadModSortingAsync(mods, ct);

            // Load mod values:
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
            foreach (ModData modData in mods.Values)
            {
                ct.ThrowIfCancellationRequested();

                ModViewModel mod = new(modData, mods.Values);
                Mods.Add(mod);
                ModPages.Add(mod.Name, new ModPageViewModel(mod));
                LeftPaneItems.Add(new LeftPaneItemViewModel(EPageType.Mod, mod));
                await Task.Yield();
            }
            FilteredLeftPaneItems = new(LeftPaneItems);

            // Update mod conflicts:
            UpdateModIds();
            UpdateModConflicts();
            UpdateModDependencies();

            // Done.
            progress?.Complete();
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
                    LeftPaneItemViewModel mod = State.LeftPaneItems.FirstOrDefault(item => item.Type == EPageType.Mod && (item?.Mod?.Name?.Replace(" ", string.Empty).Equals(parts[1].Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase) ?? false));
                    if (mod != null) State.SelectedLeftPaneItem = mod;
                    return;

                default:
                    return;
            }
        });
    }

}
