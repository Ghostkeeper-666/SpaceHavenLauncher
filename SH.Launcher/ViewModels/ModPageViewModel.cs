using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Launcher.Core.Repositories;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class ModPageViewModel : ViewModelBase
{
    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;

    public ModViewModel Mod { get; }

    [ObservableProperty]
    private string _Title;

    [ObservableProperty]
    private IBrush _TitleBrush;

    [ObservableProperty]
    private ObservableCollection<ModVariableViewModel> _Variables;

    [ObservableProperty]
    private string _SearchText;

    [ObservableProperty]
    private string _ModIdConflictText;

    [ObservableProperty]
    private string _IncompatibleModsText;

    public ModPageViewModel(ModViewModel mod)
    {
        Mod = mod;
        Variables = Mod.Variables;
        Mod.PropertyChanged += Mod_PropertyChanged;
        UpdateVisuals();
    }

    private void Mod_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Mod.IsEnabled):
                Mod.Data.IsEnabled = Mod.IsEnabled;
                Mod.Data.IsModified = true;
                UpdateVisuals();
                break;

            case nameof(Mod.HasModIdError):
            case nameof(Mod.HasAppCompatibilityError):
            case nameof(Mod.HasSpaceHavenCompatibilityError):
            case nameof(Mod.HasModConflictsError):
            case nameof(Mod.HasModDependenciesError):
                UpdateVisuals();
                break;
            default:
                return;
        }
    }

    private void UpdateVisuals()
    {
        // DISABLED
        if (!Mod.IsEnabled)
        {
            TitleBrush = Brushes.Gray;
            Title = $"{Mod.Name}  {Mod.Version}  (DISABLED)";
        }
        else
        {
            // ERRORS
            List<string> errors = new();
            if (Mod.HasAppCompatibilityError || Mod.HasSpaceHavenCompatibilityError)
                errors.Add("INCOMPATIBLE");
            if (Mod.HasModIdError || Mod.HasModConflictsError)
                errors.Add("MOD CONFLICT");
            if (Mod.HasModDependenciesError)
                errors.Add("DEPENDENCY ERROR");

            if (errors.Count > 0)
            {
                Title = $"{Mod.Name}  {Mod.Version}  ({errors.JoinToString(", ")})";
                TitleBrush = Brushes.OrangeRed;
            }
            else // No errors
            {
                TitleBrush = Mod.ForegroundColor;
                Title = $"{Mod.Name}  {Mod.Version}";
            }
        }
    }



    [RelayCommand]
    public async Task TitleClicked()
    {
        Mod.IsEnabled = !Mod.IsEnabled;
        ModValuesRepositoryService repo = new(Paths.Data, Log);
        await repo.TrySaveModValuesAsync(Mod.Data, true, default);
        State.UpdateModIds();
        State.UpdateModConflicts();
        State.UpdateModDependencies();
    }

    [RelayCommand]
    public async Task DirectoryClicked()
    {
        await Task.WhenAll(
            Framework.IO.OS.OpenDirectoryAsync(Mod.Directory, Log),
            State.CopyToClipboardAsync(Mod.Directory)
        );
    }

    [RelayCommand]
    public void UseOriginal()
    {
        foreach (ModVariableViewModel vm in Variables)
        {
            vm.CurrentValue = vm.OriginalValue;
            vm.UpdateForegrounds();
        }
    }

    [RelayCommand]
    public void UseSuggested()
    {
        foreach (ModVariableViewModel vm in Variables)
        {
            vm.CurrentValue = vm.SuggestedValue;
            vm.UpdateForegrounds();
        }
    }

    [RelayCommand]
    public void UsePrevious()
    {
        foreach (ModVariableViewModel vm in Variables)
        {
            vm.CurrentValue = vm.SuggestedValue;
            vm.UpdateForegrounds();
        }
    }

    public async Task SaveModValues(bool onlyModified, CancellationToken ct)
    {
        ModValuesRepositoryService repo = new(Paths.Data, Log);
        await repo.TrySaveModValuesAsync(Mod.Data, onlyModified, ct);
    }






}
