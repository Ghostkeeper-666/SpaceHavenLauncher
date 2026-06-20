using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Extensions;
using SH.Launcher.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class LearningComputerViewModel : ViewModelBase
{
    public LearningComputerViewModel() { }

    private readonly Bitmap BackgroundImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.ASSEMBLY_NAME}/Assets/Images/Backgrounds/LearningComputer.jpg");

    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;


    [ObservableProperty]
    private LearningItem _SelectedItem;

    [ObservableProperty]
    private ObservableCollection<LearningItem> _LearningItems = [];

    [ObservableProperty]
    private string _MarkdownText;

    public async Task Start()
    {
        State.ForcedBackground = BackgroundImage;
        LearningItems.Clear();
        string[] learningFiles = Directory.GetFiles(Paths.LearningDir, "*.md", SearchOption.TopDirectoryOnly);
        foreach (string learningFile in learningFiles.OrderBy(path => path))
        {
            LearningItem learningItem = new(this, learningFile);
            LearningItems.Add(learningItem);
        }
        SelectedItem = LearningItems.FirstOrDefault();
    }

    async partial void OnSelectedItemChanged(LearningItem value) =>
        MarkdownText = await IOUtils.TryReadAllTextAsync(value?.Path, Log, default);

}
