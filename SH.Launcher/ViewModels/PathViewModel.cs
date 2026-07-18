using CommunityToolkit.Mvvm.ComponentModel;
using SH.Content;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Modding.Models;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.ViewModels;

public partial class PathViewModel : ObservableObject
{
    public AppViewModel State => AppViewModel.State;
    public DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    public PathData Data { get; private set; }

    [ObservableProperty]
    private string _SteamDir;

    [ObservableProperty]
    private string _SteamModsDir;

    [ObservableProperty]
    private string _ClassicModsDir;

    [ObservableProperty]
    private string _SpaceHavenDir;

    [ObservableProperty]
    private string _SpaceHavenJarDir;

    [ObservableProperty]
    private string _AppDir;

    [ObservableProperty]
    private string _WorkDir;

    [ObservableProperty]
    private string _ExportDir;

    [ObservableProperty]
    private string _ModValuesDir;

    [ObservableProperty]
    private string _JREPath;



    public PathViewModel(PathData data)
    {
        SetData(data);
    }



    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        SyncData(e.PropertyName);
        base.OnPropertyChanged(e);
    }

    public PathViewModel SetData(PathData data)
    {
        Data = data;

        // Calculated directories:
        AppDir = data.AppDir;
        WorkDir = data.WorkDir;

        // Configurable directories:
        SteamDir = data.SteamDir;
        SteamModsDir = data.SteamModsDir;
        SpaceHavenDir = data.SpaceHavenDir;
        ClassicModsDir = data.ClassicModsDir;
        SpaceHavenJarDir = data.SpaceHavenJarDir;
        ModValuesDir = data.ModValuesDir;
        ExportDir = data.ExportDir;
        JREPath = data.JREPath;

        return this;
    }

    private void SyncData(string propertyName)
    {
        try
        {
            // Configurable dirs only!
            switch (propertyName)
            {
                case nameof(SteamDir):
                    Data?.SteamDir = SteamDir;
                    return;
                case nameof(SteamModsDir):
                    Data?.SteamModsDir = SteamModsDir;
                    return;
                case nameof(SpaceHavenDir):
                    Data?.SpaceHavenDir = SpaceHavenDir;
                    return;
                case nameof(ClassicModsDir):
                    Data?.ClassicModsDir = ClassicModsDir;
                    return;
                case nameof(SpaceHavenJarDir):
                    Data?.SpaceHavenJarDir = SpaceHavenJarDir;
                    return;
                case nameof(ModValuesDir):
                    Data?.ModValuesDir = ModValuesDir;
                    return;
                case nameof(JREPath):
                    Data?.JREPath = JREPath;
                    return;
                default:
                    break;
            }
        }
        catch { }
    }


}