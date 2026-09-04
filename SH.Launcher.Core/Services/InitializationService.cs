using ICSharpCode.SharpZipLib.Zip;
using SH.Content;
using SH.Content.Enums;
using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using SH.Modding;
using SH.Modding.Models;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class InitializationService
{
    private readonly PathData Paths;
    private readonly ILogger Log;
    private readonly IProgressInfo BackupProgress;
    private readonly IProgressInfo TemplateProgress;
    private readonly IProgressInfo CacheProgress;

    public InitializationService(PathData paths, ILogger log, IProgressInfo backupProgress, IProgressInfo templateProgress, IProgressInfo cacheProgress)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = log ?? new VoidLogger();
        BackupProgress = backupProgress ?? new VoidProgressInfo();
        TemplateProgress = templateProgress ?? new VoidProgressInfo();
        CacheProgress = cacheProgress ?? new VoidProgressInfo();
    }

    public async Task<InitializationData> InitializeAsync(bool forceReset, CancellationToken ct) => await Task.Run(() =>
        InitializeInternalAsync(forceReset, ct));
    private async Task<InitializationData> InitializeInternalAsync(bool forceReset, CancellationToken ct)
    {
        try
        {
            BackupProgress.Reset();
            TemplateProgress.Reset();
            CacheProgress.Reset();

            if (forceReset)
            {
                Log.Info($"Forcing reset of {SpaceHavenLauncher.Name} directories...");
                await IOUtils.TryDeleteDirContentAsync(Paths.BackupDir, Log, ct);
                await IOUtils.TryDeleteDirContentAsync(Paths.TemplateDir, Log, ct);
                await IOUtils.TryDeleteDirContentAsync(Paths.BuildDir, Log, ct);
                await IOUtils.TryDeleteDirContentAsync(Paths.CacheDir, Log, ct);
            }


            // Result container:
            InitializationData data = new();


            // BACKUP:
            if (!await TryBackupAsync(data, ct))
                return null;


            // TEMPLATE:
            if (!await Task.Run(() => TryPrepareTemplateAsync(data, ct)))
                return null;


            // CACHE:
            if (!await Task.Run(() => TryValidateCacheAsync(data, ct)))
                return null;


            // SPACE HAVEN VERSION:
            data.SpaceHavenVersion = await TryReadVersionAsync(ct);
            if (data.SpaceHavenVersion == null)
                return null;


            // GAME PLATFORM:
            EGamePlatform? gamePlatform = await TryReadGamePlatformAsync(ct);
            if (gamePlatform == null || !gamePlatform.HasValue)
                return null;
            data.GamePlatform = gamePlatform.Value;


            // JAVA Main Class:
            data.JavaMainClass = SpaceHavenConstants.GetDefaultMainClass(data.GamePlatform);


            // JAVA VMArgs:
            data.JavaVMArgs = SpaceHavenConstants.GetDefaultVMArgs(OS.Type).JoinToString(" ");


            // Done.
            Log.Success($"{SpaceHavenLauncher.Name} initialization is complete", Paths.WorkDir);
            return data;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.WorkDir);
            return null;
        }
    }

    private async Task<bool> TryBackupAsync(InitializationData data, CancellationToken ct)
    {
        try
        {
            Log.Debug("Validating backup...");
            BackupProgress.Start();

            string gameJarHash = await XxHash64Calculator.ComputeFromFileAsync(Paths.SpaceHavenJarPath, Log, ct);
            BackupProgress.SetNormalized(0.10);

            // Update Backup:
            if (IOUtils.FileExists(Paths.BackupJarPath) && IOUtils.FileExists(Paths.BackupJarHashPath))
            {
                string backupJarHash = await IOUtils.TryReadAllTextAsync(Paths.BackupJarHashPath, Log, ct);
                BackupProgress.SetNormalized(0.20);

                // Reuse files:
                if (gameJarHash == backupJarHash)
                {
                    Log.Info($"Backup was reused");
                    BackupProgress.Complete();
                    data.BackupChanged = false;
                    return true;
                }
            }

            // Reset Backup:
            Log.Debug("Resetting backup...");

            // Cleanup:
            if (!await IOUtils.TryCreateDirAsync(Paths.BackupDir, Log, ct))
                return false;
            if (!await IOUtils.TryDeleteDirContentAsync(Paths.BackupDir, Log, ct))
                return false;
            BackupProgress.SetNormalized(0.50);

            // Copy spacehaven.jar:
            if (!await IOUtils.TryCopyFileAsync(Paths.SpaceHavenJarPath, Paths.BackupJarPath, true, Log, ct))
                return false;
            BackupProgress.SetNormalized(0.95);

            // Create hash file:
            if (!await IOUtils.TryWriteAllTextAsync(Paths.BackupJarHashPath, gameJarHash, Log, ct))
                return false;

            // Done.
            Log.Info($"Backup was reset");
            BackupProgress.Complete();
            data.BackupChanged = true;
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BackupDir);
            return false;
        }
        finally
        {
            BackupProgress?.Complete();
        }
    }

    private async Task<bool> TryPrepareTemplateAsync(InitializationData data, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            Log.Debug("Validating template...");
            TemplateProgress.Start();

            string backupJarHash = await IOUtils.TryReadAllTextAsync(Paths.BackupJarHashPath, Log, ct);
            TemplateProgress.SetNormalized(0.10);

            // Try to reuse existing template:
            if (IOUtils.FileExists(Paths.BackupJarPath) && IOUtils.FileExists(Paths.BackupJarHashPath) &&
                IOUtils.FileExists(Paths.TemplateJarPath) && IOUtils.FileExists(Paths.TemplateJarHashPath))
            {
                string templateJarHash = await IOUtils.TryReadAllTextAsync(Paths.TemplateJarHashPath, Log, ct);
                TemplateProgress.SetNormalized(0.20);

                if (!data.BackupChanged && backupJarHash == templateJarHash)
                {
                    Log.Info($"Template was reused");
                    TemplateProgress.Complete();
                    data.TemplateChanged = false;
                    return true;
                }
            }

            // Reset Template:
            Log.Debug("Resetting template...");

            // Cleanup:
            if (!await IOUtils.TryCreateDirAsync(Paths.TemplateDir, Log, ct))
                return false;
            if (!await IOUtils.TryDeleteDirContentAsync(Paths.TemplateDir, Log, ct))
                return false;
            TemplateProgress.SetNormalized(0.50);

            // Build template:
            if (!await TryBuildTemplateJarAsync(ct))
                return false;
            TemplateProgress.SetNormalized(0.95);

            // Copy hash file:
            if (!await IOUtils.TryCopyFileAsync(Paths.BackupJarHashPath, Paths.TemplateJarHashPath, true, Log, ct))
                return false;

            // Done.
            Log.Info($"Template was reset");
            TemplateProgress.Complete();
            data.TemplateChanged = true;
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.TemplateDir);
            return false;
        }
    }

    private async Task<bool> TryValidateCacheAsync(InitializationData data, CancellationToken ct)
    {
        try
        {
            CacheProgress.Start();

            // Clear Cache:
            CacheProgress.Start();

            // Try to reuse existing modified:
            if (IOUtils.FileExists(Paths.TemplateJarPath) && IOUtils.FileExists(Paths.TemplateJarHashPath) &&
                IOUtils.FileExists(Paths.CacheJarPath) && IOUtils.FileExists(Paths.CacheJarHashPath))
            {
                string templateJarHash = await IOUtils.TryReadAllTextAsync(Paths.TemplateJarHashPath, Log, ct);
                CacheProgress.SetNormalized(0.10);
                string cacheJarHash = await IOUtils.TryReadAllTextAsync(Paths.CacheJarHashPath, Log, ct);
                CacheProgress.SetNormalized(0.20);

                // Reuse:
                if (!data.BackupChanged && !data.TemplateChanged && templateJarHash == cacheJarHash)
                {
                    Log.Info($"Cache was reused");
                    CacheProgress.Complete();
                    data.CacheChanged = false;
                    return true;
                }
            }

            // Reset Cache:
            Log.Debug("Resetting cache...");

            // Cleanup:
            if (!await IOUtils.TryCreateDirAsync(Paths.CacheDir, Log, ct))
                return false;
            if (!await IOUtils.TryDeleteDirContentAsync(Paths.CacheDir, Log, ct))
                return false;

            // Done.
            Log.Info($"Cache was reset");
            CacheProgress.Complete();
            data.CacheChanged = true;
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.CacheDir);
            return false;
        }
    }

    private async Task<bool> TryBuildTemplateJarAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            if (!await IOUtils.TryCreateDirAsync(Paths.TemplateStageDir, Log, ct))
                return false;

            using ZipFile zin = new(Paths.BackupJarPath); // Read-Only
            using FileStream fsout = File.Create(Paths.TemplateJarPath); // Write
            using ZipOutputStream zout = new(fsout);
            zout.SetLevel(0);

            ct.ThrowIfCancellationRequested();

            // Build Template:
            foreach (ZipEntry ein in zin)
            {
                ct.ThrowIfCancellationRequested();

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
            zout.Finish();

            // Done.
            TemplateProgress?.Complete();
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.TemplateDir);
            return false;
        }
    }

    private async Task<EGamePlatform?> TryReadGamePlatformAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            Log.Debug($@"Reading game platform...", Paths.BackupDir);


            if (!IOUtils.FileExists(Paths.BackupJarPath))
            {
                Log.Error($@"Could not find ""{Paths.BackupJarPath}""", Paths.BackupDir);
                return null;
            }

            using ZipFile zin = new(Paths.BackupJarPath);
            string entryName = "META-INF/MANIFEST.MF";
            ZipEntry entry = zin.GetEntry(entryName);
            if (entry == null)
            {
                Log.Error($@"Could not find ""{entryName}"" inside: ""{Paths.BackupJarPath}""", Paths.BackupDir);
                return null;
            }

            ct.ThrowIfCancellationRequested();

            await using Stream stream = zin.GetInputStream(entry);
            using StreamReader reader = new(stream);
            string manifest = await reader.ReadToEndAsync(ct);

            bool isGOG = manifest.Contains("SpacehavenGOG", StringComparison.OrdinalIgnoreCase);
            bool isSteam = manifest.Contains("SpacehavenSteam", StringComparison.OrdinalIgnoreCase);

            if (isGOG && !isSteam)
            {
                Log.Debug($@"Game platform 'GOG' was detected...", Paths.BackupDir);
                return EGamePlatform.GOG;
            }
            if (!isGOG && isSteam)
            {
                Log.Debug($@"Game platform 'Steam' was detected...", Paths.BackupDir);
                return EGamePlatform.Steam;
            }
            return null;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BackupDir);
            return null;
        }
    }

    private async Task<VersionInfo> TryReadVersionAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            Log.Debug($@"Reading {SpaceHavenConstants.SpaceHavenName} version...", Paths.BackupDir);

            if (!IOUtils.FileExists(Paths.BackupJarPath))
            {
                Log.Error($@"Could not find ""{Paths.BackupJarPath}""", Paths.BackupDir);
                return null;
            }

            using ZipFile zin = new(Paths.BackupJarPath);
            ZipEntry entry = zin.GetEntry(SpaceHavenConstants.VERSION_TXT);
            if (entry == null)
            {
                Log.Error($@"Could not find {SpaceHavenConstants.VERSION_TXT} inside: ""{Paths.BackupJarPath}""", Paths.BackupDir);
                return null;
            }

            ct.ThrowIfCancellationRequested();

            await using Stream stream = zin.GetInputStream(entry);
            using StreamReader reader = new(stream);
            string versionStr = (await reader.ReadToEndAsync(ct)).Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).JoinToString("");
            VersionInfo version = new(versionStr);

            Log.Success($"Detected {SpaceHavenConstants.SpaceHavenName} version {version}");
            return version;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error($"Unable to read {SpaceHavenConstants.SpaceHavenName} version: {ex}", Paths.BackupDir);
            return null;
        }
    }


}

