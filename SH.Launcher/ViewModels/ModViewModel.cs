using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Extensions;
using SH.Modding;
using SH.Modding.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SH.Launcher.ViewModels;

public partial class ModViewModel : ObservableObject, IComparable<ModViewModel>
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

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
    private string _UniqueName;

    [ObservableProperty]
    private string _DisplayName;

    [ObservableProperty]
    private string _InfoXmlDescription;

    [ObservableProperty]
    private string _MarkdownDescription;
    public bool HasMarkdownDescription =>
        !string.IsNullOrWhiteSpace(MarkdownDescription);
    partial void OnMarkdownDescriptionChanged(string value) =>
        OnPropertyChanged(nameof(HasMarkdownDescription));

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

    public bool HasCustomId =>
        FinalId != ModId && FinalId != AutoId;

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
    private string _SpriteFiles;
    [ObservableProperty]
    private string _SpriteSheetFiles;
    [ObservableProperty]
    private string _XmlLibraryFiles;
    [ObservableProperty]
    private string _XmlPatchFiles;
    [ObservableProperty]
    private string _JarFiles;

    [ObservableProperty]
    private GridLength _AudioFilesWidth;
    [ObservableProperty]
    private GridLength _SpritesFilesWidth;
    [ObservableProperty]
    private GridLength _SpriteSheetFilesWidth;
    [ObservableProperty]
    private GridLength _XmlLibraryFilesWidth;
    [ObservableProperty]
    private GridLength _XmlPatchFilesWidth;
    [ObservableProperty]
    private GridLength _JarFilesWidth;

    public string IdHelp { get; } =
        $"This ID must be unique among all enabled mods. If it's not, then there could be a potential mod conflict! \n\n- MOD IDs must be >= {ModAutoId.MinValue} and <= {ModAutoId.MaxValue}. \n\n- For savegames of the same series, you should keep this ID stable. \n\nManually assigning a new ID to the MOD is possible, but if the mod contains XML Patch or XML Library files, this will cause EXISTING instances of new entities introduced by that MOD to be removed from the savegame. \n\nJAVA mods usually don't suffer from such collateral effect, unless the mod mixes JAVA with XML Library or XML Patches.";

    [ObservableProperty]
    private IBrush _DefaultTextColor = Brushes.LightCyan;

    public ModViewModel(ModData data, IEnumerable<ModData> otherMods)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        otherMods ??= [];

        IsEnabled = data.IsEnabled;
        UniqueName = data.UniqueName;
        DisplayName = data.DisplayName;
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
        Directory = data.Dir;
        Background = data.BackgroundImagePath;

        AppCompatibility = data.AppCompatibility.ToDisplayString() ?? "(any)";
        HasAppCompatibilityError = !data.AppCompatibility.MatchAll(SpaceHavenLauncher.Name, SpaceHavenLauncher.Version);

        SpaceHavenCompatibility = data.SpaceHavenCompatibility.ToDisplayString() ?? "(any)";
        HasSpaceHavenCompatibilityError = !data.SpaceHavenCompatibility.MatchAll(SpaceHavenConstants.SpaceHavenName, State.SpaceHavenVersion);

        AllModConflictsText = data.ModConflicts.ToDisplayString() ?? "(none)";
        AllModDependenciesText = data.ModDependencies.ToDisplayString() ?? "(none)";

        if (IOUtils.FileExists(data.BackgroundImagePath))
            try { BackgroundImage = new(data.BackgroundImagePath); }
            catch (Exception ex) { Log.Debug(ex); }

        if (!data.ForegroundColor.IsNullOrWhiteSpace())
            try { ForegroundColor = new SolidColorBrush(Color.Parse(data.ForegroundColor)); } catch { }
        SetThemeForecolor();

        foreach (VarData modVariable in data.Variables)
            Variables.Add(new ModVariableViewModel(this, modVariable));

        XmlLibraryFiles = data.XmlLibraryRelativePaths.Count <= 0 ? string.Empty : $"XML library files:\n\n{data.XmlLibraryRelativePaths.Select(path => $"- {path}").JoinToString("\n")}";
        XmlPatchFiles = data.XmlPatchRelativePaths.Count <= 0 ? string.Empty : $"XML patch files:\n\n{data.XmlPatchRelativePaths.Select(path => $"- {path}").JoinToString("\n")}";
        AudioFiles = data.AudioRelativePaths.Count <= 0 ? string.Empty : $"Audio files:\n\n{data.AudioRelativePaths.Select(path => $"- {path}").JoinToString("\n")}";
        SpriteFiles = data.SpriteRelativePaths.Count <= 0 ? string.Empty : $"Sprite Textures:\n\n{data.SpriteRelativePaths.Select(path => $"- {path}").JoinToString("\n")}";
        SpriteSheetFiles = data.SpriteSheetRelativePaths.Count <= 0 ? string.Empty : $"Spritesheet Textures (CIM):\n\n{data.SpriteSheetRelativePaths.Select(path => $"- {path}").JoinToString("\n")}";
        JarFiles = data.JarRelativePaths.Count <= 0 ? string.Empty : $"JAR files:\n\n{data.JarRelativePaths.Select(path => $"- {path}").JoinToString("\n")}";

        XmlLibraryFilesWidth = new(XmlLibraryFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        XmlPatchFilesWidth = new(XmlPatchFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        AudioFilesWidth = new(AudioFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        SpritesFilesWidth = new(SpriteFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        SpriteSheetFilesWidth = new(SpriteSheetFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
        JarFilesWidth = new(JarFiles.IsNullOrWhiteSpace() ? 0.0 : 100.0);
    }



    public void SetThemeForecolor()
    {
        // Custom for known modders:
        if (Author.StartsWith("ghostkeeper", StringComparison.OrdinalIgnoreCase))
            ForegroundColor ??= Brushes.MediumSpringGreen;
        else if (Author.StartsWith("paperfox", StringComparison.OrdinalIgnoreCase))
            ForegroundColor ??= new SolidColorBrush(Color.Parse("#FF9F1F"));
        else if (Author.Equals("sub", StringComparison.OrdinalIgnoreCase)
            || Author.Equals("subzero", StringComparison.OrdinalIgnoreCase)
            || Author.Equals("sub-zero", StringComparison.OrdinalIgnoreCase))
            ForegroundColor ??= Brushes.Cyan;
        else if (Author.Contains("fuklaw", StringComparison.OrdinalIgnoreCase))
            ForegroundColor ??= new SolidColorBrush(Color.Parse("#FFBF00"));
        else if (Author.Contains("chewday", StringComparison.OrdinalIgnoreCase))
            ForegroundColor ??= new SolidColorBrush(Color.Parse("#7FBF3F"));
        else if (Author.Contains("r4v4g3", StringComparison.OrdinalIgnoreCase) || Author.Contains("r0xx0r3r", StringComparison.OrdinalIgnoreCase) || UniqueName.StartsWith("Customizer") || UniqueName.StartsWith("Furry Haven"))
            ForegroundColor ??= Brushes.LightCoral;
        else if (Author.Contains("Kaiser", StringComparison.OrdinalIgnoreCase))
            ForegroundColor ??= Brushes.DeepSkyBlue;
        else if (UniqueName.Contains("Bikini Babes", StringComparison.OrdinalIgnoreCase))
            ForegroundColor ??= Brushes.LightPink;

        // Fallback:
        ForegroundColor ??= Brushes.Gold;
    }

    public void SetThemeBackground()
    {
        State.ForcedBackground = BackgroundImage;

        // If background is not defined by the mod, use default backgrounds for known Space Haven modders:
        if (Author.StartsWith("ghostkeeper", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/bg-Ghostkeeper.jpg");
        else if (Author.StartsWith("paperfox", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/bg-PaperFox.jpg");
        else if (Author.Equals("sub", StringComparison.OrdinalIgnoreCase) || Author.Equals("subzero", StringComparison.OrdinalIgnoreCase) || Author.Equals("sub-zero", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/bg-SubZero.jpg");
        else if (Author.Contains("fuklaw", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/bg-Fuklaw.jpg");
        else if (Author.Contains("chewday", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/bg-Chewday.jpg");
        else if (Author.Contains("r4v4g3", StringComparison.OrdinalIgnoreCase) || Author.Contains("r0xx0r3r", StringComparison.OrdinalIgnoreCase) || UniqueName.StartsWith("Customizer") || UniqueName.StartsWith("Furry Haven"))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/bg-Ravage.jpg");
        else if (UniqueName.Contains("Bikini Babes", StringComparison.OrdinalIgnoreCase))
            State.ForcedBackground ??= ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/bg-Bikini.jpg");
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

    public int CompareTo(ModViewModel other) =>
        UniqueName.CompareTo(other?.UniqueName);

    public override string ToString() => UniqueName;
}
