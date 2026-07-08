using SH.Content;
using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Core.Models;
using SH.Modding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class GameLauncherService
{
    private readonly PathData Paths;
    private readonly ILogger Log;

    public GameLauncherService(PathData paths, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = logger ?? new VoidLogger();
    }

    public async Task<bool> TryLaunchWithJreAsync(
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
            modJars ??= [];
            bool hasJavaMods = modJars.Any();

            // JRE:
            string program = Paths.JREPath;
            if (!IOUtils.FileExists(Paths.JREPath))
            {
                Log.Error($@"The path to JRE is invalid: ""{Paths.JREPath}""");
                return false;
            }

            // Process details:
            ProcessStartInfo info = new()
            {
                FileName = Paths.JREPath,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Maximized, // <<< not respected on all platforms!
            };

            // VMArgs:
            foreach (string vmArg in vmArgs?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [])
                if(!vmArg.StartsWith("-javaagent", StringComparison.OrdinalIgnoreCase))
                    info.ArgumentList.Add(vmArg);

            if (hasJavaMods)
            {
                info.ArgumentList.Add($"-javaagent:{Path.Combine(Paths.CacheDir, "LauncherAgent.jar").AsOSPath()}");
                info.ArgumentList.Add($"-javaagent:{Path.Combine(Paths.CacheDir, ModdingConstants.ASPECTJWEAVER).AsOSPath()}");
                info.ArgumentList.Add("-Daj.weaving.verbose=true");
                info.ArgumentList.Add("-Dorg.aspectj.weaver.showWeaveInfo=true");
            }

            // classPath:
            //List<string> classPaths = [];
            //if (hasJavaMods)
            //{
            // AOP libs:
            //classPaths.Add(Path.Combine(Paths.AppDir, ModdingConstants.ASPECTJWEAVER).AsStdPath());
            //classPaths.Add(Path.Combine(Paths.AppDir, ModdingConstants.ASPECTJ).AsStdPath());

            // MOD libs:
            //foreach (string modJar in modJars?.Where(path => !path.IsNullOrWhiteSpace() && path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) ?? [])
            //    classPaths.Add(modJar.AsStdPath());
            //}
            //classPaths.Add(gameJar.AsStdPath());
            //info.ArgumentList.Add("-cp");
            //info.ArgumentList.Add(classPaths.JoinToString(";"));

            info.ArgumentList.Add("-cp");


            info.ArgumentList.Add($"{Path.Combine(Paths.CacheDir, ModdingConstants.ASPECTJ).AsOSPath()};{gameJar.AsOSPath()}");

            // mainClass:
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
            info.ArgumentList.Add(mainClass);

            // Working Directory:
            info.WorkingDirectory = Paths.SpaceHavenJarDir.AsOSPath();

            // Log command line:
            Log.Debug($@"""{info.WorkingDirectory}""", info.WorkingDirectory);
            Log.Debug($@"""{info.FileName}"" {info.ArgumentList.Select(arg => $@"""{arg}""").JoinToString(" ")}", info.WorkingDirectory);

            // Start the process:
            string text = $"Starting {SpaceHavenConstants.SpaceHavenName}";
            string dashedLine = new('=', text.Length);
            StringBuilder sb = new();
            sb.AppendLine(dashedLine);
            sb.AppendLine($"Starting {SpaceHavenConstants.SpaceHavenName}");
            sb.AppendLine(dashedLine);
            Log.Success(sb.ToString(), Paths.SpaceHavenDir);

            using Process process = new()
            {
                StartInfo = info,
            };

            if (!process.Start())
                return false;

            try
            {
                await process.WaitForExitAsync(ct);
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException)
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
            return false;
        }
    }
}