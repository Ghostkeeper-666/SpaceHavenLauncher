using ICSharpCode.SharpZipLib.Zip;
using SH.Content;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Modding;
using SH.Modding.Models;
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

    private void SelectFiles()
    {
        FilePaths.Clear();

        // Main settings:
        Add(Paths.AppLogPath);
        Add(Paths.PathSettingsPath);
        Add(Paths.ApplicationSettingsPath);
        Add(Paths.ModListPath);
        Add(Paths.SystemInformationFilePath);

        // Mod values:
        if (IOUtils.DirExists(Paths.ModValuesDir))
            AddMany(Paths.ModValuesDir.GetFiles(ESearchOption.TopDir));

        // Some template files:
        Add(Paths.TemplateStageVersionPath);

        // Some cache files:
        Add(Paths.CacheModsJsonPath);
        Add(Paths.LauncherAgentLogPath);

        // Some build files:
        AddMany(Paths.BuildDir.GetFiles(ESearchOption.TopDir));
        AddMany(Paths.BuildAudioDir.GetFiles(ESearchOption.All));
        AddMany(Paths.BuildLogsDir.GetFiles(ESearchOption.All));
        AddMany(Paths.BuildTextsDir.GetFiles(ESearchOption.All));

        AddMany(Paths.BuildTexturesDir.GetFiles(ESearchOption.TopDir).Where(path => !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));

        string ignoreMergeLibrary = Paths.BuildMergeDir.CombineAsOSPath(ModdingConstants.LIBRARY);
        AddMany(Paths.BuildMergeDir.GetFiles(ESearchOption.All).Where(path => !path.StartsWith(ignoreMergeLibrary, StringComparison.OrdinalIgnoreCase)));

        string ignorePatchPatch = Paths.BuildPatchDir.CombineAsOSPath(SpaceHavenConstants.LIBRARY);
        AddMany(Paths.BuildPatchDir.GetFiles(ESearchOption.All).Where(path => !path.StartsWith(ignorePatchPatch, StringComparison.OrdinalIgnoreCase)));

        // Some build stage files:
        Add(Paths.BuildStageHavenXmlPath);
        Add(Paths.BuildStageTextsXmlPath);
        Add(Paths.BuildStageAudioXmlPath);
        Add(Paths.BuildStageTexturesXmlPath);
        Add(Paths.BuildStageAnimationsXmlPath);
        Add(Paths.BuildStageSpaceHavenSettingsXmlPath);
        Add(Paths.BuildStageExtraCreditsTxtPath);
        Add(Paths.BuildStageStageVersionPath);
    }


    public DebugService(PathData paths, ILogger log)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = new LoggerCollection(log);
    }

    public async Task<bool> TryGenerateDebugFileAsync(CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => TryGenerateDebugFileInternalAsync(ct, progress));
    private async Task<bool> TryGenerateDebugFileInternalAsync(CancellationToken ct, IProgressInfo progress)
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
            string debugZipDir = Paths.DebugFilePath.GetParentDirAsOSPath();
            List<string> debugZipPaths = debugZipDir.GetFiles(ESearchOption.TopDir, equalsAny: [PathData.DebugFilename]);
            foreach (string debugZipPath in debugZipPaths)
            {
                if (!await IOUtils.TryDeleteFileAsync(Paths.DebugFilePath, Log, ct))
                {
                    Log.Error($@"Unable to delete file ""{Paths.DebugFilePath}"", please check whether a program is holding this file");
                    return false;
                }
            }
            ct.ThrowIfCancellationRequested();

            // For progress:
            double sumSize = 0;
            double totalSize = 0;
            foreach (string absolutePath in FilePaths)
                if (absolutePath.TryGetFileInfo(out long fileSize, out _))
                    totalSize += fileSize;

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
                    if (!absolutePath.TryGetFileInfo(out long fileSize, out DateTime fileTime))
                        continue;

                    // Generate new zip entry:
                    sumSize += fileSize;
                    ZipEntry eout = new(relativePath)
                    {
                        Size = fileSize,
                        DateTime = fileTime,
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

    private async Task<bool> TryGenerateOSInfoAsync(CancellationToken ct)
    {
        try
        {
            Log.Info($@"Generating ""{Paths.SystemInformationFilePath.GetFileName()}""...", Paths.BuildDir);

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
}
