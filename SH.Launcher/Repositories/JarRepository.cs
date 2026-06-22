using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using ICSharpCode.SharpZipLib.Zip;
using SH.Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using SH.Content;

namespace SH.Launcher.Repositories;

public sealed class JarRepository
{
    private readonly PathData Paths;
    private readonly ILogger Log;
    private IProgressInfo Progress;

    public JarRepository(PathData paths, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = logger ?? new VoidLogger();
    }

    public async Task<VersionInfo> TryReadVersionAsync(string jarPath, ILogger logger)
    {
        string parent = null;
        try { parent = Path.GetDirectoryName(jarPath); } catch { }

        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(jarPath));

            if (!File.Exists(jarPath))
            {
                Log.Error($@"Could not find ""{jarPath}""", parent);
                return null;
            }

            using ZipFile zin = new(jarPath);
            ZipEntry entry = zin.GetEntry(SpaceHavenConstants.VERSION_TXT);
            if (entry == null)
            {
                Log.Error($@"Could not find {SpaceHavenConstants.VERSION_TXT} inside: ""{jarPath}""", parent);
                return null;
            }

            await using Stream stream = zin.GetInputStream(entry);
            using StreamReader reader = new(stream);
            string versionStr = (await reader.ReadToEndAsync()).Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).JoinToString("");
            return new VersionInfo(versionStr);
        }
        catch (Exception ex)
        {
            logger?.Error(ex, parent);
            return null;
        }
    }

    public async Task<bool> TryExportLibraryAsync(string jarPath, string outputDirectory, CancellationToken ct, IProgressInfo progressInfo)
    {
        int fileCount = 0;
        string jarDir = null;
        try { jarDir = Path.GetDirectoryName(jarPath); } catch { }

        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(jarPath));

            Progress = progressInfo;
            Progress?.SetNormalized(0.01);

            if (!File.Exists(jarPath))
            {
                Log.Error($@"Could not find ""{jarPath}""", jarDir);
                return false;
            }

            if (!await IOUtils.TryDeleteDirectoryAsync(outputDirectory, Log, ct))
            {
                Log.Error($@"Unable to clear output directory ""{outputDirectory}""", outputDirectory);
                return false;
            }
            if (!await IOUtils.TryCreateDirectoryAsync(outputDirectory, Log, ct))
            {
                Log.Error($@"Unable to create output directory ""{outputDirectory}""");
                return false;
            }

            using ZipFile zin = new(jarPath);

            // Progress pre-calculation:
            long sumCompressedSize = 0;
            long sumUncompressedSize = 0;
            long totalCompressedSize = 0;
            long totalUncompressedSize = 0;
            HashSet<string> entries = [];
            foreach (ZipEntry entry in zin)
            {
                if (entry.IsDirectory)
                    continue;
                string entryPath = entry.Name.AsStandardPath();
                if (!ModdingConstants.PathsForModding.Any(path => entryPath.StartsWith(path, StringComparison.OrdinalIgnoreCase) || entryPath.EndsWith(path, StringComparison.OrdinalIgnoreCase)))
                    continue;
                entries.Add(entryPath);

                totalCompressedSize += entry.CompressedSize;
                totalUncompressedSize += entry.Size;
            }

            foreach (ZipEntry entry in zin)
            {
                string entryPath = entry.Name.AsStandardPath();
                if (!entries.Contains(entryPath))
                    continue;

                Log.Debug($@"Extracting ""{entryPath}""", outputDirectory);

                string relativePath = entryPath.AsOSPath();
                string targetPath = Path.GetFullPath(Path.Combine(outputDirectory, relativePath)).AsOSPath();
                if (!targetPath.StartsWith(Path.GetFullPath(outputDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    Log.Error($@"Skipping ZIP entry ""{entryPath}"" because it escapes the output directory", jarDir);
                    continue;
                }

                if (entry.IsDirectory)
                {
                    if (await IOUtils.TryCreateDirectoryAsync(targetPath, Log, ct))
                        continue;
                    Log.Error($@"Unable to create output directory ""{targetPath}""", outputDirectory);
                    return false;
                }

                // Create directory for file:
                string dir = Path.GetDirectoryName(targetPath).AsOSPath();
                if (!dir.IsNullOrWhiteSpace() && !await IOUtils.TryCreateDirectoryAsync(dir, Log, ct))
                {
                    Log.Error($@"Unable to create output directory ""{dir}""", outputDirectory);
                    return false;
                }

                using Stream zipStream = zin.GetInputStream(entry);
                using FileStream outStream = File.Create(targetPath);
                await zipStream.CopyToAsync(outStream);

                ++fileCount;
                sumCompressedSize += entry.CompressedSize;
                sumUncompressedSize += entry.Size;
                Progress?.SetNormalized((sumCompressedSize + sumUncompressedSize) / (totalCompressedSize + totalUncompressedSize));
            }

            // Done.
            Progress?.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, jarDir);
            return false;
        }
        finally
        {
            Log.Info($@"{fileCount} file(s) exported", outputDirectory);
        }
    }

    public async Task<bool> TryExportAllAsync(string jarPath, string outputDirectory, CancellationToken ct, IProgressInfo progressInfo)
    {
        int fileCount = 0;
        string parent = null;
        try { parent = Path.GetDirectoryName(jarPath); } catch { }

        try
        {
            Progress = progressInfo;
            Progress?.SetNormalized(0.01);

            if (!File.Exists(jarPath))
            {
                Log.Error($@"Could not find ""{jarPath}""", parent);
                return false;
            }

            if (!await IOUtils.TryDeleteDirectoryAsync(outputDirectory, Log, ct))
            {
                Log.Error($@"Unable to clear output subdirectory ""{outputDirectory}""", outputDirectory);
                return false;
            }

            if (!await IOUtils.TryCreateDirectoryAsync(outputDirectory, Log, ct))
            {
                Log.Error($@"Unable to create output subdirectory ""{outputDirectory}""");
                return false;
            }

            using ZipFile zin = new(jarPath);

            ct.ThrowIfCancellationRequested();

            // Progress pre-calculation:
            long sumCompressedSize = 0;
            long sumUncompressedSize = 0;
            long totalCompressedSize = 0;
            long totalUncompressedSize = 0;
            foreach (ZipEntry entry in zin)
            {
                ct.ThrowIfCancellationRequested();

                if (entry.IsDirectory)
                    continue;
                totalCompressedSize += entry.CompressedSize;
                totalUncompressedSize += entry.Size;
            }

            // Export everything:
            foreach (ZipEntry entry in zin)
            {
                ct.ThrowIfCancellationRequested();

                string path = Path.GetFullPath(Path.Combine(outputDirectory, entry.Name));

                if (!path.StartsWith(Path.GetFullPath(outputDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    Log.Error($@"Skipping ZIP entry ""{entry.Name}"" because it escapes the output directory", parent);
                    return false;
                }

                if (entry.IsDirectory)
                {
                    if (await IOUtils.TryCreateDirectoryAsync(path, Log, ct))
                        continue;
                    Log.Error($@"Unable to create output subdirectory ""{path}""", outputDirectory);
                    return false;
                }

                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !await IOUtils.TryCreateDirectoryAsync(dir, Log, ct))
                {
                    Log.Error($@"Unable to create output subdirectory ""{dir}""", outputDirectory);
                    return false;
                }

                using Stream zipStream = zin.GetInputStream(entry);
                using FileStream outStream = File.Create(path);
                await zipStream.CopyToAsync(outStream);

                ++fileCount;
                sumCompressedSize += entry.CompressedSize;
                sumUncompressedSize += entry.Size;
                Progress?.SetNormalized((sumCompressedSize + sumUncompressedSize) / (totalCompressedSize + totalUncompressedSize));
            }

            // Done.
            Progress?.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
        finally
        {
            Log.Info($@"{fileCount} file(s) exported", outputDirectory);
        }
    }


}
