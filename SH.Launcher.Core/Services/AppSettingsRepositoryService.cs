using SH.Content.Enums;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Launcher.Core.Services;

public sealed class AppSettingsRepositoryService
{
    private readonly PathData Paths;

    private readonly ILogger Log;

    public AppSettingsRepositoryService(PathData paths, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = logger ?? new VoidLogger();
    }

    /// <summary>
    /// Loads or creates application settings from file. Returns null only in case of really bad exceptions.
    /// </summary>
    public async Task<AppSettingsData> TryLoadOrCreateAsync(CancellationToken ct)
    {
        try
        {
            AppSettingsData data = AppSettingsData.GetDefault();

            // No such file?
            if (!IOUtils.FileExists(Paths.ApplicationSettingsPath))
            {
                Log.Info($@"Creating new application settings file ""{Paths.ApplicationSettingsPath}""");
                await TrySaveAsync(data, ct);
                return data; // no file? => return fresh new application settings
            }

            // Load from file:
            XDocument doc = await IOUtils.TryLoadXDocumentAsync(Paths.ApplicationSettingsPath, Log, ct);
            if (doc == null)
            {
                Log.Warn($@"Application settings file is corrupt ""{Paths.ApplicationSettingsPath}"" => creating new application settings");
                await TrySaveAsync(data, ct);
                return data; // corrupt file => return fresh new application settings
            }

            // Get root node:
            XElement root = doc.Element("AppSettings");
            if (root == null)
            {
                Log.Warn($@"Invalid root node in file ""{Paths.ApplicationSettingsPath}"" => creating new application settings");
                await TrySaveAsync(data, ct);
                return data; // invalid root node => return fresh new application settings
            }

            // TODO: Version it, for future use...
            string version = root.Attribute("launcherVersion")?.Value;


            // Read fields:
            data.MonitorIndex =
                int.TryParse(root.Element(nameof(AppSettingsData.MonitorIndex))?.Value ?? string.Empty, out int monitorIndex) ? monitorIndex : data.MonitorIndex;

            data.IsLeftPaneCollapsed =
                bool.TryParse(root.Element(nameof(AppSettingsData.IsLeftPaneCollapsed))?.Value ?? string.Empty, out bool isLeftPaneCollapsed) ? isLeftPaneCollapsed : data.IsLeftPaneCollapsed;

            data.LogVerbosity =
                Enum.TryParse(root.Element(nameof(AppSettingsData.LogVerbosity))?.Value ?? string.Empty, out ELogVerbosity logVerbosity) ? logVerbosity : data.LogVerbosity;

            data.ModPageSplitterHeight =
                int.TryParse(root.Element(nameof(AppSettingsData.ModPageSplitterHeight))?.Value ?? string.Empty, out int modPageSplitterHeight) ? modPageSplitterHeight : data.ModPageSplitterHeight;

            data.IsBackgroundEnabled =
                bool.TryParse(root.Element(nameof(AppSettingsData.IsBackgroundEnabled))?.Value ?? string.Empty, out bool isBackgroundEnabled) ? isBackgroundEnabled : data.IsBackgroundEnabled;

            data.BackgroundDarkness =
                double.TryParse(root.Element(nameof(AppSettingsData.BackgroundDarkness))?.Value ?? string.Empty, out double backgroundDarkness) ? backgroundDarkness : data.BackgroundDarkness;

            data.StartSpaceHavenAutomatically =
                bool.TryParse(root.Element(nameof(AppSettingsData.StartSpaceHavenAutomatically))?.Value ?? string.Empty, out bool startSpaceHavenAutomatically) ? startSpaceHavenAutomatically : data.StartSpaceHavenAutomatically;

            data.SkipRebuilding =
                bool.TryParse(root.Element(nameof(AppSettingsData.SkipRebuilding))?.Value ?? string.Empty, out bool skipRebuilding) ? skipRebuilding : data.SkipRebuilding;

            data.ExportXmlAnnotationLanguage =
                Enum.TryParse(root.Element(nameof(AppSettingsData.ExportXmlAnnotationLanguage))?.Value ?? string.Empty, out ELanguage language) ? language : data.ExportXmlAnnotationLanguage;

            data.ExportTextures =
                bool.TryParse(root.Element(nameof(AppSettingsData.ExportTextures))?.Value ?? string.Empty, out bool exportTextures) ? exportTextures : data.ExportTextures;

            data.ExportOption =
                Enum.TryParse(root.Element(nameof(AppSettingsData.ExportOption))?.Value ?? string.Empty, out EExportOption exportOption) ? exportOption : data.ExportOption;

            data.JavaVMArgs =
                root.Element(nameof(AppSettingsData.JavaVMArgs))?.Value ?? string.Empty;

            data.JavaMainClass =
                root.Element(nameof(AppSettingsData.JavaMainClass))?.Value ?? string.Empty;

            // Done.
            Log.Success($"Application settings loaded", Paths.ApplicationSettingsPath);
            return data;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            return null;
        }
    }



    public async Task<bool> TrySaveAsync(AppSettingsData data, CancellationToken ct)
    {
        try
        {
            XDocument doc = new();
            XElement root = new("AppSettings");

            // TODO: Version it, for future use...
            root.SetAttributeValue("launcherVersion", SpaceHavenLauncher.Version);
            doc.Add(root);

            // Write fields:

            root.Add(new XElement(nameof(AppSettingsData.MonitorIndex), data.MonitorIndex));
            root.Add(new XElement(nameof(AppSettingsData.IsLeftPaneCollapsed), data.IsLeftPaneCollapsed));
            root.Add(new XElement(nameof(AppSettingsData.LogVerbosity), data.LogVerbosity));
            root.Add(new XElement(nameof(AppSettingsData.ModPageSplitterHeight), data.ModPageSplitterHeight));
            root.Add(new XElement(nameof(AppSettingsData.IsBackgroundEnabled), data.IsBackgroundEnabled));
            root.Add(new XElement(nameof(AppSettingsData.BackgroundDarkness), data.BackgroundDarkness));
            root.Add(new XElement(nameof(AppSettingsData.StartSpaceHavenAutomatically), data.StartSpaceHavenAutomatically));
            root.Add(new XElement(nameof(AppSettingsData.SkipRebuilding), data.SkipRebuilding));
            root.Add(new XElement(nameof(AppSettingsData.ExportXmlAnnotationLanguage), data.ExportXmlAnnotationLanguage));
            root.Add(new XElement(nameof(AppSettingsData.ExportTextures), data.ExportTextures));
            root.Add(new XElement(nameof(AppSettingsData.ExportOption), data.ExportOption));
            root.Add(new XElement(nameof(AppSettingsData.JavaVMArgs), data.JavaVMArgs));
            root.Add(new XElement(nameof(AppSettingsData.JavaMainClass), data.JavaMainClass));

            // Save to file:
            if (!await IOUtils.TrySaveXDocumentAsync(Paths.ApplicationSettingsPath, doc, null, Log, ct))
                return false;

            // Done.
            Log.Debug($"Application settings saved", Paths.ApplicationSettingsPath);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            return false;
        }
    }
}
