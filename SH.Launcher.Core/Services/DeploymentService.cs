using ICSharpCode.SharpZipLib.Zip;
using SH.Content;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Modding;
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

    public DeploymentService(PathData paths, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = logger ?? new VoidLogger();
    }

    private async Task<bool> IsModifiedJar(string jarPath)
    {
        JarRepositoryService repo = new(Log);
        VersionInfo version = await repo.TryReadVersionAsync(jarPath, Log);
        return version.ToString().Contains("MODIFIED", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> TryBackupOriginalAsync(CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => TryBackupOriginalInternalAsync(ct, progress));
    private async Task<bool> TryBackupOriginalInternalAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress?.Start();

            bool spaceHavenJarIsOriginal = File.Exists(Paths.SpaceHavenJarPath) && !await IsModifiedJar(Paths.SpaceHavenJarPath);

            if (spaceHavenJarIsOriginal)
            {
                string jarHash = await XxHash64Calculator.ComputeFromFileAsync(Paths.SpaceHavenJarPath, Log, ct);

                progress?.SetNormalized(0.50);

                // Check if it is the same as what we have in 'original' directory:
                if (File.Exists(Paths.BackupConfigJsonPath) &&
                    File.Exists(Paths.BackupJarPath) &&
                    File.Exists(Paths.BackupJarHashPath) &&
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
            else if (File.Exists(Paths.BackupJarPath) && File.Exists(Paths.BackupJarHashPath))
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
            if (File.Exists(Paths.BackupJarPath) && File.Exists(Paths.BackupJarHashPath) && File.Exists(Paths.BackupConfigJsonPath) &&
                File.Exists(Paths.TemplateJarPath) && File.Exists(Paths.TemplateJarHashPath) && File.Exists(Paths.TemplateConfigJsonPath))
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
            if (File.Exists(Paths.BackupJarPath) && File.Exists(Paths.BackupJarHashPath) &&
                File.Exists(Paths.TemplateJarPath) && File.Exists(Paths.TemplateJarHashPath) &&
                File.Exists(Paths.CacheJarPath) && File.Exists(Paths.CacheJarHashPath))
            {
                string originalJarHash = File.ReadAllText(Paths.BackupJarHashPath);
                string templateJarHash = File.ReadAllText(Paths.TemplateJarHashPath);
                string modifiedJarHash = File.ReadAllText(Paths.CacheJarHashPath);
                if (originalJarHash == templateJarHash && templateJarHash == modifiedJarHash)
                    return true;
            }
            progress?.SetNormalized(0.10);

            ct.ThrowIfCancellationRequested();

            // Otherwise clean the cached modified jar:
            if (!await IOUtils.TryDeleteDirectoryAsync(Paths.CacheDir, Log, ct))
                return false;
            ct.ThrowIfCancellationRequested();
            return await IOUtils.TryCreateDirectoryAsync(Paths.CacheDir, Log, ct);
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
            if (!await IOUtils.TryDeleteDirectoryAsync(Paths.TemplateDir, Log, ct))
                return false;
            if (!await IOUtils.TryCreateDirectoryAsync(Paths.TemplateDir, Log, ct))
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
            if (!await IOUtils.TryCreateDirectoryAsync(Paths.TemplateStageDir, Log, ct))
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
                        if (!string.IsNullOrEmpty(extractDir) && !await IOUtils.TryCreateDirectoryAsync(extractDir, Log, ct))
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

    public async Task<bool> RestoreOriginalGameAsync(CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => RestoreOriginalGameInternalAsync(ct, progress));
    public async Task<bool> RestoreOriginalGameInternalAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress.Start();
            Log.Info($"Restoring ORIGINAL game...");

            // Restore original config.json file to space haven directory:
            Log.Info($@"Restoring ""{SpaceHavenConstants.CONFIG_JSON}""...");
            ConfigJsonFile config = await ConfigJsonFile.TryLoadAsync(Paths.BackupConfigJsonPath, Log, ct);
            if(config == null)
            {
                Log.Warn($@"Unable to restore ""{SpaceHavenConstants.CONFIG_JSON}"" from backup, generating a new default one");
                if(Paths.SteamDir.IsNullOrWhiteSpace())
                {
                    Log.Warn($@"Generating a ""{SpaceHavenConstants.CONFIG_JSON}"" for Steam...");
                    if((config = ConfigJsonFile.GetDefaultForSteam()) == null) // errors should never happen
                        throw new NotImplementedException($@"Generation of a new Steam ""{SpaceHavenConstants.CONFIG_JSON}""");
                }
                else // we assume GOG
                {
                    Log.Warn($@"Generating a ""{SpaceHavenConstants.CONFIG_JSON} for GOG""...");
                    if((config = ConfigJsonFile.GetDefaultForGOG()) == null) // errors should never happen
                        throw new NotImplementedException($@"Generation of a new GOG ""{SpaceHavenConstants.CONFIG_JSON}""");

                }

                config =
                    (Paths.SteamDir.IsNullOrWhiteSpace() ? ConfigJsonFile.GetDefaultForGOG() : ConfigJsonFile.GetDefaultForSteam()) // Fallback
                    ?? throw new NotImplementedException($@"Generation of a new ""{SpaceHavenConstants.CONFIG_JSON}"" file"); // should never happen
            }
            config.ClassPath.Remove(ModdingConstants.MODIFIED_SPACEHAVEN_JAR); // remove modified
            config.ClassPath.Remove(SpaceHavenConstants.SPACEHAVEN_JAR); // avoids duplicate
            config.ClassPath.Add(SpaceHavenConstants.SPACEHAVEN_JAR); // as last JAR
            progress.SetNormalized(0.20);

            // Deploy restored config.json file:
            Log.Info($@"Deploying ""{SpaceHavenConstants.CONFIG_JSON}""...");
            if (!await IOUtils.TryWriteAllTextAsync(Paths.SpaceHavenConfigJsonPath, config.ToJsonString(), Log, ct))
                return false;
            progress.SetNormalized(0.40);

            // Try to delete modifiedspacehaven.jar, but do not stop on errors:
            Log.Info($@"Removing ""{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}""...");
            if (File.Exists(Paths.SpaceHavenModifiedJarPath))
                await IOUtils.TryDeleteFileAsync(Paths.SpaceHavenModifiedJarPath, Log, ct);
            progress.SetNormalized(0.60);

            // Try to delete mods.json, but do not stop on errors:
            Log.Info($@"Removing ""{ModdingConstants.MODS_JSON}""...");
            if (File.Exists(Paths.SpaceHavenModsJsonPath))
                await IOUtils.TryDeleteFileAsync(Paths.SpaceHavenModsJsonPath, Log, ct);
            progress.SetNormalized(0.80);

            // Try to delete AOP libraries, but do not stop on error:
            Log.Info($@"Removing ""{ModdingConstants.ASPECTJ}""...");
            if (File.Exists(Paths.SpaceHavenAspectJPath))
                await IOUtils.TryDeleteFileAsync(Paths.SpaceHavenAspectJPath, Log, ct);
            Log.Info($@"Removing ""{ModdingConstants.ASPECTJWEAVER}""...");
            if (File.Exists(Paths.SpaceHavenAspectJWeaverPath))
                await IOUtils.TryDeleteFileAsync(Paths.SpaceHavenAspectJWeaverPath, Log, ct);

            // Done.
            progress.Complete();
            Log.Success($@"ORIGINAL game was successfully restored");
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable to restore ORIGINAL game: {ex}");
            return false;
        }
    }

    public async Task<bool> DeployModifiedGameAsync(bool hasXmlMods, bool hasJavaMods, CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => DeployModifiedGameInternalAsync(hasXmlMods, hasJavaMods, ct, progress));
    private async Task<bool> DeployModifiedGameInternalAsync(bool hasXmlMods, bool hasJavaMods, CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            Log.Info($"Deploying MODIFIED game...");

            if (hasXmlMods)
            {
                // Deploy modified JAR file

                // Check if target JAR hash differs:
                Log.Info($@"Deploying ""{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}""...");
                string hash = await XxHash64Calculator.ComputeFromFileAsync(Paths.SpaceHavenModifiedJarPath, Log, ct);
                if (hash.IsNullOrEmpty() || hash != await IOUtils.TryReadAllTextAsync(Paths.CacheModifiedJarHashPath, Log, ct))
                {
                    // Copy modified JAR to space haven directory:
                    if (!await IOUtils.TryCopyFileAsync(Paths.CacheJarPath, Paths.SpaceHavenModifiedJarPath, true, Log, ct))
                        return false;
                    Log.Debug($@"Deployment of ""{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}"" to ""{Paths.SpaceHavenModifiedJarPath}"" was successful");
                }
                else Log.Info($@"Deployment of ""{ModdingConstants.MODIFIED_SPACEHAVEN_JAR}"" was skipped, since it is up-to-date");
            }

            if (hasJavaMods)
            {
                // Forcefully deploy AOP libraries:
                Log.Info($@"Deploying JAVA AOP libraries...");
    
                if (!await IOUtils.TryCopyFileAsync(Paths.AppAspectJPath, Paths.SpaceHavenAspectJPath, true, Log, ct))
                {
                    Log.Error($@"Unable to deploy ""{ModdingConstants.ASPECTJ}""");
                    return false;
                }
                else Log.Debug($@"Deployment of ""{ModdingConstants.ASPECTJ}"" to ""{Paths.SpaceHavenAspectJPath}"" was successful");

                if (!await IOUtils.TryCopyFileAsync(Paths.AppAspectJWeaverPath, Paths.SpaceHavenAspectJWeaverPath, true, Log, ct))
                {
                    Log.Error($@"Unable to deploy ""{ModdingConstants.ASPECTJWEAVER}""");
                    return false;
                }
                else Log.Debug($@"Deployment of ""{ModdingConstants.ASPECTJWEAVER}"" to ""{Paths.SpaceHavenAspectJWeaverPath}"" was successful");
            }

            // Copy config.json file to space haven directory:
            Log.Info($@"Deploying ""{SpaceHavenConstants.CONFIG_JSON}""...");
            if (!await IOUtils.TryCopyFileAsync(Paths.CacheConfigJsonPath, Paths.SpaceHavenConfigJsonPath, true, Log, ct))
            {
                Log.Error($@"Unable to deploy ""{SpaceHavenConstants.CONFIG_JSON}"" to ""{Paths.SpaceHavenConfigJsonPath}""");
                return false;
            }
            else Log.Debug($@"Deployment of ""{SpaceHavenConstants.CONFIG_JSON}"" to ""{Paths.SpaceHavenConfigJsonPath}"" was successful");

            // Copy mods.json file to space haven directory:
            if (!await IOUtils.TryCopyFileAsync(Paths.CacheModsJsonPath, Paths.SpaceHavenModsJsonPath, true, Log, ct))
            {
                Log.Error($@"Unable to deploy ""{ModdingConstants.MODS_JSON}"" to ""{Paths.SpaceHavenModsJsonPath}""");
                return false;
            }
            else Log.Debug($@"Deployment of ""{ModdingConstants.MODS_JSON}"" to ""{Paths.SpaceHavenModsJsonPath}"" was successful");

            // Done.
            Log.Success($"MODIFIED game was successfully deployed");
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to deploy MODIFIED game: {ex}");
            return false;
        }
    }

}

