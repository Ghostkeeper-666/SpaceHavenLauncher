using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SH.Framework.Logging;
using SH.Launcher.Extensions;
using SH.Launcher.Core.Models;
using SH.Launcher.Core.Repositories;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class LeftPaneItem : ObservableObject
{
    public LeftPaneItem(EPageType type, ModViewModel mod)
    {
        Type = type;
        Mod = Type == EPageType.Mod ? mod ?? throw new ArgumentNullException(nameof(mod)) : null;
        switch (Type)
        {
            case EPageType.LearningComputer:
                Image = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Icons/LearningComputerIcon.png");
                break;

            case EPageType.NavigationConsole:
                Image = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Icons/NavigationConsoleIcon.png");
                break;

            case EPageType.SystemCore:
                Image = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Icons/SystemCoreIcon.png");
                break;

            case EPageType.Airlock:
                Image = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Icons/AirlockIcon.png");
                break;

            case EPageType.Mod:
                EnabledImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Icons/ModIncludedIcon.png");
                ErrorImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Icons/ModErrorIcon.png");
                DisabledImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Icons/ModExcludedIcon.png");
                Image = EnabledImage;
                break;

            default:
                break;
        }

        UpdateVisuals();
        UpdateSelected();

        State.PropertyChanged -= State_PropertyChanged;
        State.PropertyChanged += State_PropertyChanged;

        if (Type == EPageType.Mod)
        {
            Mod.PropertyChanged -= Mod_PropertyChanged;
            Mod.PropertyChanged += Mod_PropertyChanged;
        }
    }

    public EPageType Type { get; }
    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;

    public ModViewModel Mod { get; }

    [ObservableProperty]
    private string _Label;

    [ObservableProperty]
    private IBrush _LabelColor;

    [ObservableProperty]
    private int _Strikethrough;

    [ObservableProperty]
    private int _Height = 36;

    [ObservableProperty]
    private IImage _Image;

    [ObservableProperty]
    private IBrush _SelectedColor = new SolidColorBrush(Color.Parse("#3fffff00"));

    private readonly Bitmap DisabledImage;
    private readonly Bitmap EnabledImage;
    private readonly Bitmap ErrorImage;

    private void State_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SharedState.SelectedLeftPaneItem))
            UpdateSelected();
    }

    private void UpdateSelected() =>
        SelectedColor =
            State?.SelectedLeftPaneItem == this ?
            new SolidColorBrush(Color.Parse("#7fffffff")) :
            Brushes.Transparent;

    private void Mod_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Mod.IsEnabled):
            case nameof(Mod.FinalId):
            case nameof(Mod.HasModIdError):
            case nameof(Mod.HasAppCompatibilityError):
            case nameof(Mod.HasSpaceHavenCompatibilityError):
            case nameof(Mod.HasModConflictsError):
            case nameof(Mod.HasModDependenciesError):
                UpdateVisuals();
                break;

            default:
                break;
        }
    }

    private void UpdateVisuals()
    {
        switch (Type)
        {
            case EPageType.LearningComputer:
                Label = "Learning Computer";
                LabelColor = Brushes.LightCyan;
                break;

            case EPageType.NavigationConsole:
                Label = "Navigation Console";
                LabelColor = Brushes.LightCyan;
                break;

            case EPageType.SystemCore:
                Label = "System Core";
                LabelColor = Brushes.LightCyan;
                break;

            case EPageType.Airlock:
                Label = "Airlock";
                LabelColor = Brushes.LightCyan;
                return;

            case EPageType.Mod:

                // DISABLED
                if (!Mod.IsEnabled)
                {
                    Label = Mod.Name;
                    LabelColor = new SolidColorBrush(Color.Parse("#bf7f888f"));
                    Image = DisabledImage;
                    Strikethrough = 3;
                    return;
                }

                // ERRORS
                if (Mod.HasModIdError || Mod.HasAppCompatibilityError || Mod.HasSpaceHavenCompatibilityError || Mod.HasModConflictsError || Mod.HasModDependenciesError)
                {
                    Label = Mod.Name;
                    LabelColor = Brushes.Tomato;
                    Image = ErrorImage;
                    Strikethrough = 0;
                    return;
                }

                // HIGHLIGHT MODS WITH CUSTOM IDS:
                if (Mod.HasCustomId)
                {
                    Label = $"{Mod.Name} (custom ID)";
                    LabelColor = Brushes.LemonChiffon;
                    Image = EnabledImage;
                    Strikethrough = 0;
                    break;
                }

                // OK
                Label = Mod.Name;
                LabelColor = Brushes.LightCyan;
                Image = EnabledImage;
                Strikethrough = 0;
                break;

            default:
                throw new NotImplementedException($"{nameof(EPageType)} = {Type}");
        }
    }

    [RelayCommand]
    public void TextClicked()
    {
        if (State.IsProcessing)
            return;
        State.SelectedLeftPaneItem = this;
    }


    [RelayCommand]
    public async Task IconClickedAsync()
    {
        if (State.IsProcessing)
            return;

        if (AppSettings.IsLeftPaneCollapsed)
        {
            State.SelectedLeftPaneItem = this;
            return;
        }

        switch (Type)
        {
            case EPageType.LearningComputer:
                State.SelectedLeftPaneItem = this;
                break;

            case EPageType.NavigationConsole:
                State.SelectedLeftPaneItem = this;
                break;

            case EPageType.SystemCore:
                State.SelectedLeftPaneItem = this;
                break;

            case EPageType.Airlock:
                State.SelectedLeftPaneItem = this;
                return;

            case EPageType.Mod:
                Mod.IsEnabled = !Mod.IsEnabled;
                ModValuesRepositoryService repo = new(Paths.Data, Log);
                await repo.TrySaveModValuesAsync(Mod.Data, true, default);
                State.UpdateModIds();
                State.UpdateModConflicts();
                State.UpdateModDependencies();
                break;

            default:
                return;
        }
    }

    public override string ToString() => Mod?.Name ?? Type.ToString();
}
