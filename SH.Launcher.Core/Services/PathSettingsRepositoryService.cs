using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            data.WorkDir = ResolveWorkDir(); // force

            if (data.PathSettingsPath.IsNullOrWhiteSpace() || !IOUtils.FileExists(data.PathSettingsPath))
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
            if (!IOUtils.DirectoryExists(data.WorkDir))
            {
                data.WorkDir = ResolveWorkDir();
                if (data.WorkDir.IsNullOrWhiteSpace())
                    Log.Error($"Unable to resolve {SpaceHavenLauncher.Name} work directory");
            }

            // Mod Values Directory:
            if (!IOUtils.DirectoryExists(data.ModValuesDir))
            {
                data.ModValuesDir = ResolveModValuesDirFromWorkDir(data.WorkDir);
                if (data.ModValuesDir.IsNullOrWhiteSpace())
                {
                    Log.Error("Unable to resolve the mod values directory");
                    success = false;
                }
            }

            // Steam Directory:
            if (!IOUtils.DirectoryExists(data.SteamDir))
            {
                data.SteamDir = ResolveSteamDirFromAppDir(data.AppDir);
                if (data.SteamDir.IsNullOrWhiteSpace())
                    Log.Warn("Unable to detect the Steam directory");
            }

            // Steam Mods Directory:
            if (!IOUtils.DirectoryExists(data.SteamModsDir))
            {
                data.SteamModsDir = ResolveSteamModsDirFromSteamDir(data.SteamDir);
                if (data.SteamModsDir.IsNullOrWhiteSpace())
                    Log.Warn("Unable to detect the Steam Workshop mods directory");
            }

            // Space Haven JAR Directory:
            if (!IOUtils.DirectoryExists(data.SpaceHavenJarDir))
            {
                data.SpaceHavenJarDir = ResolveSpaceHavenJarDirFromAppDir(data.AppDir);
                if (data.SpaceHavenJarDir.IsNullOrWhiteSpace())
                {
                    Log.Error($"Unable to detect the {SpaceHavenConstants.SpaceHavenName} JAR directory");
                    success = false;
                }
            }

            // Space Haven Directory:
            if (!IOUtils.DirectoryExists(data.SpaceHavenDir))
            {
                data.SpaceHavenDir = ResolveSpaceHavenDirFromSpaceHavenJarDir(data.SpaceHavenJarDir);
                if (data.SpaceHavenDir.IsNullOrWhiteSpace())
                {
                    Log.Error($"Unable to detect the {SpaceHavenConstants.SpaceHavenName} directory");
                    success = false;
                }
            }

            // Classic Mods Directory:
            if (!IOUtils.DirectoryExists(data.ClassicModsDir))
            {
                data.ClassicModsDir = ResolveClassicModsDirFromSpaceHavenJarDir(data.SpaceHavenJarDir);
                if (data.ClassicModsDir.IsNullOrWhiteSpace())
                {
                    Log.Warn("Unable to detect the Classic Mods directory");
                    success = false;
                }
            }

            // JRE Path:
            if (!IOUtils.FileExists(data.JREPath))
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
        string dir = IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpaceHavenLauncher").AsOSPath();

        try
        {
            if (!IOUtils.DirectoryExists(dir))
                IOUtils.TryCreateDirectory(dir, Log);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        // Done.
        Log.Debug($@"{nameof(ResolveWorkDir)}: Auto resolved as ""{dir}""");
        return dir;
    }



    public string ResolveSteamDirFromAppDir(string appDir)
    {
        appDir = appDir.AsOSPath();
        if (!IOUtils.DirectoryExists(appDir))
            return null;

        // Detect Steam dir when this app was installed from workshop:
        if (appDir.Contains("979110"))
        {
            try
            {
                string dir =
                    Path.GetDirectoryName(
                    Path.GetDirectoryName(
                    Path.GetDirectoryName(
                    Path.GetDirectoryName(
                    Path.GetDirectoryName(
                    appDir
                )))))
                .AsOSPath();

                if (!dir.EndsWith("Steam", StringComparison.OrdinalIgnoreCase) || !IOUtils.DirectoryExists(dir))
                    return null;

                // Done.
                Log.Debug($@"{nameof(ResolveSteamDirFromAppDir)}: Auto detected ""{dir}""");
                return dir;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }



        // Brute force:
        foreach (string possibleDir in PossibleSteamDirs)
        {
            Log.Debug($@"{nameof(ResolveSteamDirFromAppDir)}: Testing ""{possibleDir}""");
            
            string dir = possibleDir.AsOSPath();
            if (dir.IsNullOrWhiteSpace())
                continue;

            // Relative to app directory:
            if (dir.StartsWith(".."))
            {
                try
                {
                    string baseDir = appDir;
                    do
                    {
                        dir = dir.Substring(3);
                        baseDir = Path.GetDirectoryName(baseDir) ?? string.Empty;
                    } while (dir.StartsWith(".."));

                    if (baseDir.IsNullOrWhiteSpace() || dir.IsNullOrWhiteSpace())
                    {
                        Log.Info($@"{nameof(ResolveSteamDirFromAppDir)}: Failed to evaluate ""{dir}""");
                        continue;
                    }

                    dir = IOUtils.CombineAsOSPath(baseDir, dir);
                }
                catch (Exception ex)
                {
                    Log.Info($@"{nameof(ResolveSteamDirFromAppDir)}: Failed to evaluate ""{dir}"": {ex.Message}");
                    continue;
                }
            }

            // Relative to home directory:
            else if (dir.StartsWith("~/"))
            {
                try
                {
                    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).AsOSPath();
                    dir = dir.Substring(2);
                    dir = IOUtils.CombineAsOSPath(homeDir, dir);
                }
                catch (Exception ex)
                {
                    Log.Info($@"{nameof(ResolveSteamDirFromAppDir)}: Failed to evaluate ""{dir}"": {ex.Message}");
                    continue;
                }
            }

            // Test it:
            if (!IOUtils.DirectoryExists(dir))
            {
                Log.Info($@"{nameof(ResolveSteamDirFromAppDir)}: Path doesn't exist ""{dir}""");
                continue;
            }

            // Done.
            Log.Info($@"{nameof(ResolveSteamDirFromAppDir)}: Auto detected ""{dir}""");
            return dir;
        }

        // Nothing was found.
        Log.Error($@"{nameof(ResolveSteamDirFromAppDir)}: Nothing could be found");
        return null;
    }



    public string ResolveSpaceHavenJarDirFromAppDir(string appDir)
    {
        appDir = appDir.AsOSPath();
        if (!IOUtils.DirectoryExists(appDir))
            return null;

        // Brute force:
        foreach (string possibleDir in PossibleSpaceHavenJarDirs)
        {
            Log.Debug($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Testing ""{possibleDir}""");

            string path = IOUtils.CombineAsOSPath(possibleDir, SpaceHavenConstants.SPACEHAVEN_JAR).AsOSPath();

            // Relative to app directory:
            if (path.StartsWith(".."))
            {
                try
                {
                    string baseDir = appDir;
                    do
                    {
                        path = path.Substring(3);
                        baseDir = Path.GetDirectoryName(baseDir);
                    } while (path.StartsWith(".."));

                    if (baseDir.IsNullOrWhiteSpace() || path.IsNullOrWhiteSpace())
                    {
                        Log.Info($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Failed to evaluate ""{path}""");
                        continue;
                    }

                    path = IOUtils.CombineAsOSPath(baseDir, path);
                }
                catch (Exception ex)
                {
                    Log.Info($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Failed to evaluate ""{path}"": {ex.Message}");
                    continue;
                }
            }

            // Relative to home directory:
            else if (path.StartsWith("~/"))
            {
                try
                {
                    string homeDir = Path.TrimEndingDirectorySeparator(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    path = path.Substring(2);
                    path = IOUtils.CombineAsOSPath(homeDir, path);
                }
                catch (Exception ex)
                {
                    Log.Info($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Failed to evaluate ""{path}"": {ex.Message}");
                    continue;
                }
            }

            // Test it:
            if (!IOUtils.FileExists(path))
            {
                Log.Info($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Path doesn't exist ""{path}""");
                continue;
            }

            // Done.
            string dir = Path.GetDirectoryName(path).AsOSPath();
            Log.Info($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Auto detected ""{dir}""");
            return dir;
        }

        // Nothing was found.
        Log.Error($@"{nameof(ResolveSpaceHavenJarDirFromAppDir)}: Nothing could be found");
        return null;
    }



    public string ResolveSpaceHavenDirFromSpaceHavenJarDir(string spaceHavenJarDir)
    {
        try
        {
            spaceHavenJarDir = spaceHavenJarDir.AsOSPath();
            if (!IOUtils.DirectoryExists(spaceHavenJarDir))
                return null;

            string dir;
            switch (OS.Type)
            {
                case EOSType.Windows:
                    dir = spaceHavenJarDir;
                    break;
                case EOSType.OSX:
                    dir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(spaceHavenJarDir))).AsOSPath();
                    break;
                case EOSType.Linux:
                    dir = spaceHavenJarDir;
                    break;
                default:
                    throw new NotImplementedException($"OS = {OS.Type}");
            }

            Log.Info($@"{nameof(ResolveSpaceHavenDirFromSpaceHavenJarDir)}: Testing path ""{dir}""");
            if (!IOUtils.DirectoryExists(dir))
                return null;

            Log.Info($@"{nameof(ResolveSpaceHavenDirFromSpaceHavenJarDir)}: Auto detected ""{dir}""");
            return dir;
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
            spaceHavenDir = spaceHavenDir.AsOSPath();
            if (!IOUtils.DirectoryExists(spaceHavenDir))
                return null;

            string dir;
            switch (OS.Type)
            {
                case EOSType.Windows:
                    dir = spaceHavenDir;
                    break;

                case EOSType.OSX:
                    spaceHavenDir.RemoveSuffix("/spacehaven.app", StringComparison.OrdinalIgnoreCase);
                    string[] dirs = Directory.GetDirectories(spaceHavenDir).Where(d => d.Equals("spacehaven.app", StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (dirs.Length != 1)
                        return null;
                    dir = IOUtils.CombineAsOSPath(dirs[0], "Contents", "Resources").AsOSPath();
                    break;

                case EOSType.Linux:
                    dir = spaceHavenDir;
                    break;

                default:
                    throw new NotImplementedException($"OS = {OS.Type}");
            }

            Log.Info($@"{nameof(ResolveSpaceHavenJarDirFromSpaceHavenDir)}: Testing path ""{dir}""");
            if (!IOUtils.DirectoryExists(dir))
                return null;

            Log.Info($@"{nameof(ResolveSpaceHavenJarDirFromSpaceHavenDir)}: Auto detected ""{dir}""");
            return dir;
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
            spaceHavenJarDir = spaceHavenJarDir.AsOSPath();
            if (!IOUtils.DirectoryExists(spaceHavenJarDir))
                return null;

            string path = IOUtils.CombineAsOSPath(spaceHavenJarDir, "jre", "bin", SpaceHavenConstants.JRE_FILENAME).AsOSPath();
            if (!IOUtils.FileExists(path))
                return null;

            Log.Debug($@"{nameof(ResolveJREPathFromSpaceHavenJarDir)}: Auto detected ""{path}""");
            return path;
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
            steamDir = steamDir.AsOSPath();
            if (!IOUtils.DirectoryExists(steamDir))
                return null;
            
            string dir = IOUtils.CombineAsOSPath(steamDir, "steamapps", "workshop", "content", "979110").AsOSPath();
            if (!IOUtils.DirectoryExists(dir))
                return null;

            Log.Debug($@"{nameof(ResolveSteamModsDirFromSteamDir)}: Auto detected ""{dir}""");
            return dir;
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
            spaceHavenJarDir = spaceHavenJarDir.AsOSPath();
            if (!IOUtils.DirectoryExists(spaceHavenJarDir))
                return null;

            string dir = IOUtils.CombineAsOSPath(spaceHavenJarDir, "mods").AsOSPath();
            if (!IOUtils.DirectoryExists(dir))
                if (!IOUtils.TryCreateDirectory(dir, Log))
                    return null;

            Log.Debug($@"{nameof(ResolveClassicModsDirFromSpaceHavenJarDir)}: Auto detected ""{dir}""");
            return dir;
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
            workDir = workDir.AsOSPath();
            if (workDir.IsNullOrWhiteSpace())
                return null;

            string dir = IOUtils.CombineAsOSPath(workDir, "values").AsOSPath();
            if (!IOUtils.DirectoryExists(dir))
                if (!IOUtils.TryCreateDirectory(dir, Log))
                    return null;

            Log.Debug($@"{nameof(ResolveModValuesDirFromWorkDir)}: Auto detected ""{dir}""");
            return dir;
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
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\steamapps\common\SpaceHaven"),
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Steam\steamapps\common\SpaceHaven"),
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

        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games\SpaceHaven"),
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games\Space Haven"),
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games\SpaceHaven\game"),
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games\Space Haven\game"),

        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games\SpaceHaven"),
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games\Space Haven"),
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games\SpaceHaven\game"),
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games\Space Haven\game"),
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
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
        IOUtils.CombineAsOSPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
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
