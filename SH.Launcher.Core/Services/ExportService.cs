using ICSharpCode.SharpZipLib.Zip;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class ExportService
{
    private readonly ILogger Log;

    public ExportService(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    public async Task<bool> TryExportLibraryAsync(string jarPath, string outputDirectory, CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => TryExportLibraryInternalAsync(jarPath, outputDirectory, ct, progress));
    private async Task<bool> TryExportLibraryInternalAsync(string jarPath, string outputDirectory, CancellationToken ct, IProgressInfo progress)
    {
        int fileCount = 0;
        string jarDir = null;
        try { jarDir = Path.GetDirectoryName(jarPath); } catch { }

        try
        {
            progress?.Start();
            outputDirectory = Path.GetFullPath(outputDirectory).AsOSPath();

            if (!IOUtils.FileExists(jarPath))
            {
                Log.Error($@"Could not find ""{jarPath}""", jarDir);
                return false;
            }

            // Clear:
            Log.Info("Clearing export directory...");
            if (!await IOUtils.TryDeleteDirContentAsync(outputDirectory, Log, ct))
            {
                Log.Error($@"Unable to clear output directory ""{outputDirectory}""", outputDirectory);
                return false;
            }
            if (!await IOUtils.TryCreateDirAsync(outputDirectory, Log, ct))
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
                string entryPath = entry.Name.AsStdPath();
                if (!ModdingConstants.PathsForModding.Any(path => entryPath.StartsWith(path, StringComparison.OrdinalIgnoreCase) || entryPath.EndsWith(path, StringComparison.OrdinalIgnoreCase)))
                    continue;
                entries.Add(entryPath);

                totalCompressedSize += entry.CompressedSize;
                totalUncompressedSize += entry.Size;
            }

            foreach (ZipEntry entry in zin)
            {
                string entryPath = entry.Name.AsStdPath();
                if (!entries.Contains(entryPath))
                    continue;

                Log.Debug($@"Extracting ""{entryPath}""", outputDirectory);

                string targetPath = IOUtils.CombineAsOSPath(outputDirectory, entryPath);
                if (!targetPath.StartsWith(Path.GetFullPath(outputDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    Log.Error($@"Skipping ZIP entry ""{entryPath}"" because it escapes the output directory", jarDir);
                    continue;
                }

                if (entry.IsDirectory)
                {
                    if (await IOUtils.TryCreateDirAsync(targetPath, Log, ct))
                        continue;
                    Log.Error($@"Unable to create output directory ""{targetPath}""", outputDirectory);
                    return false;
                }

                // Create directory for file:
                string dir = Path.GetDirectoryName(targetPath).AsOSPath();
                if (!dir.IsNullOrWhiteSpace() && !await IOUtils.TryCreateDirAsync(dir, Log, ct))
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
                progress?.SetNormalized((sumCompressedSize + sumUncompressedSize) / (totalCompressedSize + totalUncompressedSize));
            }

            // Done.
            progress?.Complete();
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
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

    public async Task<bool> TryExportAllAsync(string jarPath, string outputDirectory, CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => TryExportAllInternalAsync(jarPath, outputDirectory, ct, progress));
    private async Task<bool> TryExportAllInternalAsync(string jarPath, string outputDirectory, CancellationToken ct, IProgressInfo progress)
    {
        int fileCount = 0;
        string parent = null;
        try { parent = Path.GetDirectoryName(jarPath); } catch { }

        try
        {
            progress?.Start();
            outputDirectory = Path.GetFullPath(outputDirectory).AsOSPath();

            if (!IOUtils.FileExists(jarPath))
            {
                Log.Error($@"Could not find ""{jarPath}""", parent);
                return false;
            }

            if (!await IOUtils.TryDeleteDirContentAsync(outputDirectory, Log, ct))
            {
                Log.Error($@"Unable to clear output subdirectory ""{outputDirectory}""", outputDirectory);
                return false;
            }

            if (!await IOUtils.TryCreateDirAsync(outputDirectory, Log, ct))
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

                string targetPath = IOUtils.CombineAsOSPath(outputDirectory, entry.Name);
                if (!targetPath.StartsWith(outputDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Error($@"Skipping ZIP entry ""{entry.Name}"" because it escapes the output directory", parent);
                    return false;
                }

                if (entry.IsDirectory)
                {
                    if (await IOUtils.TryCreateDirAsync(targetPath, Log, ct))
                        continue;
                    Log.Error($@"Unable to create output subdirectory ""{targetPath}""", outputDirectory);
                    return false;
                }

                string dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !await IOUtils.TryCreateDirAsync(dir, Log, ct))
                {
                    Log.Error($@"Unable to create output subdirectory ""{dir}""", outputDirectory);
                    return false;
                }

                using Stream zipStream = zin.GetInputStream(entry);
                using FileStream outStream = File.Create(targetPath);
                await zipStream.CopyToAsync(outStream);

                ++fileCount;
                sumCompressedSize += entry.CompressedSize;
                sumUncompressedSize += entry.Size;
                progress?.SetNormalized((sumCompressedSize + sumUncompressedSize) / (totalCompressedSize + totalUncompressedSize));
            }

            // Done.
            progress?.Complete();
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
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
