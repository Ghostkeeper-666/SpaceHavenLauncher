using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SH.Content.Modding;
using SH.Content;
using SH.Launcher.Extensions;

namespace SH.Launcher.ViewModels;

public partial class ModViewModel : ObservableObject, IComparable<ModViewModel>
{
    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public IBrush DefaultTextColor { get; } = Brushes.LightCyan;

    public ModViewModel(ModData data, IEnumerable<ModData> otherMods)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        otherMods ??= [];

        IsEnabled = data.IsEnabled;
        Name = data.Name;
        InfoXmlDescription = data.InfoXmlDescription;
        MarkdownDescription = data.MarkdownDescription;
        Version = data.Version;
        ModId = data.ModId;
        AutoId = data.AutoId;
        CustomId = data.CustomId;
        FinalId =
            CustomId >= ModAutoId.MinValue && CustomId <= ModAutoId.MaxValue ? CustomId :
            ModId != 0 ? ModId :
            AutoId;
        Author = data.Author;
        Directory = data.Directory;
        Background = data.BackgroundImagePath;

        AppCompatibility = data.AppCompatibility.ToDisplayString("\r\n") ?? "(any)";
        HasAppCompatibilityError = !data.AppCompatibility.MatchAll(Paths.AppName, Paths.AppVersion);

        SpaceHavenCompatibility = data.SpaceHavenCompatibility.ToDisplayString("\r\n") ?? "(any)";
        HasSpaceHavenCompatibilityError = !data.SpaceHavenCompatibility.MatchAll(Paths.SpaceHavenName, Paths.SpaceHavenVersion);

        AllModConflictsText = data.ModConflicts.ToDisplayString("\r\n") ?? "(none)";
        AllModDependenciesText = data.ModDependencies.ToDisplayString("\r\n") ?? "(none)";

        if (!data.BackgroundImagePath.IsNullOrWhiteSpace() && File.Exists(data.BackgroundImagePath))
            try { BackgroundImage = new(data.BackgroundImagePath); }
            catch (Exception ex) { Log.Debug(ex); }

        if (!data.ForegroundColor.IsNullOrWhiteSpace())
            try { ForegroundColor = new SolidColorBrush(Color.Parse(data.ForegroundColor)); } catch { }
        SetThemeForecolor();

        foreach (VarData modVariable in data.Variables)
            Variables.Add(new ModVariableViewModel(this, modVariable));

        XmlLibraryFiles = data.XmlLibraryFilePaths.Count <= 0 ? string.Empty : $"XML library files:\n\n{data.XmlLibraryFilePaths.Select(path => $"- {path.Substring(data.XmlLibraryDirectory.Length + 1)}").JoinToString("\n")}";
        XmlPatchFiles = data.XmlPatchFilePaths.Count <= 0 ? string.Empty : $"XML patch files:\n\n{data.XmlPatchFilePaths.Select(path => $"- {path.Substring(data.XmlPatchesDirectory.Length + 1)}").JoinToString("\n")}";
        AudioFiles = data.AudioFilePaths.Count <= 0 ? string.Empty : $"Audio files:\n\n{data.AudioFilePaths.Select(path => $"- {path.Substring(data.AudioDirectory.Length + 1)}").JoinToString("\n")}";
        TextureFiles = data.TextureFilePaths.Count <= 0 ? string.Empty : $"Texture files:\n\n{data.TextureFilePaths.Select(path => $"- {path.Substring(data.TexturesDirectory.Length + 1)}").JoinToString("\n")}";
        JarFiles = data.JavaFilePaths.Count <= 0 ? string.Empty : $"JAR files:\n\n{data.JavaFilePaths.Select(path => $"- {path.Substring(data.Directory.Length + 1)}").JoinToString("\n")}";

        XmlLibraryFilesWidth = new(XmlLibraryFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        XmlPatchFilesWidth = new(XmlPatchFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        AudioFilesWidth = new(AudioFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        TextureFilesWidth = new(TextureFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        JarFilesWidth = new(JarFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
    }

    public void SetThemeForecolor()
    {
        // Custom for known modders:
        if (Author.StartsWith("ghostkeeper", StringComparison.OrdinalIgnoreCase))
        {
            ForegroundColor ??= Brushes.MediumSpringGreen;
        }
        else if (Author.StartsWith("paperfox", StringComparison.OrdinalIgnoreCase))
        {
            ForegroundColor ??= Brushes.Orange;
        }
        else if (Author.Equals("sub", StringComparison.OrdinalIgnoreCase)
            || Author.Equals("subzero", StringComparison.OrdinalIgnoreCase)
            || Author.Equals("sub-zero", StringComparison.OrdinalIgnoreCase))
        {
            ForegroundColor ??= Brushes.Cyan;
        }
        else if (Author.Contains("fuklaw", StringComparison.OrdinalIgnoreCase))
        {
            ForegroundColor ??= Brushes.Gold;
        }

        // Fallback:
        ForegroundColor ??= Brushes.Gold;
    }

    public void SetThemeBackground()
    {
        State.ForcedBackground = BackgroundImage;

        // If background is not defined by the mod, use default backgrounds for known Space Haven modders:
        if (Author.StartsWith("ghostkeeper", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/bg-Ghostkeeper.jpg");
        else if (Author.StartsWith("paperfox", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/bg-PaperFox.jpg");
        else if (Author.Equals("sub", StringComparison.OrdinalIgnoreCase) || Author.Equals("subzero", StringComparison.OrdinalIgnoreCase) || Author.Equals("sub-zero", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/bg-SubZero.jpg");
        else if (Author.Contains("fuklaw", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/bg-Fuklaw.jpg");
        else if (Author.Contains("chewday", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/bg-Chewday.jpg");
        else if (Author.Contains("r4v4g3", StringComparison.OrdinalIgnoreCase) || Author.Contains("r0xx0r3r", StringComparison.OrdinalIgnoreCase) || Name.StartsWith("Customizer") || Name.StartsWith("Furry Haven"))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/bg-Ravage.jpg");
        else if (Name.Contains("Bikini"))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/bg-Bikini.jpg");
    }

    partial void OnFinalIdChanged(int value)
    {
        CustomId = value >= ModAutoId.MinValue && value <= ModAutoId.MaxValue ? value : 0;
        if (Data.CustomId != CustomId)
        {
            Data.CustomId = CustomId;
            Data.IsModified = true;
        }
        IdTitle = CustomId == ModId ? "MOD-ID" : CustomId == AutoId ? "AUTO-ID" : "CUSTOM-ID";
        State.UpdateModIds();
    }

    public void ResetId() =>
        FinalId = ModId != 0 ? ModId : AutoId;

    public string IdHelp { get; } = $"This ID should be unique among all mods in your list. If not, you could potentially have a mod conflict. \n\nMOD IDs must be >= {ModAutoId.MinValue} and <= {ModAutoId.MaxValue}.\n\nFor savegames of the same series, you should keep this ID stable. \n\nManually assigning a new ID to the MOD is possible, but if the mod contains XML Patch or XML Library files, this will cause EXISTING instances of new entities introduced by that MOD to be removed from the savegame. \n\nJAVA mods usually don't suffer from such collateral effect, unless the mod mixes JAVA with XML Library or XML Patches.";

    public ModData Data { get; }

    [ObservableProperty]
    private ObservableCollection<ModVariableViewModel> _Variables = [];

    [ObservableProperty]
    private bool _IsEnabled;

    [ObservableProperty]
    private Bitmap _BackgroundImage;

    [ObservableProperty]
    private IBrush _ForegroundColor;

    [ObservableProperty]
    private string _Name;

    [ObservableProperty]
    private string _InfoXmlDescription;

    [ObservableProperty]
    private string _MarkdownDescription;
    public bool HasMarkdownDescription => !string.IsNullOrWhiteSpace(MarkdownDescription);
    partial void OnMarkdownDescriptionChanged(string value) => OnPropertyChanged(nameof(HasMarkdownDescription));



    [ObservableProperty]
    private VersionInfo _Version;


    [ObservableProperty]
    private int _ModId;

    [ObservableProperty]
    private int _AutoId;

    [ObservableProperty]
    private int _CustomId;

    [ObservableProperty]
    private string _IdTitle;

    [ObservableProperty]
    private int _FinalId;

    public bool HasCustomId => FinalId != ModId && FinalId != AutoId;

    [ObservableProperty]
    private bool _HasModIdError;
    [ObservableProperty]
    private string _ModIdErrorText;
    public SortedSet<ModViewModel> ModIdErrors { get; } = [];



    [ObservableProperty]
    private string _Author;

    [ObservableProperty]
    private string _Directory;

    [ObservableProperty]
    private string _Background;

    [ObservableProperty]
    private string _AppCompatibility;
    [ObservableProperty]
    private bool _HasAppCompatibilityError;

    [ObservableProperty]
    private string _SpaceHavenCompatibility;
    [ObservableProperty]
    private bool _HasSpaceHavenCompatibilityError;

    [ObservableProperty]
    private string _AllModDependenciesText;
    [ObservableProperty]
    private bool _HasModDependenciesError;
    [ObservableProperty]
    private string _ModDependenciesSummaryText;
    public SortedSet<VersionCompatibility> MissingDependencies { get; } = [];
    public SortedSet<ModViewModel> CircularDependencyChain { get; } = [];
    public SortedSet<ModViewModel> DirectDependencies { get; } = [];
    public SortedSet<ModViewModel> DirectReferences { get; } = [];
    public SortedSet<ModViewModel> AllDependencies { get; } = [];
    public SortedSet<ModViewModel> AllReferences { get; } = [];

    [ObservableProperty]
    private string _AllModConflictsText;
    [ObservableProperty]
    private bool _HasModConflictsError;
    [ObservableProperty]
    private string _ModConflictsErrorText;
    public SortedSet<ModViewModel> ModConflictsErrors { get; } = [];

    [ObservableProperty]
    private string _AudioFiles;
    [ObservableProperty]
    private string _TextureFiles;
    [ObservableProperty]
    private string _XmlLibraryFiles;
    [ObservableProperty]
    private string _XmlPatchFiles;
    [ObservableProperty]
    private string _JarFiles;

    [ObservableProperty]
    private GridLength _AudioFilesWidth;
    [ObservableProperty]
    private GridLength _TextureFilesWidth;
    [ObservableProperty]
    private GridLength _XmlLibraryFilesWidth;
    [ObservableProperty]
    private GridLength _XmlPatchFilesWidth;
    [ObservableProperty]
    private GridLength _JarFilesWidth;

    public override string ToString() => Name;

    public int CompareTo(ModViewModel other) => Name.CompareTo(other?.Name);
}
