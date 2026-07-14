using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using SH.Launcher.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class LearningComputerViewModel : ViewModelBase
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private readonly Bitmap BackgroundImage = ImageX.FromAssetLoader($"avares://{SpaceHavenLauncher.AssemblyName}/Assets/Images/Backgrounds/LearningComputer.jpg");
    [ObservableProperty]
    private LearningItemViewModel _SelectedItem;

    [ObservableProperty]
    private ObservableCollection<LearningItemViewModel> _LearningItems = [];

    [ObservableProperty]
    private string _MarkdownText;

    private readonly MainWindowViewModel Parent;



    public LearningComputerViewModel(MainWindowViewModel parent)
    {
        Parent = parent?? throw new ArgumentNullException(nameof(parent));
    }



    public async Task Start()
    {
        State.ForcedBackground = BackgroundImage;
        LearningItems.Clear();
        List<string> learningFiles = Paths.Data.LearningDir.GetFiles(ESearchOption.TopDir);
        foreach (string learningFile in learningFiles.OrderBy(path => path))
        {
            LearningItemViewModel learningItem = new(this, learningFile);
            LearningItems.Add(learningItem);
        }
        SelectedItem = LearningItems.FirstOrDefault();
    }

    async partial void OnSelectedItemChanged(LearningItemViewModel value) =>
        MarkdownText = await IOUtils.TryReadAllTextAsync(value?.Path, Log, default);

}
