using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using SH.Framework.Logging;
using SH.Launcher.ViewModels;
using System.Threading.Tasks;

namespace SH.Launcher.Views;

public partial class LearningComputerView : UserControl
{
    public AppViewModel State => AppViewModel.State;
    public new DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    public LearningComputerView()
    {
        InitializeComponent();
    }

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not LearningComputerViewModel vm)
            return;
        await vm.Start();
        base.OnAttachedToVisualTree(e);
    }

    private async void CopyLogStartAsync(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is not LearningComputerViewModel vm)
            return;
        CopyTextIcon.Foreground = Brushes.Gold;
        IClipboard clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        await clipboard?.SetTextAsync(vm?.MarkdownText ?? string.Empty);
    }

    private async void CopyLogEndAsync(object sender, PointerReleasedEventArgs e)
    {
        await Task.Delay(100);
        CopyTextIcon.Foreground = Brushes.DeepSkyBlue;
    }

}
