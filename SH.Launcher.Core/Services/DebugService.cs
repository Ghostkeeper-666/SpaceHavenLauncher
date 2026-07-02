using ICSharpCode.SharpZipLib.Zip;
using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Launcher.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class DebugService
{
    private readonly LoggerCollection Log;
    private readonly PathData Paths;

    private readonly List<string> FilePaths = [];

    private void Add(string filePath)
    {
        if (IOUtils.FileExists(filePath))
            FilePaths.Add(filePath.AsOSPath());
    }

    private void AddMany(IEnumerable<string> filePaths)
    {
        foreach (string filePath in filePaths ?? [])
            if (IOUtils.FileExists(filePath))
                FilePaths.Add(filePath.AsOSPath());
    }

    private void SelectFiles()
    {
        FilePaths.Clear();

        // Main settings:
        Add(Paths.PathSettingsPath);
        Add(Paths.ApplicationSettingsPath);
        Add(Paths.ModListPath);
        Add(Paths.SystemInformationFilePath);

        // Mod values:
        if (IOUtils.DirectoryExists(Paths.ModValuesDir))
            AddMany(Directory.GetFiles(Paths.ModValuesDir, "*.xml", SearchOption.TopDirectoryOnly));

        // Some backup files:
        Add(Paths.BackupConfigJsonPath);

        // Some template files:
        Add(Paths.TemplateConfigJsonPath);
        Add(Paths.TemplateStageVersionPath);

        // Some cache files:
        Add(Paths.CacheConfigJsonPath);
        Add(Paths.CacheModsJsonPath);

        // Some build files:
        if (IOUtils.DirectoryExists(Paths.BuildDir))
            AddMany(Directory.GetFiles(Paths.BuildDir, "*.*", SearchOption.TopDirectoryOnly));
        if (IOUtils.DirectoryExists(Paths.BuildAudioDir))
            AddMany(Directory.GetFiles(Paths.BuildAudioDir, "*.*", SearchOption.AllDirectories));
        if (IOUtils.DirectoryExists(Paths.BuildLogsDir))
            AddMany(Directory.GetFiles(Paths.BuildLogsDir, "*.*", SearchOption.AllDirectories));
        if (IOUtils.DirectoryExists(Paths.BuildTextsDir))
            AddMany(Directory.GetFiles(Paths.BuildTextsDir, "*.*", SearchOption.AllDirectories));
        if (IOUtils.DirectoryExists(Paths.BuildTexturesDir))
            AddMany(Directory.GetFiles(Paths.BuildTexturesDir, "*.*", SearchOption.AllDirectories));
        string ignoreMergeLibrary = Path.Combine(Paths.BuildMergeDir, SpaceHavenConstants.LIBRARY);
        if (IOUtils.DirectoryExists(Paths.BuildMergeDir))
            AddMany(Directory.GetFiles(Paths.BuildMergeDir, "*.*", SearchOption.AllDirectories).Where(path => !path.StartsWith(ignoreMergeLibrary, StringComparison.OrdinalIgnoreCase)));
        string ignorePatchLibrary = Path.Combine(Paths.BuildPatchDir, SpaceHavenConstants.LIBRARY);
        if (IOUtils.DirectoryExists(Paths.BuildPatchDir))
            AddMany(Directory.GetFiles(Paths.BuildPatchDir, "*.*", SearchOption.AllDirectories).Where(path => !path.StartsWith(ignorePatchLibrary, StringComparison.OrdinalIgnoreCase)));

        // Some build stage files:
        Add(Paths.BuildStageHavenXmlPath);
        Add(Paths.BuildStageTextsXmlPath);
        Add(Paths.BuildStageAudioXmlPath);
        Add(Paths.BuildStageTexturesXmlPath);
        Add(Paths.BuildStageAnimationsXmlPath);
        Add(Paths.BuildStageSpaceHavenSettingsXmlPath);
        Add(Paths.BuildStageExtraCreditsVersionPath);
        Add(Paths.BuildStageStageVersionPath);
    }


    public DebugService(PathData paths, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = new LoggerCollection(logger);
    }

    private async Task<bool> TryGenerateOSInfoAsync(CancellationToken ct)
    {
        try
        {
            Log.Info($@"Generating ""{Path.GetFileName(Paths.SystemInformationFilePath)}""...", Paths.BuildDir);

            // Try to delete existing file:
            if (!await IOUtils.TryDeleteFileAsync(Paths.SystemInformationFilePath, Log, ct))
            {
                Log.Error($@"Unable to delete file ""{Paths.SystemInformationFilePath}"", please check whether a program is holding this file");
                return false;
            }
            ct.ThrowIfCancellationRequested();

            // Generate content:
            StringBuilder sb = new();
            sb.AppendLine($"OS:            {RuntimeInformation.OSDescription}");
            sb.AppendLine($"OS Arch:       {RuntimeInformation.OSArchitecture}");
            sb.AppendLine($"Process Arch:  {RuntimeInformation.ProcessArchitecture}");
            sb.AppendLine($"64-bit OS:     {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"64-bit Proc:   {Environment.Is64BitProcess}");
            sb.AppendLine($"Processors:    {Environment.ProcessorCount}");
            sb.AppendLine($"Available RAM: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes}");
            sb.AppendLine($"Is Windows:    {OS.IsWin}");
            sb.AppendLine($"Is Linux:      {OS.IsLnx}");
            sb.AppendLine($"Is macOS:      {OS.IsMac}");

            // Write file:
            return await IOUtils.TryWriteAllTextAsync(Paths.SystemInformationFilePath, sb.ToString(), Log, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable to generate a debug file: {ex}", Paths.WorkDir);
            return false;
        }
    }

    public async Task<bool> TryGenerateDebugFileAsync(CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress?.Start();
            Log.Success("Generating a debug file...", Paths.BuildDir);

            // Collect system information:
            if (!await TryGenerateOSInfoAsync(ct))
                return false;

            // Get files to pack:
            SelectFiles();
            ct.ThrowIfCancellationRequested();

            // Try to delete existing file:
            if (!await IOUtils.TryDeleteFileAsync(Paths.DebugFilePath, Log, ct))
            {
                Log.Error($@"Unable to delete file ""{Paths.DebugFilePath}"", please check whether a program is holding this file");
                return false;
            }
            ct.ThrowIfCancellationRequested();

            // For progress:
            double sumSize = 0;
            double totalSize = 0;
            foreach (string absolutePath in FilePaths)
            {
                try
                {
                    FileInfo fi = new(absolutePath);
                    totalSize += fi.Length;
                }
                catch { }
            }

            // Pack files:
            using FileStream fsout = File.Create(Paths.DebugFilePath);
            using ZipOutputStream zout = new(fsout);
            zout.SetLevel(9);
            string baseDir = $"{Paths.WorkDir.AsStdPath()}/";

            bool errors = false;
            foreach (string absolutePath in FilePaths)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // Get relative path:
                    if (!absolutePath.AsStdPath().StartsWith(baseDir))
                    {
                        Log.Error($@"Unable to collect file ""{absolutePath}"" because it escapes the base directory ""{baseDir}""");
                        continue;
                    }
                    string relativePath = absolutePath.Substring(baseDir.Length);
                    if (relativePath.IsNullOrWhiteSpace())
                        continue; // should never happen

                    // Read basic file information:
                    FileInfo fi = new(absolutePath);
                    if (!fi.Exists)
                        continue; // should not happen

                    // Generate new zip entry:
                    sumSize += fi.Length;
                    ZipEntry eout = new(relativePath)
                    {
                        Size = fi.Length,
                        DateTime = fi.LastWriteTime,
                        CompressionMethod = CompressionMethod.Deflated,
                    };
                    zout.PutNextEntry(eout);
                    using FileStream input = File.OpenRead(absolutePath);
                    input.CopyTo(zout);
                    zout.CloseEntry();
                }
                catch (Exception ex)
                {
                    errors = true;
                    Log.Error($@"Unable to collect file ""{absolutePath}"": {ex}", Paths.WorkDir);
                }
                finally
                {
                    progress?.SetNormalized(sumSize / totalSize);
                }
            }
            zout.Finish();
            progress?.Complete();

            // Done.
            if (!errors) Log.Success($@"A debug file was successfully generated", Paths.BuildDir);
            else Log.Error($@"A debug file was generated with errors", Paths.BuildDir);
            return !errors;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error($@"Unable to generate a debug file: {ex}", Paths.WorkDir);
            return false;
        }
    }
}
