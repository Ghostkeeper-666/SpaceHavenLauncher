using SH.Content;
using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Modding;
using SH.Modding.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class GameLaunchService
{
    private readonly PathData Paths;
    private readonly ILogger Log;

    public GameLaunchService(PathData paths, ILogger log)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = log ?? new VoidLogger();
    }

    public async Task<bool> TryLaunchModifiedGameAsync(
        EGamePlatform gamePlatform,
        string mainClass,
        string vmArgs,
        IEnumerable<string> modJars,
        string gameJar,
        CancellationToken ct) =>
        await Task.Run(() => TryLaunchWithJreInternalAsync(
            gamePlatform,
            mainClass,
            vmArgs,
            modJars,
            gameJar,
            ct));

    private async Task<bool> TryLaunchWithJreInternalAsync(
        EGamePlatform gamePlatform,
        string mainClass,
        string vmArgs,
        IEnumerable<string> modJars,
        string gameJar,
        CancellationToken ct)
    {
        try
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);

            modJars ??= [];
            bool hasJavaMods = modJars.Any();

            // JRE:
            string program = Paths.JREPath;
            if (!IOUtils.FileExists(Paths.JREPath))
            {
                Log.Error($@"The path to JRE is invalid: ""{Paths.JREPath}""");
                return false;
            }

            // Simple check for classpath in the provided VM args, just to warn:
            // If the user wants to do this, it's not our problem...
            vmArgs ??= string.Empty;
            if (vmArgs.Contains("-cp", StringComparison.OrdinalIgnoreCase) ||
                vmArgs.Contains("-classpath", StringComparison.OrdinalIgnoreCase) ||
                vmArgs.Contains("--class-path", StringComparison.OrdinalIgnoreCase))
                Log.Warn("The provided VMArgs contain class paths. This attempt will probably fail...");


            // First of all, the Launcher's args:
            List<string> args = [];
            args.Add($@"""-Daj.weaving.verbose=true""");
            args.Add($@"""-Dorg.aspectj.weaver.showWeaveInfo=true""");
            args.Add($@"""-XshowSettings:properties""");
            args.Add($@"""-D{ModdingConstants.JVM_VAR_MODS_JSON}={Paths.CacheModsJsonPath}""");

            // The first java agent must be the LauncherAgent:
            args.Add($@"""-javaagent:{IOUtils.CombineAsOSPath(Paths.CacheDir, "LauncherAgent.jar")}""");

            // Now add advanced-user-customized vmArgs or the default VMArgs:
            if(vmArgs.IsNullOrWhiteSpace())
                vmArgs = SpaceHavenConstants.GetDefaultVMArgs(OS.Type).Select(arg => $@"""{arg}""").JoinToString(" ");
            args.Add(vmArgs);

            // Now add the AOP agent after everything else and just before the class path:
            args.Add($@"""-javaagent:{IOUtils.CombineAsOSPath(Paths.CacheDir, ModdingConstants.ASPECTJWEAVER)}""");

            // Add class paths for AOP and spacehaven.jar:
            args.Add("-cp");
            args.Add($@"""{IOUtils.CombineAsOSPath(Paths.CacheDir, ModdingConstants.ASPECTJ)}{IOUtils.PathSeparator}{gameJar.AsOSPath()}""");

            // And finally set the main class:
            if (mainClass.IsNullOrWhiteSpace())
            {
                switch (gamePlatform)
                {
                    case EGamePlatform.GOG:
                        mainClass = "fi.bugbyte.spacehaven.gog.SpacehavenGOG";
                        break;
                    case EGamePlatform.Steam:
                        mainClass = "fi.bugbyte.spacehaven.steam.SpacehavenSteam";
                        break;
                    default:
                        throw new NotImplementedException($"{nameof(gamePlatform)} = {gamePlatform}");
                }
            }
            args.Add(mainClass);

            // Clear any previously existing LauncherAgent log:
            await IOUtils.TryDeleteFileAsync(Paths.LauncherAgentLogPath, Log, ct);

            // Start monitoring the LauncherAgent log:
            using LogMonitor monitor = new(Paths.LauncherAgentLogPath);
            monitor.OnLog += Monitor_OnLog;
            _ = monitor.RunAsync();

            // Create process:
            ProcessStartInfo info = new()
            {
                FileName = Paths.JREPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Maximized, // <<< not respected on all platforms!
                WorkingDirectory = Paths.SpaceHavenJarDir.AsOSPath(),
                Arguments = args.JoinToString(' '),
            };

            // Log:
            Log.Debug($@"Cache Directory: ""{Paths.CacheDir}""");
            Log.Debug($@"{SpaceHavenConstants.SPACEHAVEN_JAR}: ""{Paths.CacheJarPath}""");
            Log.Debug($@"{ModdingConstants.MODS_JSON}: ""{Paths.CacheModsJsonPath}""");
            Log.Debug($@"JVM Variable for path to {ModdingConstants.MODS_JSON} file: {ModdingConstants.JVM_VAR_MODS_JSON}");

            string text = $"Starting {SpaceHavenConstants.SpaceHavenName}";
            string dashedLine = new('=', text.Length);
            StringBuilder sb = new();
            sb.AppendLine(dashedLine);
            sb.AppendLine($"Starting {SpaceHavenConstants.SpaceHavenName}");
            sb.AppendLine(dashedLine);
            Log.Success(sb.ToString(), Paths.SpaceHavenDir);

            Log.Debug($@"Working Directory: ""{info.WorkingDirectory}""", info.WorkingDirectory);
            Log.Debug($@"""{Paths.JREPath}"" {info.Arguments}", info.WorkingDirectory);

            // Process:
            using Process process = new()
            {
                StartInfo = info,
            };

            process.OutputDataReceived += (_, e) =>
            {
                try
                {
                    string msg = e?.Data ?? string.Empty;
                    if (msg.IsNullOrEmpty())
                        return;

                    if (msg.StartsWith("ERROR:") || msg.StartsWith("[ERROR]") || msg.StartsWith("[FAILURE]"))
                        Log.Error($"[JVM]  {msg}");

                    if (msg.StartsWith("WARN:") || msg.StartsWith("WARNING:") || msg.StartsWith("[WARNING]"))
                        Log.Warn($"[JVM]  {msg}");

                    if (msg.StartsWith("DEBUG:") || msg.StartsWith("[DEBUG]"))
                        Log.Debug($"[JVM]  {msg}");

                    Log.Info($"[JVM]  {msg}");
                }
                catch { }
            };

            process.ErrorDataReceived += (_, e) =>
                Log.Debug($"[JVM]  {e?.Data}");

            if (!process.Start())
            {
                await Task.Delay(200, ct);
                return false;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(ct);
                return process.ExitCode == 0;
            }
            catch (Exception ex) when (ex.IsOperationCancelled())
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch { }
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            if (!ct.IsCancellationRequested)
                await Task.Delay(200, default);
            return false;
        }
    }

    private void Monitor_OnLog(object sender, string text)
    {
        try
        {
            if (text.IsNullOrWhiteSpace())
                return;
            LogMessage[] msgs =
                text.Replace("\r", string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(text => new LogMessage(text.Contains("ERROR", StringComparison.Ordinal) ? ELogLevel.Error : ELogLevel.Debug, text.TrimEnd()))
                .ToArray();
            foreach (LogMessage msg in msgs)
                Log.Add(msg);
        }
        catch { }
    }
}

