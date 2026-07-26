using CommunityToolkit.Mvvm.ComponentModel;
using SH.Modding.Models;
using System.Threading;

namespace SH.Launcher.ViewModels;

public partial class ConsoleWindowViewModel : ViewModelBase
{
    public CancellationTokenSource CTS { get; } = new();

    [ObservableProperty]
    private string _Title;

    public ConsoleWindowViewModel()
    {
        Title = $@"{SpaceHavenLauncher.Name} {SpaceHavenLauncher.Version} (console)";
    }
}
