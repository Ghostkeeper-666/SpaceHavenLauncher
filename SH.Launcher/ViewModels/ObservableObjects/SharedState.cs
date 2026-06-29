using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class SharedState : ObservableObject
{
    public static SharedState State { get; } = new(); // Singleton


    public SharedState()
    {
        AppSettings.PropertyChanged -= UI_PropertyChanged;
        AppSettings.PropertyChanged += UI_PropertyChanged;
        UpdateLeftPanelIsCollapsed();
    }

    private void UI_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettingsViewModel.IsLeftPaneCollapsed))
            UpdateLeftPanelIsCollapsed();
    }

    private void UpdateLeftPanelIsCollapsed()
    {
        CollapseIconRotation = AppSettings.IsLeftPaneCollapsed ? 180 : 0;
        LeftPaneWidth = AppSettings.IsLeftPaneCollapsed ? new GridLength(48) : new GridLength(432);
    }


    // LOG:
    public ILogger Log { get; } = new Logger();

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
    private IBrush _LeftPaneBackgroundColor = Brushes.Transparent;

    [ObservableProperty]
    private int _CollapseIconRotation;

    [ObservableProperty]
    private LeftPaneItem _SelectedLeftPaneItem;

    [ObservableProperty]
    private ObservableCollection<LeftPaneItem> _LeftPaneItems = [];

    [ObservableProperty]
    private ObservableCollection<LeftPaneItem> _FilteredLeftPaneItems = [];

    partial void OnSelectedLeftPaneItemChanged(LeftPaneItem value)
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

    #region Helper methods

    public void CopyToClipboardAsync(string text)
    {
        if (text.IsNullOrWhiteSpace())
            return;

        // Fire and forget:
        DispatchQueue.TryEnqueue(async () =>
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





    public DispatchQueue DispatchQueue { get; } = new();

    public void Run(Action a)
    {
        if (a == null) return;
        if (Dispatcher.UIThread.CheckAccess())
        {
            try { a.Invoke(); }
            catch (Exception ex) { Log.Debug(ex); }
        }
        else
        {
            try { Dispatcher.UIThread.Invoke(a); }
            catch (Exception ex) { Log.Debug(ex); }
        }
    }

    public async Task RunAsync(Action a)
    {
        if (a == null) return;
        if (Dispatcher.UIThread.CheckAccess())
        {
            try { a.Invoke(); }
            catch (Exception ex) { Log.Debug(ex); }
        }
        else
        {
            try { await Dispatcher.UIThread.InvokeAsync(a); }
            catch (Exception ex) { Log.Debug(ex); }
        }
    }

    public T Run<T>(Func<T> f)
    {
        if (f == null) return default;
        if (Dispatcher.UIThread.CheckAccess())
        {
            try { return f.Invoke(); }
            catch (Exception ex) { Log.Debug(ex); return default; }
        }
        else
        {
            try { return Dispatcher.UIThread.Invoke(f); }
            catch (Exception ex) { Log.Debug(ex); return Dispatcher.UIThread.Invoke(() => default(T)); }
        }
    }

    public async Task<T> RunAsync<T>(Func<T> f)
    {
        if (f == null) return default;
        if (Dispatcher.UIThread.CheckAccess())
        {
            try { return f.Invoke(); }
            catch (Exception ex) { Log.Debug(ex); return default; }
        }
        else
        {
            try { return await Dispatcher.UIThread.InvokeAsync(f); }
            catch (Exception ex) { Log.Debug(ex); return Dispatcher.UIThread.Invoke(() => default(T)); }
        }
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

    #endregion
}
