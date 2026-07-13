using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Launcher.Core.Services;

#warning TODO: Create a new path finding system which combines diverse possible paths!

public sealed class PathSettingsRepositoryService
{
    public PathSettingsRepositoryService(ILogger logger) =>
        Log = logger ?? new VoidLogger();

    private readonly ILogger Log;

    private const string NOT_FOUND = "Not found";

    public async Task<bool> TrySaveAsync(PathData data, CancellationToken ct) =>
        await Task.Run(() => TrySaveInternalAsync(data, ct));
    private async Task<bool> TrySaveInternalAsync(PathData data, CancellationToken ct)
    {
        try
        {
            XDocument doc = new();
            XElement rootNode = new("PathSettings");

            rootNode.SetAttributeValue("schemaVersion", "1");

            doc.Add(rootNode);
            rootNode.Add(new XElement(nameof(PathData.SteamDir), data.SteamDir));
            rootNode.Add(new XElement(nameof(PathData.SpaceHavenDir), data.SpaceHavenDir));
            rootNode.Add(new XElement(nameof(PathData.SpaceHavenJarDir), data.SpaceHavenJarDir));
            rootNode.Add(new XElement(nameof(PathData.SteamModsDir), data.SteamModsDir));
            rootNode.Add(new XElement(nameof(PathData.ClassicModsDir), data.ClassicModsDir));
            rootNode.Add(new XElement(nameof(PathData.ModValuesDir), data.ModValuesDir));
            rootNode.Add(new XElement(nameof(PathData.JREPath), data.JREPath));

            if (!await IOUtils.TrySaveXDocumentAsync(data.PathSettingsPath, doc, null, Log, ct))
                return false;

            // Done.
            Log.Debug($"Path settings were successfully saved", data.PathSettingsPath);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, data?.WorkDir);
            return false;
        }
    }

    public PathData TryLoad()
    {
        try
        {
            PathData data = new();
            data.AppDir = ResolveAppDir(); // force
            if (data.AppDir == null)
                return null;

            data.WorkDir = ResolveWorkDir(); // force
            if (data.WorkDir == null)
                return null;

            if (!data.PathSettingsPath.FileExists())
                return null;

            XDocument doc = XDocument.Load(data.PathSettingsPath);
            XElement rootNode = doc.Element("PathSettings");
            if (rootNode == null)
            {
                Log.Error($"Invalid root node, <PathSettings> is expected", data.PathSettingsPath);
                return null;
            }

            string version = rootNode.Attribute("schemaVersion")?.Value;

            data.SteamDir = rootNode.Element(nameof(PathData.SteamDir))?.Value?.Trim();
            data.SpaceHavenDir = rootNode.Element(nameof(PathData.SpaceHavenDir))?.Value?.Trim();
            data.SpaceHavenJarDir = rootNode.Element(nameof(PathData.SpaceHavenJarDir))?.Value?.Trim();
            data.SteamModsDir = rootNode.Element(nameof(PathData.SteamModsDir))?.Value?.Trim();
            data.ClassicModsDir = rootNode.Element(nameof(PathData.ClassicModsDir))?.Value?.Trim();
            data.ModValuesDir = rootNode.Element(nameof(PathData.ModValuesDir))?.Value?.Trim();
            data.JREPath = rootNode.Element(nameof(PathData.JREPath))?.Value?.Trim();

            // Done.
            Log.Success($"Path settings were successfully loaded", data.PathSettingsPath);
            return data;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }

    public bool ResolveAll(PathData data)
    {
        bool success = true;
        try
        {
            // Launcher App Directory is always auto-calculated and always successful:
            data.AppDir = ResolveAppDir();

            // Launcher Work Directory:
            if (!data.WorkDir.DirExists())
            {
                data.WorkDir = ResolveWorkDir();
                if (data.WorkDir.IsNullOrWhiteSpace())
                    Log.Error($"Unable to resolve {SpaceHavenLauncher.Name} work directory");
            }

            // Mod Values Directory:
            if (!data.ModValuesDir.DirExists())
            {
                data.ModValuesDir = ResolveModValuesDirFromWorkDir(data.WorkDir);
                if (data.ModValuesDir.IsNullOrWhiteSpace())
                {
                    Log.Error("Unable to resolve the mod values directory");
                    success = false;
                }
            }

            // Steam Directory:
            if (!data.SteamDir.DirExists())
            {
                data.SteamDir = ResolveSteamDirFromAppDir(data.AppDir);
                if (data.SteamDir.IsNullOrWhiteSpace())
                    Log.Warn("Unable to detect the Steam directory");
            }

            // Steam Mods Directory:
            if (!data.SteamModsDir.DirExists())
            {
                data.SteamModsDir = ResolveSteamModsDirFromSteamDir(data.SteamDir);
                if (data.SteamModsDir.IsNullOrWhiteSpace())
                    Log.Warn("Unable to detect the Steam Workshop mods directory");
            }

            // Space Haven JAR Directory:
            if (!data.SpaceHavenJarDir.DirExists())
            {
                data.SpaceHavenJarDir = ResolveSpaceHavenJarDirFromAppDir(data.AppDir);
                if (data.SpaceHavenJarDir.IsNullOrWhiteSpace())
                {
                    Log.Error($"Unable to detect the {SpaceHavenConstants.SpaceHavenName} JAR directory");
                    success = false;
                }
            }

            // Space Haven Directory:
            if (!data.SpaceHavenDir.DirExists())
            {
                data.SpaceHavenDir = ResolveSpaceHavenDirFromSpaceHavenJarDir(data.SpaceHavenJarDir);
                if (data.SpaceHavenDir.IsNullOrWhiteSpace())
                {
                    Log.Error($"Unable to detect the {SpaceHavenConstants.SpaceHavenName} directory");
                    success = false;
                }
            }

            // Classic Mods Directory:
            if (!data.ClassicModsDir.DirExists())
            {
                data.ClassicModsDir = ResolveClassicModsDirFromSpaceHavenJarDir(data.SpaceHavenJarDir);
                if (data.ClassicModsDir.IsNullOrWhiteSpace())
                {
                    Log.Warn("Unable to detect the Classic Mods directory");
                    success = false;
                }
            }

            // JRE Path:
            if (!data.JREPath.FileExists())
            {
                data.JREPath = ResolveJREPathFromSpaceHavenJarDir(data.SpaceHavenJarDir);
                if (data.JREPath.IsNullOrWhiteSpace())
                {
                    Log.Warn("Unable to detect the JRE path");
                    success = false;
                }
            }

            // Done.
            return success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, data?.WorkDir);
            return false;
        }
    }



    public string ResolveAppDir()
    {
        string dir = SpaceHavenLauncher.Directory;
        Log.Debug($@"{nameof(ResolveAppDir)}: Auto resolved as ""{dir}""");
        return dir;
    }



    public string ResolveWorkDir()
    {
        // Windows C:\Users\<User>\AppData\Local
        // Linux   ~/.local/share
        // macOS   ~/Library/Application Support
        try
        {
            string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = appDataDir.CombineAsOSPath("SpaceHavenLauncher");
            if (!dir.DirExists())
                IOUtils.TryCreateDirectory(dir, Log);
            return dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }



    public string ResolveSteamDirFromAppDir(string appDir)
    {
        try
        {
            if (!appDir.DirExists())
                return null;

            // Detect Steam dir when this app was installed from workshop:
            if (appDir.Contains("979110"))
            {
                try
                {
                    string absoluteDir = appDir
                        .GetParentDirAsOSPath()
                        .GetParentDirAsOSPath()
                        .GetParentDirAsOSPath()
                        .GetParentDirAsOSPath()
                        .GetParentDirAsOSPath();

                    Log.Debug($@"{nameof(ResolveSteamDirFromAppDir)}: Testing ""{absoluteDir}""");

                    if (!absoluteDir.EndsWith("Steam", StringComparison.OrdinalIgnoreCase) || !absoluteDir.DirExists())
                        return null;

                    Log.Debug($@"{nameof(ResolveSteamDirFromAppDir)}: Auto detected ""{absoluteDir}""");
                    return absoluteDir;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            // Brute force:
            foreach (string tentativeDir in PossibleSteamDirs)
            {
                Log.Debug($@"{nameof(ResolveSteamDirFromAppDir)}: Testing ""{tentativeDir}""");

                string textativePath = tentativeDir;

                // Relative to app directory:
                if (textativePath.StartsWith('.'))
                    textativePath = appDir.CombineAsEvaluatedOSPath(textativePath);

                // Relative to home directory:
                else if (textativePath.StartsWith("~/"))
                    textativePath = textativePath.AsEvaluatedStdPath();

                // Absolute:
                textativePath = textativePath.FindDir();

                // Done.
                Log.Info($@"{nameof(ResolveSteamDirFromAppDir)}: Auto detected ""{textativePath}""");
                return textativePath;
            }

            // Nothing was found.
            Log.Error($@"{nameof(ResolveSteamDirFromAppDir)}: {NOT_FOUND}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($@"{nameof(ResolveSteamDirFromAppDir)}: {ex}");
            return null;
        }
    }



    public string ResolveSpaceHavenJarDirFromAppDir(string appDir)
    {
        try
        {
            if (!appDir.DirExists())
                return null;

            // Brute force:
            foreach (string tentativeDir in PossibleSpaceHavenJarDirs)
            {
                Log.Debug($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Testing ""{tentativeDir}""");

                string textativePath = tentativeDir.CombineAsOSPath(SpaceHavenConstants.SPACEHAVEN_JAR);

                // Relative to app directory:
                if (textativePath.StartsWith('.'))
                    textativePath = appDir.CombineAsEvaluatedOSPath(textativePath);

                // Relative to home directory:
                else if (textativePath.StartsWith("~/"))
                    textativePath = textativePath.AsEvaluatedStdPath();

                // Absolute:
                textativePath = textativePath.FindFile();

                // Test it:
                if (textativePath.IsNullOrWhiteSpace())
                    continue;

                // Done.
                string absoluteDir = textativePath.GetParentDirAsOSPath().FindDir();
                if (absoluteDir.IsNullOrWhiteSpace())
                    continue;

                Log.Info($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Auto detected ""{absoluteDir}""");
                return absoluteDir;
            }

            // Nothing was found.
            Log.Error($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: {NOT_FOUND}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: {ex}");
            return null;
        }
    }



    public string ResolveSpaceHavenDirFromSpaceHavenJarDir(string spaceHavenJarDir)
    {
        try
        {
            if (!spaceHavenJarDir.DirExists())
                return null;

            switch (OS.Type)
            {
                case EOSType.Windows:
                    return spaceHavenJarDir;
                case EOSType.OSX:
                    return spaceHavenJarDir.GetParentDirAsOSPath().GetParentDirAsOSPath().FindDir();
                case EOSType.Linux:
                    return spaceHavenJarDir;
                default:
                    throw new OSException();
            }
        }
        catch (Exception ex)
        {
            Log.Error($@"{nameof(ResolveSpaceHavenDirFromSpaceHavenJarDir)}: {ex}");
            return null;
        }
    }


    public string ResolveSpaceHavenJarDirFromSpaceHavenDir(string spaceHavenDir)
    {
        try
        {
            if (!spaceHavenDir.DirExists())
                return null;

            switch (OS.Type)
            {
                case EOSType.Windows:
                    return spaceHavenDir.FindDir();
                case EOSType.OSX:
                    return spaceHavenDir.CombineAsOSPath("Contents", "Resources").FindDir();
                case EOSType.Linux:
                    return spaceHavenDir.FindDir();
                default:
                    throw new OSException();
            }
        }
        catch (Exception ex)
        {
            Log.Error($@"{nameof(ResolveSpaceHavenJarDirFromSpaceHavenDir)}: {ex}");
            return null;
        }
    }


    public string ResolveJREPathFromSpaceHavenJarDir(string spaceHavenJarDir)
    {
        try
        {
            if (!spaceHavenJarDir.DirExists())
                return null;

            return spaceHavenJarDir.CombineAsOSPath("jre", "bin", SpaceHavenConstants.JRE_FILENAME).FindFile();
        }
        catch (Exception ex)
        {
            Log.Error($@"{nameof(ResolveJREPathFromSpaceHavenJarDir)}: {ex}");
            return null;
        }
    }


    public string ResolveSteamModsDirFromSteamDir(string steamDir)
    {
        try
        {
            if (!steamDir.DirExists())
                return null;

            return steamDir.CombineAsOSPath("steamapps", "workshop", "content", "979110").FindDir();
        }
        catch (Exception ex)
        {
            Log.Error($@"{nameof(ResolveSteamModsDirFromSteamDir)}: {ex}");
            return null;
        }
    }



    public string ResolveClassicModsDirFromSpaceHavenJarDir(string spaceHavenJarDir)
    {
        try
        {
            if (!spaceHavenJarDir.DirExists())
                return null;

            string tentativeDir = spaceHavenJarDir.CombineAsOSPath("mods");

            string absoluteDir = tentativeDir.FindDir();
            if (absoluteDir != null)
                return absoluteDir;

            if (!IOUtils.TryCreateDirectory(tentativeDir, Log))
                return null;

            return tentativeDir;
        }
        catch (Exception ex)
        {
            Log.Error($@"{nameof(ResolveClassicModsDirFromSpaceHavenJarDir)}: {ex}");
            return null;
        }
    }



    public string ResolveModValuesDirFromWorkDir(string workDir)
    {
        try
        {
            if (!workDir.DirExists())
                return null;

            string tentativeDir = workDir.CombineAsOSPath("values");

            string absoluteDir = tentativeDir.FindDir();
            if (absoluteDir != null)
                return absoluteDir;

            if (!IOUtils.TryCreateDirectory(tentativeDir, Log))
                return null;

            return tentativeDir;
        }
        catch (Exception ex)
        {
            Log.Error($@"{nameof(ResolveModValuesDirFromWorkDir)}: {ex}");
            return null;
        }
    }

    #region Possible Space Haven Locations

    private readonly IReadOnlyList<string> PossibleSpaceHavenJarDirs =

    OS.IsWin ?
    [
        //E:\SteamLibrary\steamapps\common\SpaceHaven

        // Steam:
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).CombineAsOSPath( @"Steam\steamapps\common\SpaceHaven"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).CombineAsOSPath( @"Steam\steamapps\common\SpaceHaven"),
        @"C:\Steam\steamapps\common\SpaceHaven",
        @"C:\Games\Steam\steamapps\common\SpaceHaven",
        @"D:\Steam\steamapps\common\SpaceHaven",
        @"D:\Games\Steam\steamapps\common\SpaceHaven",
        @"E:\Steam\steamapps\common\SpaceHaven",
        @"E:\Games\Steam\steamapps\common\SpaceHaven",
        @"F:\Steam\steamapps\common\SpaceHaven",
        @"F:\Games\Steam\steamapps\common\SpaceHaven",
        @"G:\Steam\steamapps\common\SpaceHaven",
        @"G:\Games\Steam\steamapps\common\SpaceHaven",

        // User-Defined:
        @"..",
        @"..\..",
        @"..\..\..",
        @"..\SpaceHaven",
        @"..\Space Haven",
        @"..\..\SpaceHaven",
        @"..\..\Space Haven",
        @"C:\Games\SpaceHaven",
        @"C:\Games\Space Haven",
        @"E:\Games\SpaceHaven",
        @"E:\Games\Space Haven",
        @"F:\Games\SpaceHaven",
        @"F:\Games\Space Haven",
        @"G:\Games\SpaceHaven",
        @"G:\Games\Space Haven",

        // GOG:
        @"C:\GOG Games\SpaceHaven",
        @"C:\GOG Games\Space Haven",
        @"C:\GOG Games\SpaceHaven\game",
        @"C:\GOG Games\Space Haven\game",

        @"D:\GOG Games\SpaceHaven",
        @"D:\GOG Games\Space Haven",
        @"D:\GOG Games\SpaceHaven\game",
        @"D:\GOG Games\Space Haven\game",

        @"E:\GOG Games\SpaceHaven",
        @"E:\GOG Games\Space Haven",
        @"E:\GOG Games\SpaceHaven\game",
        @"E:\GOG Games\Space Haven\game",

        @"F:\GOG Games\SpaceHaven",
        @"F:\GOG Games\Space Haven",
        @"F:\GOG Games\SpaceHaven\game",
        @"F:\GOG Games\Space Haven\game",

        @"G:\GOG Games\SpaceHaven",
        @"G:\GOG Games\Space Haven",
        @"G:\GOG Games\SpaceHaven\game",
        @"G:\GOG Games\Space Haven\game",

        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).CombineAsOSPath(@"GOG Galaxy\Games\SpaceHaven"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).CombineAsOSPath(@"GOG Galaxy\Games\Space Haven"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).CombineAsOSPath(@"GOG Galaxy\Games\SpaceHaven\game"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).CombineAsOSPath(@"GOG Galaxy\Games\Space Haven\game"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).CombineAsOSPath(@"GOG Galaxy\Games\SpaceHaven"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).CombineAsOSPath(@"GOG Galaxy\Games\Space Haven"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).CombineAsOSPath(@"GOG Galaxy\Games\SpaceHaven\game"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).CombineAsOSPath(@"GOG Galaxy\Games\Space Haven\game"),
    ] :

    OS.IsMac ?
    [
        // Steam:
        "~/Library/Application Support/Steam/steamapps/common/SpaceHaven/spacehaven.app/Contents/Resources",
        "~/Library/Application Support/Steam/steamapps/common/spacehaven/spacehaven.app/Contents/Resources",
        "~/Library/Application Support/Steam/steamapps/common/Space Haven/spacehaven.app/Contents/Resources",

        // User-Defined:
        "../spacehaven.app/Contents/Resources",
        "~/Games/spacehaven.app/Contents/Resources",
        "~/Applications/spacehaven.app/Contents/Resources",
        "~/Applications/Games/spacehaven.app/Contents/Resources",

        "~/Games/SpaceHaven/spacehaven.app/Contents/Resources",
        "~/Games/spacehaven/spacehaven.app/Contents/Resources",
        "~/Games/Space Haven/spacehaven.app/Contents/Resources",

        "~/Applications/Games/SpaceHaven/spacehaven.app/Contents/Resources",
        "~/Applications/Games/spacehaven/spacehaven.app/Contents/Resources",
        "~/Applications/Games/Space Haven/spacehaven.app/Contents/Resources",

        "~/Library/Application Support/SpaceHaven/spacehaven.app/Contents/Resources",
        "~/Library/Application Support/spacehaven/spacehaven.app/Contents/Resources",
        "~/Library/Application Support/Space Haven/spacehaven.app/Contents/Resources",

        // GOG:
        "~/Applications/GOG Games/spacehaven.app/Contents/Resources",

        "~/Applications/GOG Games/SpaceHaven/spacehaven.app/Contents/Resources",
        "~/Applications/GOG Games/spacehaven/spacehaven.app/Contents/Resources",
        "~/Applications/GOG Games/Space Haven/spacehaven.app/Contents/Resources",

        "~/Library/Application Support/GOG Games/SpaceHaven/spacehaven.app/Contents/Resources",
        "~/Library/Application Support/GOG Games/spacehaven/spacehaven.app/Contents/Resources",
        "~/Library/Application Support/GOG Games/Space Haven/spacehaven.app/Contents/Resources",

    ] :

    OS.IsLnx ?
    [
        // Steam:
        "~/.steam/steam/steamapps/common/SpaceHaven",
        "~/.steam/steam/steamapps/common/spacehaven",
        "~/.steam/steam/steamapps/common/Space Haven",

        "~/.local/share/Steam/steamapps/common/SpaceHaven",
        "~/.local/share/Steam/steamapps/common/spacehaven",
        "~/.local/share/Steam/steamapps/common/Space Haven",

        "~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/SpaceHaven",
        "~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/spacehaven",
        "~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/Space Haven",

        // User-Defined:
        "../SpaceHaven",
        "../spacehaven",
        "../Space Haven",

        "../../SpaceHaven",
        "../../spacehaven",
        "../../Space Haven",

        "~/Games/SpaceHaven",
        "~/Games/spacehaven",
        "~/Games/Space Haven",

        "~/.local/share/SpaceHaven",
        "~/.local/share/spacehaven",
        "~/.local/share/Space Haven",

        "~/.local/share/applications/SpaceHaven",
        "~/.local/share/applications/spacehaven",
        "~/.local/share/applications/Space Haven",

        "/opt/SpaceHaven",
        "/opt/spacehaven",
        "/opt/Space Haven",

        // GOG:
        "~/GOG Games/SpaceHaven",
        "~/GOG Games/spacehaven",
        "~/GOG Games/Space Haven",

        "~/GOG Games/SpaceHaven/game",
        "~/GOG Games/spacehaven/game",
        "~/GOG Games/Space Haven/game",

        "~/GOG Games/SpaceHaven/Game",
        "~/GOG Games/spacehaven/Game",
        "~/GOG Games/Space Haven/Game",
    ] :
    [];

    #endregion

    #region Possible Steam Locations

    private readonly IReadOnlyList<string> PossibleSteamDirs =

    OS.IsWin ?
    [
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).CombineAsOSPath("Steam"),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).CombineAsOSPath("Steam"),
        @"C:\Games\Steam",
        @"C:\Steam",
        @"D:\Games\Steam",
        @"D:\Steam",
        @"E:\Games\Steam",
        @"E:\Steam",
    ] :

    OS.IsMac ?
    [
        "~/Library/Application Support/Steam",
    ] :

    OS.IsLnx ?
    [
        "~/.steam/steam",
        "~/.steam/Steam",

        "~/.local/share/steam",
        "~/.local/share/Steam",

        "~/.var/app/com.valvesoftware.Steam/.local/share/Steam",
        "~/.var/app/com.valvesoftware.Steam/.local/share/Steam",
    ] :
    [];

    #endregion
}
