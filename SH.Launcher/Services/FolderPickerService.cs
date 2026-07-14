using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SH.Framework.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SH.Launcher.Services;

public class FolderPickerService
{
    private readonly Window Window;

    public FolderPickerService(Window window)
    {
        Window = window;
    }

    public async Task<string> PickFolderAsync()
    {
        IReadOnlyList<IStorageFolder> folders = await Window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Folder",
                AllowMultiple = false,
            });
        return folders.FirstOrDefault()?.Path?.LocalPath.AsOSPath();
    }

    public async Task<string> PickFileAsync(string filter)
    {
        IReadOnlyList<IStorageFolder> folders = await Window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Folder",
                AllowMultiple = false,
                SuggestedFileName = filter,
            });
        return folders.FirstOrDefault()?.Path?.LocalPath.AsOSPath();
    }
}