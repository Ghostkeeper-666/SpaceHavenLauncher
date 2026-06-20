using Avalonia;
using Avalonia.Controls;
using SH.Framework.Logging;
using SH.Launcher.ViewModels;

namespace SH.Launcher.Views;

public partial class AirlockView : UserControl
{
    public SharedState State => SharedState.State;
    public ILogger Logger => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;

    public AirlockView() => InitializeComponent();

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
}