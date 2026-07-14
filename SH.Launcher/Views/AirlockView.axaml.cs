using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SH.Framework.Logging;
using SH.Launcher.ViewModels;

namespace SH.Launcher.Views;

public partial class AirlockView : UserControl
{
    public AppViewModel State => AppViewModel.State;
    public new DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    public AirlockView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not AirlockViewModel vm)
            return;
        vm.Start();
        base.OnAttachedToVisualTree(e);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not AirlockViewModel vm)
            return;
        vm.Stop();
        base.OnAttachedToVisualTree(e);
    }

    private void Background_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint((Visual)sender);
        if (point.Properties.IsRightButtonPressed)
            State.MoveToPrevBackgroundImage = true;
        else State.MoveToNextBackgroundImage = true;
    }
}