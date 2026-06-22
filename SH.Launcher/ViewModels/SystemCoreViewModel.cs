using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Content.Enums;
using SH.Framework.Logging;
using SH.Launcher.Extensions;
using SH.Launcher.Models;
using System;
using System.Linq;

namespace SH.Launcher.ViewModels;

public partial class SystemCoreViewModel : ViewModelBase
{
    public SystemCoreViewModel() { }

    private readonly Bitmap BackgroundImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/SystemCore.jpg");

    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;

    public void SetBackgroundImage() =>
        State.ForcedBackground = BackgroundImage;

    [ObservableProperty]
    private ELogVerbosity[] _LogVerbosityValues = Enum.GetValues<ELogVerbosity>().ToArray();

    [ObservableProperty]
    private ELanguage[] _ExportXmlAnnotationLanguages = Enum.GetValues<ELanguage>().OrderByDescending(v => v).ToArray();

    [ObservableProperty]
    private EExportOption[] _ExportOptions = Enum.GetValues<EExportOption>().ToArray();

}
