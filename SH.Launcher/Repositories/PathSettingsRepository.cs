using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SH.Launcher.Repositories;

#warning TODO: Create a new path finding system which combinates diverse possible paths, e.g. on D:, E:, F:, etc
public sealed class PathSettingsRepository
{
    public PathSettingsRepository(ILogger logger) =>
        Log = logger ?? new VoidLogger();

    private readonly ILogger Log;

    public bool ResolveAll(PathData data)
    {
        bool success = true;
        try
        {
            // Launcher App Directory is always auto-calculated and always successful:
            data.AppDir = ResolveAppDir();

            // Launcher Work Directory:
            if (data.WorkDir.IsNullOrEmpty() || !Directory.Exists(data.WorkDir))
            {
                data.WorkDir = ResolveWorkDir();
                if (data.WorkDir.IsNullOrWhiteSpace())
                    Log.Error($"Unable to resolve {SpaceHavenLauncher.APP_NAME} work directory");
            }

            // Steam Directory:
            if (data.SteamDir.IsNullOrEmpty() || !Directory.Exists(data.SteamDir))
            {
                data.SteamDir = ResolveSteamDir(data.AppDir);
                if (data.SteamDir.IsNullOrWhiteSpace())
                    Log.Warn("Unable to detect the Steam directory");
            }

            // Space Haven Directory:
            if (data.SpaceHavenDir.IsNullOrWhiteSpace() || !Directory.Exists(data.SpaceHavenDir))
            {
                data.SpaceHavenDir = ResolveSpaceHavenDir(data.AppDir);
                if (data.SpaceHavenDir.IsNullOrWhiteSpace())
                {
                    Log.Error($"Unable to detect the {SpaceHavenConstants.SpaceHavenName} directory");
                    success = false;
                }
            }

            // Space Haven JAR Directory:
            if (data.SpaceHavenJarDir.IsNullOrWhiteSpace() || !Directory.Exists(data.SpaceHavenJarDir))
            {
                data.SpaceHavenJarDir = ResolveSpaceHavenJarDir(data.SpaceHavenDir);
                if (data.SpaceHavenJarDir.IsNullOrWhiteSpace())
                {
                    Log.Error($"Unable to detect the {SpaceHavenConstants.SpaceHavenName} JAR directory");
                    success = false;
                }
            }

            // Steam Mods Directory:
            if (data.SteamModsDir.IsNullOrWhiteSpace() || !Directory.Exists(data.SteamModsDir))
            {
                data.SteamModsDir = ResolveSteamModsDir(data.SteamDir);
                if (data.SteamModsDir.IsNullOrWhiteSpace())
                    Log.Warn("Unable to detect the Steam Workshop mods directory");
            }

            // Classic Mods Directory:
            if (data.ClassicModsDir.IsNullOrWhiteSpace() || !Directory.Exists(data.ClassicModsDir))
            {
                data.ClassicModsDir = ResolveClassicModsDir(data.SpaceHavenJarDir);
                if (data.ClassicModsDir.IsNullOrWhiteSpace())
                {
                    Log.Warn("Unable to detect the Classic Mods directory");
                    success = false;
                }
            }

            // Mod Values Directory:
            if (data.ModValuesDir.IsNullOrWhiteSpace() || !Directory.Exists(data.ModValuesDir))
            {
                data.ModValuesDir = ResolveModValuesDir(data.WorkDir);
                if (data.ModValuesDir.IsNullOrWhiteSpace())
                {
                    Log.Error("Unable to resolve the mod values directory");
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
        string dir = SpaceHavenLauncher.GetAppDir();
        Log.Debug($@"{nameof(ResolveAppDir)}: Resolved {SpaceHavenLauncher.APP_NAME} directory as ""{dir}""");
        return dir;
    }

    public string ResolveWorkDir()
    {
        // Windows C:\Users\<User>\AppData\Local
        // Linux   ~/.local/share
        // macOS   ~/Library/Application Support
        string dir = Path.TrimEndingDirectorySeparator(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpaceHavenLauncher"));

        try
        {
            if (!Directory.Exists(dir))
                IOUtils.TryCreateDirectory(dir, Log);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        // Done.
        Log.Debug($@"{nameof(ResolveWorkDir)}: Resolved {SpaceHavenLauncher.APP_NAME} work directory as ""{dir}""");
        return dir;
    }

    public string ResolveSteamDir(string appDir)
    {
        if (appDir.IsNullOrWhiteSpace())
            return null;

        if (appDir.Contains("979110"))
        {
            // This app is on Steam workshop folder!
            try
            {
                string dir =
                    Path.TrimEndingDirectorySeparator(
                    Path.GetDirectoryName(
                    Path.GetDirectoryName(
                    Path.GetDirectoryName(
                    Path.GetDirectoryName(
                    Path.GetDirectoryName(
                    appDir
                ))))));

                if (!dir.EndsWith("Steam", StringComparison.OrdinalIgnoreCase) || !Directory.Exists(dir))
                    return null;

                // Done.
                Log.Debug($@"{nameof(ResolveSteamDir)}: Auto detected Steam directory ""{dir}""");
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
            Log.Debug($@"{nameof(ResolveSteamDir)}: Testing possible directory ""{possibleDir}""");
            try
            {
                string dir = Path.TrimEndingDirectorySeparator(possibleDir);

                if (dir.StartsWith(".."))
                {
                    string baseDir = Path.TrimEndingDirectorySeparator(appDir);
                    do
                    {
                        dir = dir.Substring(3);
                        try { baseDir = Path.GetDirectoryName(baseDir); } catch { }
                    } while (dir.StartsWith(".."));

                    try
                    {
                        dir = Path.Combine(baseDir, dir);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                        continue;
                    }
                    Log.Debug($@"{nameof(ResolveSteamDir)}: Possible directory was evaluated to ""{dir}""");
                }

                else if (dir.StartsWith("~/"))
                {
                    try
                    {
                        string homeDir = Path.TrimEndingDirectorySeparator(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                        dir = dir.Substring(2);
                        dir = Path.Combine(homeDir, dir);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex);
                    }
                    Log.Debug($@"{nameof(ResolveSteamDir)}: Possible directory was evaluated to ""{dir}""");
                }

                // Test it:
                if (!Directory.Exists(dir))
                {
                    Log.Debug($@"{nameof(ResolveSteamDir)}: Directory not found ""{dir}""");
                    continue;
                }

                // Done.
                Log.Debug($@"{nameof(ResolveSteamDir)}: Auto detected Steam directory ""{dir}""");
                return dir;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        Log.Error($@"{nameof(ResolveSpaceHavenDir)}: Nothing could be found");
        return null;
    }

    public string ResolveSpaceHavenDir(string appDir)
    {
        if (appDir.IsNullOrWhiteSpace())
            return null;

        foreach (string possiblePath in PossibleSpaceHavenPaths)
        {
            try
            {
                Log.Debug($@"{nameof(ResolveSpaceHavenDir)}: Testing possible path ""{possiblePath}""");

                string path = possiblePath;

                if (path.StartsWith(".."))
                {
                    string baseDir = Path.TrimEndingDirectorySeparator(appDir);
                    do
                    {
                        path = path.Substring(3);
                        try { baseDir = Path.GetDirectoryName(baseDir); } catch { }
                    } while (path.StartsWith(".."));

                    try
                    {
                        path = Path.Combine(baseDir, path);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex);
                        continue;
                    }
                    Log.Debug($@"{nameof(ResolveSpaceHavenDir)}: Possible path was evaluated to ""{path}""");
                }

                else if (path.StartsWith("~/"))
                {
                    try
                    {
                        string homeDir = Path.TrimEndingDirectorySeparator(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                        path = path.Substring(2);
                        path = Path.Combine(homeDir, path);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex);
                    }
                    Log.Debug($@"{nameof(ResolveSpaceHavenDir)}: Possible path was evaluated to ""{path}""");
                }

                // Test it:
                if (!File.Exists(path))
                    continue;

                // Done.
                string dir = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(path));
                Log.Debug($@"{nameof(ResolveSpaceHavenDir)}: Auto detected Space Haven directory ""{dir}""");
                return dir;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        // Nothing was found.
        Log.Error($@"{nameof(ResolveSpaceHavenDir)}: Nothing could be found");
        return null;
    }

    public string ResolveSpaceHavenJarDir(string spaceHavenDir)
    {
        try
        {
            if (spaceHavenDir.IsNullOrWhiteSpace())
                return null;

            string spaceHavenJarPath =
                OS.IsWin ? Path.Combine(spaceHavenDir, "spacehaven.jar") :
                OS.IsMac ? Path.Combine(spaceHavenDir, "Contents", "Resources", "spacehaven.jar") :
                OS.IsLnx ? Path.Combine(spaceHavenDir, "spacehaven.jar") :
                throw new NotImplementedException("Unsupported operational system");

            Log.Debug($@"{nameof(ResolveSpaceHavenJarDir)}: Testing path ""{spaceHavenJarPath}""");
            if (!File.Exists(spaceHavenJarPath))
                return null;

            string dir = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(spaceHavenJarPath));
            Log.Debug($@"{nameof(ResolveSpaceHavenJarDir)}: Auto detected Space Haven JAR directory ""{dir}""");
            return dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }

    public string ResolveSteamModsDir(string steamDir)
    {
        try
        {
            if (steamDir.IsNullOrWhiteSpace())
                return null;
            string dir = Path.Combine(steamDir, "steamapps", "workshop", "content", "979110");
            if (!Directory.Exists(dir))
                return null;

            Log.Debug($@"{nameof(ResolveSteamModsDir)}: Auto detected Steam mods directory ""{dir}""");
            return dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }

    public string ResolveClassicModsDir(string spaceHavenJarDir)
    {
        try
        {
            if (spaceHavenJarDir.IsNullOrWhiteSpace())
                return null;
            string dir = Path.Combine(spaceHavenJarDir, "mods");
            if (!Directory.Exists(dir))
                if (!IOUtils.TryCreateDirectory(dir, Log))
                    return null;

            Log.Debug($@"{nameof(ResolveClassicModsDir)}: Auto detected classic mods directory ""{dir}""");
            return dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }

    public string ResolveModValuesDir(string workDir)
    {
        try
        {
            if (workDir.IsNullOrWhiteSpace())
                return null;
            string dir = Path.Combine(workDir, "values");
            if (!Directory.Exists(dir))
                if (!IOUtils.TryCreateDirectory(dir, Log))
                    return null;

            Log.Debug($@"{nameof(ResolveModValuesDir)}: Auto detected mod values directory ""{dir}""");
            return dir;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }

    private string ResolveTempDir()
    {
        //// Default temp directory -> Path.GetTempPath():
        //// Windows	C:\Users\<User>\AppData\Local\Temp\
        //// Linux	/tmp/ (or $TMPDIR if set)
        //// macOS	/var/folders/... (system-managed temp path)
        //Paths.SH.LauncherTempDir = Path.TrimEndingDirectorySeparator(Path.Combine(Path.GetTempPath(), "SpaceHavenSH.Launcher"));
        //if (!Directory.Exists(Paths.SH.LauncherTempDir))
        //{
        //    try { Directory.CreateDirectory(Paths.SH.LauncherTempDir); }
        //    catch (Exception ex)
        //    {
        //        Logger.Info(ex);
        //        return false;
        //    }
        //}
        return null;
    }



    public PathData TryLoad()
    {
        try
        {
            PathData data = new();
            data.AppDir = ResolveAppDir(); // force
            data.WorkDir = ResolveWorkDir(); // force

            if (data.PathSettingsPath.IsNullOrWhiteSpace() || !File.Exists(data.PathSettingsPath))
                return null;

            XDocument doc = XDocument.Load(data.PathSettingsPath);
            XElement rootNode = doc.Element("PathSettings");
            if (rootNode == null)
            {
                Log.Error($"Invalid root node, <PathSettings> is expected", data.PathSettingsPath);
                return null;
            }

            // TODO: Improve deserialization of settings by using version:
            string version = rootNode.Attribute("version")?.Value;

            data.SteamDir = rootNode.Element(nameof(PathData.SteamDir))?.Value?.Trim();
            data.SpaceHavenDir = rootNode.Element(nameof(PathData.SpaceHavenDir))?.Value?.Trim();
            data.SpaceHavenJarDir = rootNode.Element(nameof(PathData.SpaceHavenJarDir))?.Value?.Trim();
            data.SteamModsDir = rootNode.Element(nameof(PathData.SteamModsDir))?.Value?.Trim();
            data.ClassicModsDir = rootNode.Element(nameof(PathData.ClassicModsDir))?.Value?.Trim();
            data.ModValuesDir = rootNode.Element(nameof(PathData.ModValuesDir))?.Value?.Trim();

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



    public async Task<bool> TrySave(PathData data, CancellationToken ct)
    {
        try
        {
            XDocument doc = new();
            XElement rootNode = new("PathSettings");

            // TODO: Improve serialization of settings by using version:
            rootNode.SetAttributeValue("version", "1.0.0.0");

            doc.Add(rootNode);
            rootNode.Add(new XElement(nameof(PathData.SteamDir), data.SteamDir));
            rootNode.Add(new XElement(nameof(PathData.SpaceHavenDir), data.SpaceHavenDir));
            rootNode.Add(new XElement(nameof(PathData.SpaceHavenJarDir), data.SpaceHavenJarDir));
            rootNode.Add(new XElement(nameof(PathData.SteamModsDir), data.SteamModsDir));
            rootNode.Add(new XElement(nameof(PathData.ClassicModsDir), data.ClassicModsDir));
            rootNode.Add(new XElement(nameof(PathData.ModValuesDir), data.ModValuesDir));

            if (!await IOUtils.TrySaveXDocumentAsync(data.PathSettingsPath, doc, Log, ct))
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




    #region Possible Space Haven Locations

    private readonly IReadOnlyList<string> PossibleSpaceHavenPaths =

    OS.IsWin ?
    [
        //E:\SteamLibrary\steamapps\common\SpaceHaven

        // Steam:
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\steamapps\common\SpaceHaven\spacehaven.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Steam\steamapps\common\SpaceHaven\spacehaven.exe"),
        @"C:\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"C:\Games\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"D:\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"D:\Games\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"E:\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"E:\Games\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"F:\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"F:\Games\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"G:\Steam\steamapps\common\SpaceHaven\spacehaven.exe",
        @"G:\Games\Steam\steamapps\common\SpaceHaven\spacehaven.exe",

        // User-Defined:
        @"..\spacehaven.exe",
        @"..\..\spacehaven.exe",
        @"..\..\..\spacehaven.exe",
        @"..\SpaceHaven\spacehaven.exe",
        @"..\Space Haven\spacehaven.exe",
        @"..\..\SpaceHaven\spacehaven.exe",
        @"..\..\Space Haven\spacehaven.exe",
        @"C:\Games\SpaceHaven\spacehaven.exe",
        @"C:\Games\Space Haven\spacehaven.exe",
        @"E:\Games\SpaceHaven\spacehaven.exe",
        @"E:\Games\Space Haven\spacehaven.exe",
        @"F:\Games\SpaceHaven\spacehaven.exe",
        @"F:\Games\Space Haven\spacehaven.exe",
        @"G:\Games\SpaceHaven\spacehaven.exe",
        @"G:\Games\Space Haven\spacehaven.exe",

        // GOG:
        @"C:\GOG Games\SpaceHaven\spacehaven.exe",
        @"C:\GOG Games\Space Haven\spacehaven.exe",
        @"C:\GOG Games\SpaceHaven\game\spacehaven.exe",
        @"C:\GOG Games\Space Haven\game\spacehaven.exe",

        @"D:\GOG Games\SpaceHaven\spacehaven.exe",
        @"D:\GOG Games\Space Haven\spacehaven.exe",
        @"D:\GOG Games\SpaceHaven\game\spacehaven.exe",
        @"D:\GOG Games\Space Haven\game\spacehaven.exe",

        @"E:\GOG Games\SpaceHaven\spacehaven.exe",
        @"E:\GOG Games\Space Haven\spacehaven.exe",
        @"E:\GOG Games\SpaceHaven\game\spacehaven.exe",
        @"E:\GOG Games\Space Haven\game\spacehaven.exe",

        @"F:\GOG Games\SpaceHaven\spacehaven.exe",
        @"F:\GOG Games\Space Haven\spacehaven.exe",
        @"F:\GOG Games\SpaceHaven\game\spacehaven.exe",
        @"F:\GOG Games\Space Haven\game\spacehaven.exe",

        @"G:\GOG Games\SpaceHaven\spacehaven.exe",
        @"G:\GOG Games\Space Haven\spacehaven.exe",
        @"G:\GOG Games\SpaceHaven\game\spacehaven.exe",
        @"G:\GOG Games\Space Haven\game\spacehaven.exe",

        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games\SpaceHaven\spacehaven.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games\Space Haven\spacehaven.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games\SpaceHaven\game\spacehaven.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games\Space Haven\game\spacehaven.exe"),

        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games\SpaceHaven\spacehaven.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games\Space Haven\spacehaven.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games\SpaceHaven\game\spacehaven.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games\Space Haven\game\spacehaven.exe"),
    ] :

    OS.IsMac ?
    [
        // Steam:
        "~/Library/Application Support/Steam/steamapps/common/SpaceHaven/spacehaven.app",
        "~/Library/Application Support/Steam/steamapps/common/spacehaven/spacehaven.app",
        "~/Library/Application Support/Steam/steamapps/common/Space Haven/spacehaven.app",

        // User-Defined:
        "../spacehaven.app",
        "~/Games/spacehaven.app",
        "~/Applications/spacehaven.app",
        "~/Applications/Games/spacehaven.app",

        "~/Games/SpaceHaven/spacehaven.app",
        "~/Games/spacehaven/spacehaven.app",
        "~/Games/Space Haven/spacehaven.app",

        "~/Applications/Games/SpaceHaven/spacehaven.app",
        "~/Applications/Games/spacehaven/spacehaven.app",
        "~/Applications/Games/Space Haven/spacehaven.app",

        "~/Library/Application Support/SpaceHaven/spacehaven.app",
        "~/Library/Application Support/spacehaven/spacehaven.app",
        "~/Library/Application Support/Space Haven/spacehaven.app",

        // GOG:
        "~/Applications/GOG Games/spacehaven.app",

        "~/Applications/GOG Games/SpaceHaven/spacehaven.app",
        "~/Applications/GOG Games/spacehaven/spacehaven.app",
        "~/Applications/GOG Games/Space Haven/spacehaven.app",

        "~/Library/Application Support/GOG Games/SpaceHaven/spacehaven.app",
        "~/Library/Application Support/GOG Games/spacehaven/spacehaven.app",
        "~/Library/Application Support/GOG Games/Space Haven/spacehaven.app",

    ] :

    OS.IsLnx ?
    [
        // Steam:
        @"~/.steam/steam/steamapps/common/SpaceHaven/spacehaven",
        @"~/.steam/steam/steamapps/common/spacehaven/spacehaven",
        @"~/.steam/steam/steamapps/common/Space Haven/spacehaven",

        @"~/.local/share/Steam/steamapps/common/SpaceHaven/spacehaven",
        @"~/.local/share/Steam/steamapps/common/spacehaven/spacehaven",
        @"~/.local/share/Steam/steamapps/common/Space Haven/spacehaven",

        @"~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/SpaceHaven/spacehaven",
        @"~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/spacehaven/spacehaven",
        @"~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/Space Haven/spacehaven",

        // User-Defined:
        @"../SpaceHaven/spacehaven",
        @"../spacehaven/spacehaven",
        @"../Space Haven/spacehaven",

        @"../../SpaceHaven/spacehaven",
        @"../../spacehaven/spacehaven",
        @"../../Space Haven/spacehaven",

        @"~/Games/SpaceHaven/spacehaven",
        @"~/Games/spacehaven/spacehaven",
        @"~/Games/Space Haven/spacehaven",

        @"~/.local/share/SpaceHaven/spacehaven",
        @"~/.local/share/spacehaven/spacehaven",
        @"~/.local/share/Space Haven/spacehaven",

        @"~/.local/share/applications/SpaceHaven/spacehaven",
        @"~/.local/share/applications/spacehaven/spacehaven",
        @"~/.local/share/applications/Space Haven/spacehaven",

        @"/opt/SpaceHaven/spacehaven",
        @"/opt/spacehaven/spacehaven",
        @"/opt/Space Haven/spacehaven",

        // GOG:
        @"~/GOG Games/SpaceHaven/spacehaven",
        @"~/GOG Games/spacehaven/spacehaven",
        @"~/GOG Games/Space Haven/spacehaven",

        @"~/GOG Games/SpaceHaven/game/spacehaven",
        @"~/GOG Games/spacehaven/game/spacehaven",
        @"~/GOG Games/Space Haven/game/spacehaven",

        @"~/GOG Games/SpaceHaven/Game/spacehaven",
        @"~/GOG Games/spacehaven/Game/spacehaven",
        @"~/GOG Games/Space Haven/Game/spacehaven",
    ] :
    [];

    #endregion


    #region Possible Steam Loactions

    private readonly IReadOnlyList<string> PossibleSteamDirs =

    OS.IsWin ?
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
        @"C:\Games\Steam",
        @"C:\Steam",
        @"D:\Games\Steam",
        @"D:\Steam",
        @"E:\Games\Steam",
        @"E:\Steam",
    ] :

    OS.IsMac ?
    [
        @"~/Library/Application Support/Steam",
    ] :

    OS.IsLnx ?
    [
        @"~/.steam/steam",
        @"~/.steam/Steam",

        @"~/.local/share/steam",
        @"~/.local/share/Steam",

        @"~/.var/app/com.valvesoftware.Steam/.local/share/Steam",
        @"~/.var/app/com.valvesoftware.Steam/.local/share/Steam",
    ] :
    [];

    #endregion

}
