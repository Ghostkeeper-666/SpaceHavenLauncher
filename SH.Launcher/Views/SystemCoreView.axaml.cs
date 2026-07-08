using Avalonia.Controls;
using Avalonia.Interactivity;
using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Services;
using SH.Launcher.Services;
using SH.Launcher.ViewModels;
using System;
using System.IO;

namespace SH.Launcher.Views;

public partial class SystemCoreView : UserControl
{
    public SharedState State => SharedState.State;
    public DispatchQueue DispatchQueue => State.DispatchQueue;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;


    public SystemCoreView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is SystemCoreViewModel vm)
                vm.OnActivated();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            if (DataContext is SystemCoreViewModel vm)
                vm.OnDeactivated();
        };
    }



    private void Resolve_SpaceHavenDirs(object sender, RoutedEventArgs e)
    {
        string spaceHavenJarDir = Paths.SpaceHavenJarDir.AsOSPath();
        string spaceHavenDir = Paths.SpaceHavenDir.AsOSPath();
        string classicModsDir = Paths.ClassicModsDir.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);

            // First make sure space haven JAR dir is correctly located:
            if (!IOUtils.DirectoryExists(Paths.SpaceHavenJarDir) || IOUtils.FileExists(Paths.Data.SpaceHavenJarPath))
            {
                string dir = svc.ResolveSpaceHavenJarDirFromAppDir(Paths.AppDir);
                if (!IOUtils.DirectoryExists(dir))
                    return;
                Paths.SpaceHavenJarDir = dir;
            }

            // Auto resolve space haven dir:
            if (!IOUtils.DirectoryExists(Paths.SpaceHavenDir) || !Paths.SpaceHavenJarDir.StartsWith(Paths.SpaceHavenDir, StringComparison.Ordinal))
            {
                string dir = svc.ResolveSpaceHavenDirFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
                if (IOUtils.DirectoryExists(dir))
                    Paths.SpaceHavenDir = dir;
            }

            // Auto resolve JRE path:
            if (spaceHavenJarDir != Paths.SpaceHavenJarDir || !IOUtils.FileExists(Paths.JREPath))
            {
                string path = svc.ResolveJREPathFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
                if (IOUtils.FileExists(path))
                    Paths.JREPath = path;
            }

            // Also resolve classic mods dir if it's not defined yet:
            if (!IOUtils.DirectoryExists(Paths.ClassicModsDir))
            {
                string dir = svc.ResolveClassicModsDirFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
                if (IOUtils.DirectoryExists(dir))
                    Paths.ClassicModsDir = dir;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (spaceHavenJarDir != Paths.SpaceHavenJarDir && IOUtils.DirectoryExists(Paths.SpaceHavenJarDir) ||
                spaceHavenDir != Paths.SpaceHavenDir && IOUtils.DirectoryExists(Paths.SpaceHavenDir) ||
                classicModsDir != Paths.ClassicModsDir && IOUtils.DirectoryExists(Paths.ClassicModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }

    private async void Pick_SpaceHavenDir(object sender, RoutedEventArgs e)
    {
        string spaceHavenJarDir = Paths.SpaceHavenJarDir.AsOSPath();
        string spaceHavenDir = Paths.SpaceHavenDir.AsOSPath();
        string classicModsDir = Paths.ClassicModsDir.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);
            FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);

            // Pick space haven dir:
            {
                string dir = await picker.PickFolderAsync();
                if (OS.IsMac) // we need the base dir, not the bundle one
                    dir = dir.RemoveSuffix("/spacehaven.app", StringComparison.OrdinalIgnoreCase);
                if (!IOUtils.DirectoryExists(dir))
                    return;
                Paths.SpaceHavenDir = dir;
            }

            // Auto resolve space haven JAR dir:
            if (!IOUtils.DirectoryExists(Paths.SpaceHavenJarDir) || !Paths.SpaceHavenJarDir.StartsWith(Paths.SpaceHavenDir, StringComparison.Ordinal))
            {
                string dir = svc.ResolveSpaceHavenJarDirFromSpaceHavenDir(Paths.SpaceHavenDir);
                if (IOUtils.DirectoryExists(dir))
                    Paths.SpaceHavenJarDir = dir;
            }

            // Auto resolve JRE path:
            if (spaceHavenJarDir != Paths.SpaceHavenJarDir || !IOUtils.FileExists(Paths.JREPath))
            {
                string path = svc.ResolveJREPathFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
                if (IOUtils.FileExists(path))
                    Paths.JREPath = path;
            }

            // Also resolve classic mods dir if it's not defined yet:
            if (!IOUtils.DirectoryExists(Paths.ClassicModsDir))
            {
                string dir = svc.ResolveClassicModsDirFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
                if (IOUtils.DirectoryExists(dir))
                    Paths.ClassicModsDir = dir;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (spaceHavenJarDir != Paths.SpaceHavenJarDir && IOUtils.DirectoryExists(Paths.SpaceHavenJarDir) ||
                spaceHavenDir != Paths.SpaceHavenDir && IOUtils.DirectoryExists(Paths.SpaceHavenDir) ||
                classicModsDir != Paths.ClassicModsDir && IOUtils.DirectoryExists(Paths.ClassicModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }


    private async void Pick_SpaceHavenJarDir(object sender, RoutedEventArgs e)
    {
        string spaceHavenJarDir = Paths.SpaceHavenJarDir.AsOSPath();
        string spaceHavenDir = Paths.SpaceHavenDir.AsOSPath();
        string classicModsDir = Paths.ClassicModsDir.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);
            FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);

            // Pick space haven JAR dir:
            {
                string dir = await picker.PickFolderAsync();
                if (!IOUtils.DirectoryExists(dir))
                    return;
                Paths.SpaceHavenJarDir = dir;
            }

            // Auto resolve space haven dir:
            if (!IOUtils.DirectoryExists(Paths.SpaceHavenDir) || !Paths.SpaceHavenJarDir.StartsWith(Paths.SpaceHavenDir, StringComparison.Ordinal))
            {
                string dir = svc.ResolveSpaceHavenDirFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
                if (IOUtils.DirectoryExists(dir))
                    Paths.SpaceHavenDir = dir;

            }

            // Auto resolve JRE path:
            if (spaceHavenJarDir != Paths.SpaceHavenJarDir || !IOUtils.FileExists(Paths.JREPath))
            {
                string path = svc.ResolveJREPathFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
                if (IOUtils.FileExists(path))
                    Paths.JREPath = path;
            }

            // Also resolve classic mods dir if it's not defined yet:
            if (!IOUtils.DirectoryExists(Paths.ClassicModsDir))
            {
                string dir = svc.ResolveClassicModsDirFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
                if (IOUtils.DirectoryExists(dir))
                    Paths.ClassicModsDir = dir;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (spaceHavenJarDir != Paths.SpaceHavenJarDir && IOUtils.DirectoryExists(Paths.SpaceHavenJarDir) ||
                spaceHavenDir != Paths.SpaceHavenDir && IOUtils.DirectoryExists(Paths.SpaceHavenDir) ||
                classicModsDir != Paths.ClassicModsDir && IOUtils.DirectoryExists(Paths.ClassicModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }



    private async void Resolve_SteamDir(object sender, RoutedEventArgs e)
    {
        string steamDir = Paths.SteamDir.AsOSPath();
        string steamModsDir = Paths.SteamModsDir.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);

            // Auto resolve steam dir:
            {
                string dir = svc.ResolveSteamDirFromAppDir(Paths.AppDir);
                if (!IOUtils.DirectoryExists(dir))
                    return;
                Paths.SteamDir = dir;
            }

            // Auto resolve steam mods dir:
            if (!IOUtils.DirectoryExists(Paths.SteamModsDir))
            {
                string dir = svc.ResolveSteamModsDirFromSteamDir(Paths.SteamDir);
                if (IOUtils.DirectoryExists(dir))
                    Paths.SteamModsDir = dir;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (steamDir != Paths.SteamDir && IOUtils.DirectoryExists(Paths.SteamDir) ||
                steamModsDir != Paths.SteamModsDir && IOUtils.DirectoryExists(Paths.SteamModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }

    private async void Pick_SteamDir(object sender, RoutedEventArgs e)
    {
        string steamDir = Paths.SteamDir.AsOSPath();
        string steamModsDir = Paths.SteamModsDir.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);
            FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);

            // Pick steam dir:
            {
                string dir = await picker.PickFolderAsync();
                if (!IOUtils.DirectoryExists(dir))
                    return;
                Paths.SteamDir = dir;
            }

            // Auto resolve steam mods dir:
            if (!IOUtils.DirectoryExists(Paths.SteamModsDir))
            {
                string dir = svc.ResolveSteamModsDirFromSteamDir(Paths.SteamDir);
                if (IOUtils.DirectoryExists(dir))
                    Paths.SteamModsDir = dir;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (steamDir != Paths.SteamDir && IOUtils.DirectoryExists(Paths.SteamDir) ||
                steamModsDir != Paths.SteamModsDir && IOUtils.DirectoryExists(Paths.SteamModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }



    private async void Resolve_SteamModsDir(object sender, RoutedEventArgs e)
    {
        string steamDir = Paths.SteamDir.AsOSPath();
        string steamModsDir = Paths.SteamModsDir.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);

            // Auto resolve steam mods dir:
            string dir = svc.ResolveSteamModsDirFromSteamDir(Paths.SteamDir);
            if (!IOUtils.DirectoryExists(dir))
                return;
            Paths.SteamModsDir = dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (steamModsDir != Paths.SteamModsDir && IOUtils.DirectoryExists(Paths.SteamModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }

    private async void Pick_SteamModsDir(object sender, RoutedEventArgs e)
    {
        string steamModsDir = Paths.SteamModsDir.AsOSPath();

        try
        {
            FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);

            // Auto resolve steam mods dir:
            string dir = await picker.PickFolderAsync();
            if (!IOUtils.DirectoryExists(dir))
                return;
            Paths.SteamModsDir = dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (steamModsDir != Paths.SteamModsDir && IOUtils.DirectoryExists(Paths.SteamModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }



    private async void Resolve_ClassicModsDir(object sender, RoutedEventArgs e)
    {
        string classicModsDir = Paths.ClassicModsDir.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);

            // Auto resolve classic mods dir:
            string dir = svc.ResolveClassicModsDirFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
            if (!IOUtils.DirectoryExists(dir))
                return;
            Paths.ClassicModsDir = dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (classicModsDir != Paths.ClassicModsDir && IOUtils.DirectoryExists(Paths.ClassicModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }

    private async void Pick_ClassicModsDir(object sender, RoutedEventArgs e)
    {
        string classicModsDir = Paths.ClassicModsDir.AsOSPath();

        try
        {
            FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);

            // Pick classic mods dir:
            string dir = await picker.PickFolderAsync();
            if (!IOUtils.DirectoryExists(dir))
                return;
            Paths.ClassicModsDir = dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (classicModsDir != Paths.ClassicModsDir && IOUtils.DirectoryExists(Paths.ClassicModsDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }



    private async void Resolve_ModValuesDir(object sender, RoutedEventArgs e)
    {
        string modValuesDir = Paths.ModValuesDir.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);

            // Auto resolve mod values dir:
            string dir = svc.ResolveModValuesDirFromWorkDir(Paths.WorkDir);
            if (!IOUtils.DirectoryExists(dir))
                return;
            Paths.ModValuesDir = dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (modValuesDir != Paths.ModValuesDir && IOUtils.DirectoryExists(Paths.ModValuesDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }

    private async void Pick_ModValuesDir(object sender, RoutedEventArgs e)
    {
        string modValuesDir = Paths.ModValuesDir.AsOSPath();

        try
        {
            FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);

            // Pick mod values dir:
            string dir = await picker.PickFolderAsync();
            if (!IOUtils.DirectoryExists(dir))
                return;
            Paths.ModValuesDir = dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (modValuesDir != Paths.ModValuesDir && IOUtils.DirectoryExists(Paths.ModValuesDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }

    private async void Resolve_JREPath(object sender, RoutedEventArgs e)
    {
        string jrePath = Paths.JREPath.AsOSPath();

        try
        {
            PathSettingsRepositoryService svc = new(Log);

            // Auto resolve JRE path:
            string path = svc.ResolveJREPathFromSpaceHavenJarDir(Paths.SpaceHavenJarDir);
            if (!IOUtils.FileExists(path))
                return;
            Paths.JREPath = path;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (jrePath != Paths.ModValuesDir && IOUtils.DirectoryExists(Paths.ModValuesDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }

    private async void Pick_JREPath(object sender, RoutedEventArgs e)
    {
        string jrePath = Paths.JREPath.AsOSPath();

        try
        {
            FolderPickerService picker = new(TopLevel.GetTopLevel(this) as Window);

            // Pick JRE path:
            string path = (await picker.PickFileAsync(SpaceHavenConstants.JRE_FILENAME)).AsOSPath();
            if (!IOUtils.FileExists(path))
                return;
            Paths.JREPath = path;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            if (jrePath != Paths.ModValuesDir && IOUtils.DirectoryExists(Paths.ModValuesDir))
                DispatchQueue.TryEnqueue(async () => await State.InitializeAsync(true));
        }
    }


    private async void DonateAsync(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenHttpAsync("https://buymeacoffee.com/ghostkeepeb", Log));

    private async void Open_WorkDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.WorkDir, Log));
    private void Open_ExportDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.ExportDir, Log));

    private void Open_SpaceHavenDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.SpaceHavenDir, Log));
    private void Open_SpaceHavenJarDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.SpaceHavenJarDir, Log));

    private void Open_SteamDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.SteamDir, Log));
    private void Open_SteamModsDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.SteamModsDir, Log));
    private void Open_ClassicModsDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.ClassicModsDir, Log));

    private void Open_ModValuesDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Paths.ModValuesDir, Log));

    private void Open_JREDir(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(async () => await OS.OpenDirectoryAsync(Path.GetDirectoryName(Paths.JREPath), Log));

    private void CreateWindowsShortcut(object sender, RoutedEventArgs e) =>
        State.DispatchQueue.TryEnqueue(() => new IconService(Log).CreateSpaceHavenLauncherWindowsDesktopIcon());

    private async void CollectDebuggingInformation(object sender, RoutedEventArgs e) =>
        await (DataContext as SystemCoreViewModel)?.CollectDebuggingInformation();

    private void Reset_JavaVMArgs(object sender, RoutedEventArgs e) =>
        AppSettings.JavaVMArgs = State.TemplateJavaVMArgs;
    private void Reset_JavaMainClass(object sender, RoutedEventArgs e) =>
        AppSettings.JavaMainClass = State.TemplateJavaMainClass;

    private void Copy_JavaVMArgs(object sender, RoutedEventArgs e) =>
        State.CopyToClipboardAsync(AppSettings.JavaVMArgs);
    private void Copy_JavaMainClass(object sender, RoutedEventArgs e) =>
        State.CopyToClipboardAsync(AppSettings.JavaVMArgs);

}
