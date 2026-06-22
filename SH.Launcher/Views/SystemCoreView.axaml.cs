using Avalonia;
using Avalonia.Controls;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Repositories;
using SH.Launcher.Services;
using SH.Launcher.ViewModels;
using System.IO;

namespace SH.Launcher.Views;

public partial class SystemCoreView : UserControl
{
    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;


    public SystemCoreView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not SystemCoreViewModel vm)
            return;
        vm.SetBackgroundImage();
        base.OnAttachedToVisualTree(e);
    }

    private void Resolve_SpaceHavenDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PathSettingsRepository repo = new(Log);
        Paths.SpaceHavenDir = repo.ResolveSpaceHavenDir(Paths.AppDir);
        if (!Paths.SpaceHavenDir.IsNullOrWhiteSpace())
        {
            Paths.SpaceHavenJarDir = repo.ResolveSpaceHavenJarDir(Paths.SpaceHavenDir);
            if (Paths.SpaceHavenJarDir != null && Paths.ClassicModsDir.IsNullOrWhiteSpace())
                Paths.ClassicModsDir = repo.ResolveClassicModsDir(Paths.SpaceHavenJarDir);
        }
    }

    private async void Pick_SpaceHavenDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);
        string dir = await picker.PickFolderAsync();
        if (dir.IsNullOrWhiteSpace())
            return;
        dir = Path.TrimEndingDirectorySeparator(dir);
        Paths.SpaceHavenDir = dir;

        PathSettingsRepository repo = new(Log);
        if (!Paths.SpaceHavenDir.IsNullOrWhiteSpace())
        {
            Paths.SpaceHavenJarDir = repo.ResolveSpaceHavenJarDir(Paths.SpaceHavenDir);
            if (Paths.SpaceHavenJarDir != null && Paths.ClassicModsDir.IsNullOrWhiteSpace())
                Paths.ClassicModsDir = repo.ResolveClassicModsDir(Paths.SpaceHavenJarDir);
        }
    }



    private void Resolve_SpaceHavenJarDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PathSettingsRepository repo = new(Log);
        Paths.SpaceHavenJarDir = repo.ResolveSpaceHavenJarDir(Paths.SpaceHavenDir);
    }

    private async void Pick_SpaceHavenJarDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);
        string dir = await picker.PickFolderAsync();
        if (dir.IsNullOrWhiteSpace())
            return;
        dir = Path.TrimEndingDirectorySeparator(dir);
        Paths.SpaceHavenJarDir = dir;
    }



    private void Resolve_SteamDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PathSettingsRepository repo = new(Log);
        Paths.SteamDir = repo.ResolveSteamDir(Paths.AppDir);
        if (!Paths.SteamDir.IsNullOrWhiteSpace())
            Paths.SteamModsDir = repo.ResolveSteamModsDir(Paths.SteamDir);
    }

    private async void Pick_SteamDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);
        string dir = await picker.PickFolderAsync();
        if (dir.IsNullOrWhiteSpace())
            return;
        dir = Path.TrimEndingDirectorySeparator(dir);
        Paths.SteamDir = dir;

        PathSettingsRepository repo = new(Log);
        if (!Paths.SteamDir.IsNullOrWhiteSpace())
            Paths.SteamModsDir = repo.ResolveSteamModsDir(Paths.SteamDir);
    }



    private void Resolve_SteamModsDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PathSettingsRepository repo = new(Log);
        Paths.SteamModsDir = repo.ResolveSteamModsDir(Paths.SteamDir);
    }

    private async void Pick_SteamModsDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);
        string dir = await picker.PickFolderAsync();
        if (dir.IsNullOrWhiteSpace())
            return;
        dir = Path.TrimEndingDirectorySeparator(dir);
        Paths.SteamModsDir = dir;
    }



    private void Resolve_ClassicModsDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PathSettingsRepository repo = new(Log);
        Paths.ClassicModsDir = repo.ResolveClassicModsDir(Paths.SpaceHavenJarDir);
    }

    private async void Pick_ClassicModsDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);
        string dir = await picker.PickFolderAsync();
        if (dir.IsNullOrWhiteSpace())
            return;
        dir = Path.TrimEndingDirectorySeparator(dir);
        Paths.ClassicModsDir = dir;
    }



    private void Resolve_ModValuesDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PathSettingsRepository repo = new(Log);
        Paths.ModValuesDir = repo.ResolveModValuesDir(Paths.WorkDir);
    }

    private async void Pick_ModValuesDir(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);
        string dir = await picker.PickFolderAsync();
        if (dir.IsNullOrWhiteSpace())
            return;
        dir = Path.TrimEndingDirectorySeparator(dir);
        Paths.ModValuesDir = dir;
    }

    private async void DonateAsync(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenHttpAsync("https://buymeacoffee.com/ghostkeepeb", Log));

    private async void Open_WorkDir(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.WorkDir, Log));
    private void Open_ExportDir(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.ExportDir, Log));

    private void Open_SpaceHavenDir(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.SpaceHavenDir, Log));
    private void Open_SpaceHavenJarDir(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.SpaceHavenJarDir, Log));

    private void Open_SteamDir(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.SteamDir, Log));
    private void Open_SteamModsDir(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.SteamModsDir, Log));
    private void Open_ClassicModsDir(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.ClassicModsDir, Log));

    private void Open_ModValuesDir(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.ModValuesDir, Log));

}
