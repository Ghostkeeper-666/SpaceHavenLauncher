using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.ComponentModel;

namespace SH.Launcher.ViewModels;

public partial class LearningItemViewModel : ObservableObject
{
    private readonly char[] TrimStartChars = ['-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'];

    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;
    
    [ObservableProperty]
    private string _Path;

    [ObservableProperty]
    private string _Label;

    [ObservableProperty]
    private IBrush _SelectedColor = Brushes.Black;

    private readonly LearningComputerViewModel Parent;



    public LearningItemViewModel(LearningComputerViewModel parent, string path)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Label = Path.GetFileNameWithoutExtension().TrimStart(TrimStartChars).Replace('_', ' ').Trim();
        Parent.PropertyChanged -= State_PropertyChanged;
        Parent.PropertyChanged += State_PropertyChanged;
    }



    private void State_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LearningComputerViewModel.SelectedItem))
            UpdateSelected();
    }

    private void UpdateSelected() =>
        SelectedColor = Parent?.SelectedItem == this ? new SolidColorBrush(Color.Parse("#FFC000")) : Brushes.Black;

    [RelayCommand]
    public void TextClicked()
    {
        if (State.IsProcessing)
            return;
        Parent?.SelectedItem = this;
    }

    public override string ToString() =>
        Label;
}
