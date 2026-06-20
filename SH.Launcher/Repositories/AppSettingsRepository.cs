using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Launcher.Repositories;

public sealed class AppSettingsRepository
{
    private readonly PathData Paths;

    private readonly ILogger Log;

    public AppSettingsRepository(PathData paths, ILogger logger)
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
            if (Paths.ApplicationSettingsPath.IsNullOrWhiteSpace() || !File.Exists(Paths.ApplicationSettingsPath))
            {
                Log.Warn($@"Creating new application settings file ""{Paths.ApplicationSettingsPath}""");
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
                int.TryParse(root.Element(nameof(AppSettingsData.MonitorIndex))?.Value ?? string.Empty, out int monitorIndex) ? monitorIndex : 0;

            data.IsLeftPaneCollapsed =
                bool.TryParse(root.Element(nameof(AppSettingsData.IsLeftPaneCollapsed))?.Value ?? string.Empty, out bool isLeftPaneCollapsed) && isLeftPaneCollapsed;

            data.LogVerbosity =
                Enum.TryParse(root.Element(nameof(AppSettingsData.LogVerbosity))?.Value ?? string.Empty, out ELogVerbosity logVerbosity) ? logVerbosity : ELogVerbosity.Minimal;

            data.ModPageSplitterHeight =
                int.TryParse(root.Element(nameof(AppSettingsData.ModPageSplitterHeight))?.Value ?? string.Empty, out int modPageSplitterHeight) ? modPageSplitterHeight : data.ModPageSplitterHeight;

            data.IsBackgroundEnabled =
                bool.TryParse(root.Element(nameof(AppSettingsData.IsBackgroundEnabled))?.Value ?? string.Empty, out bool isBackgroundEnabled) && isBackgroundEnabled;

            data.BackgroundDarkness =
                double.TryParse(root.Element(nameof(AppSettingsData.BackgroundDarkness))?.Value ?? string.Empty, out double backgroundDarkness) ? backgroundDarkness : data.BackgroundDarkness;

            data.ForceSpritesheetSize2048 =
                bool.TryParse(root.Element(nameof(AppSettingsData.ForceSpritesheetSize2048))?.Value ?? string.Empty, out bool forceSpritesheetSize2048) && forceSpritesheetSize2048;

            data.XmlAnnotationLanguage =
                Enum.TryParse(root.Element(nameof(AppSettingsData.XmlAnnotationLanguage))?.Value ?? string.Empty, out ELanguage language) ? language : ELanguage.EN;

            data.ExportTextures = 
                bool.TryParse(root.Element(nameof(AppSettingsData.ExportTextures))?.Value ?? string.Empty, out bool exportTextures) && exportTextures;

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
            root.SetAttributeValue("launcherVersion", SpaceHavenLauncher.GetAppVersion());
            doc.Add(root);

            // Write fields:

            root.Add(new XElement(nameof(AppSettingsData.MonitorIndex), data.MonitorIndex));
            root.Add(new XElement(nameof(AppSettingsData.IsLeftPaneCollapsed), data.IsLeftPaneCollapsed));
            root.Add(new XElement(nameof(AppSettingsData.LogVerbosity), data.LogVerbosity));
            root.Add(new XElement(nameof(AppSettingsData.ModPageSplitterHeight), data.ModPageSplitterHeight));
            root.Add(new XElement(nameof(AppSettingsData.IsBackgroundEnabled), data.IsBackgroundEnabled));
            root.Add(new XElement(nameof(AppSettingsData.BackgroundDarkness), data.BackgroundDarkness));
            root.Add(new XElement(nameof(AppSettingsData.ForceSpritesheetSize2048), data.ForceSpritesheetSize2048));
            root.Add(new XElement(nameof(AppSettingsData.XmlAnnotationLanguage), data.XmlAnnotationLanguage));
            root.Add(new XElement(nameof(AppSettingsData.ExportTextures), data.ExportTextures));

            // Save to file:
            if (!await IOUtils.TrySaveXDocumentAsync(Paths.ApplicationSettingsPath, doc, Log, ct))
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
