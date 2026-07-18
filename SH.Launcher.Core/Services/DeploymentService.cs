using ICSharpCode.SharpZipLib.Zip;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Modding;
using SH.Modding.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

/// <summary>
/// Manipulates files (to/from) game directory
/// </summary>
public sealed class DeploymentService
{
    private readonly PathData Paths;
    private readonly ILogger Log;

    public DeploymentService(PathData paths, ILogger log)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = log ?? new VoidLogger();
    }

    private async Task<bool> IsModifiedJar(string jarPath)
    {
        JarRepositoryService repo = new(Log);
        VersionInfo version = await repo.TryReadVersionAsync(jarPath);
        return version.ToString().Contains("MODIFIED", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> TryBackupOriginalAsync(CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => TryBackupOriginalInternalAsync(ct, progress));
    private async Task<bool> TryBackupOriginalInternalAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress?.Start();

            bool spaceHavenJarIsOriginal = IOUtils.FileExists(Paths.SpaceHavenJarPath) && !await IsModifiedJar(Paths.SpaceHavenJarPath);

            if (spaceHavenJarIsOriginal)
            {
                string jarHash = await XxHash64Calculator.ComputeFromFileAsync(Paths.SpaceHavenJarPath, Log, ct);

                progress?.SetNormalized(0.50);

                // Check if it is the same as what we have in 'original' directory:
                if (IOUtils.FileExists(Paths.BackupConfigJsonPath) &&
                    IOUtils.FileExists(Paths.BackupJarPath) &&
                    IOUtils.FileExists(Paths.BackupJarHashPath) &&
                    jarHash == File.ReadAllText(Paths.BackupJarHashPath))
                {
                    // Last used original jar file matches the jar file in game's dir.
                    return true;
                }
                else
                {
                    // Copy the new original spacehaven.jar file to 'original' folder:
                    if (!await IOUtils.TryCopyFileAsync(Paths.SpaceHavenConfigJsonPath, Paths.BackupConfigJsonPath, true, Log, ct))
                        return false;

                    // Copy the new original spacehaven.jar file to 'original' folder:
                    if (!await IOUtils.TryCopyFileAsync(Paths.SpaceHavenJarPath, Paths.BackupJarPath, true, Log, ct))
                        return false;

                    // Write updated hash file:
                    File.WriteAllText(Paths.BackupJarHashPath, jarHash);

                    // Done.
                    return true;
                }
            }

            // Try to use whatever jar there is in the 'original' directory:
            else if (IOUtils.FileExists(Paths.BackupJarPath) && IOUtils.FileExists(Paths.BackupJarHashPath))
                return true;

            // There is no original/spacehaven.jar, and there is no SpaceHaven/spacehaven.jar file !!!
            else return false;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BackupDir);
            return false;
        }
        finally
        {
            progress?.Complete();
        }
    }

    public async Task<bool> TryPrepareTemplateAsync(CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => TryPrepareTemplateInternalAsync(ct, progress));
    private async Task<bool> TryPrepareTemplateInternalAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress?.Start();

            // Try to reuse existing template:
            if (IOUtils.FileExists(Paths.BackupJarPath) && IOUtils.FileExists(Paths.BackupJarHashPath) && IOUtils.FileExists(Paths.BackupConfigJsonPath) &&
                IOUtils.FileExists(Paths.TemplateJarPath) && IOUtils.FileExists(Paths.TemplateJarHashPath) && IOUtils.FileExists(Paths.TemplateConfigJsonPath))
            {
                string originalJarHash = File.ReadAllText(Paths.BackupJarHashPath);
                string templateJarHash = File.ReadAllText(Paths.TemplateJarHashPath);
                if (originalJarHash == templateJarHash)
                    return true;
            }

            ct.ThrowIfCancellationRequested();

            // Otherwise, cleanup and rebuild a new template jar:
            return await TryRebuildTemplateAsync(ct, progress);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.TemplateDir);
            return false;
        }
        finally
        {
            progress?.Complete();
        }
    }

    public async Task<bool> TryValidateModifiedCacheAsync(CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => TryValidateModifiedCacheInternalAsync(ct, progress));
    private async Task<bool> TryValidateModifiedCacheInternalAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress?.Start();

            // Try to reuse existing modified:
            if (IOUtils.FileExists(Paths.BackupJarPath) && IOUtils.FileExists(Paths.BackupJarHashPath) &&
                IOUtils.FileExists(Paths.TemplateJarPath) && IOUtils.FileExists(Paths.TemplateJarHashPath) &&
                IOUtils.FileExists(Paths.CacheJarPath) && IOUtils.FileExists(Paths.CacheJarHashPath))
            {
                string originalJarHash = File.ReadAllText(Paths.BackupJarHashPath);
                string templateJarHash = File.ReadAllText(Paths.TemplateJarHashPath);
                string modifiedJarHash = File.ReadAllText(Paths.CacheJarHashPath);
                if (originalJarHash == templateJarHash && templateJarHash == modifiedJarHash)
                    return true;
            }
            progress?.SetNormalized(0.10);

            ct.ThrowIfCancellationRequested();

            // Otherwise reset the cached modified jar:
            if (!await IOUtils.TryDeleteDirContentAsync(Paths.CacheDir, Log, ct))
                return false;

            ct.ThrowIfCancellationRequested();

            return await IOUtils.TryCreateDirAsync(Paths.CacheDir, Log, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.CacheDir);
            return false;
        }
        finally
        {
            progress?.Complete();
        }
    }

    private async Task<bool> TryRebuildTemplateAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            // Cleanup:
            if (!await IOUtils.TryDeleteDirContentAsync(Paths.TemplateDir, Log, ct))
                return false;
            if (!await IOUtils.TryCreateDirAsync(Paths.TemplateDir, Log, ct))
                return false;

            // Copy config.json file:
            if (!await IOUtils.TryCopyFileAsync(Paths.BackupConfigJsonPath, Paths.TemplateConfigJsonPath, true, Log, ct))
                return false;

            // Build jar template:
            if (!await TryBuildTemplateJarAsync(ct, progress))
                return false;

            return await IOUtils.TryCopyFileAsync(Paths.BackupJarHashPath, Paths.TemplateJarHashPath, true, Log, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.TemplateDir);
            return false;
        }
    }

    private async Task<bool> TryBuildTemplateJarAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            if (!await IOUtils.TryCreateDirAsync(Paths.TemplateStageDir, Log, ct))
                return false;

            using ZipFile zin = new(Paths.BackupJarPath);
            using FileStream fsout = File.Create(Paths.TemplateJarPath);
            using ZipOutputStream zout = new(fsout);
            zout.SetLevel(0);

            ct.ThrowIfCancellationRequested();

            // For progress calculation only:
            long compressed = 0;
            long uncompressed = 0;
            double totalCompressed = 0;
            double totalUncompressed = 0;
            if (progress != null)
            {
                foreach (ZipEntry ein in zin)
                {
                    totalCompressed += ein.CompressedSize;
                    totalUncompressed += ein.Size;
                }
            }

            ct.ThrowIfCancellationRequested();

            // Build Template:
            foreach (ZipEntry ein in zin)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    string name = ein.Name.AsStdPath() ?? string.Empty;
                    if (name.IsNullOrWhiteSpace())
                        continue;

                    // Skip moddable files, and extract them, so they can be used later for modding:
                    if (ModdingConstants.PathsForModding.Any(path => name.StartsWith(path, StringComparison.Ordinal) || name.EndsWith(path, StringComparison.Ordinal)))
                    {
                        if (ein.IsDirectory)
                            continue;

                        string extractPath = Path.Combine(Paths.TemplateStageDir, ein.Name);
                        string extractDir = Path.GetDirectoryName(extractPath);
                        if (!string.IsNullOrEmpty(extractDir) && !await IOUtils.TryCreateDirAsync(extractDir, Log, ct))
                            return false;
                        using Stream inStream = zin.GetInputStream(ein);
                        using FileStream outStream = File.Create(extractPath);
                        await inStream.CopyToAsync(outStream, ct);
                        File.SetLastWriteTime(extractPath, ein.DateTime);
                    }

                    // Include all other original files:
                    else
                    {
                        ZipEntry eout = new(ein.Name)
                        {
                            Flags = ein.Flags,
                            Size = ein.Size,
                            Crc = ein.Crc,
                            DateTime = ein.DateTime,
                            CompressionMethod = CompressionMethod.Stored, // ein.CompressionMethod,
                            ExternalFileAttributes = ein.ExternalFileAttributes,
                        };

                        zout.PutNextEntry(eout);

                        if (!ein.IsDirectory)
                        {
                            // Files only:
                            using Stream input = zin.GetInputStream(ein);
                            input.CopyTo(zout);
                        }

                        zout.CloseEntry();
                    }
                }
                finally
                {
                    if (progress != null && !ein.IsDirectory)
                    {
                        compressed += ein.CompressedSize;
                        uncompressed += ein.Size;
                        progress.SetNormalized((compressed + uncompressed) / (totalCompressed + totalUncompressed));
                    }
                }
            }
            zout.Finish();

            // Done.
            progress?.Complete();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.TemplateDir);
            return false;
        }
    }



}

