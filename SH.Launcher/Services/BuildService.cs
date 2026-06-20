using SH.Content.Modding;
using SH.Content.Modding.Build;
using SH.Framework.Logging;
using SH.Launcher.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Services;

public sealed class BuildService
{
    private readonly LoggerCollection Log;
    private readonly PathData Paths;

    public BuildService(PathData paths, ILogger logger)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Log = new LoggerCollection(logger);
    }

    public async Task<bool> TryBuildAsync(BuildPathData paths, BuildSettings settings)
    {
        try
        {
            Log.Info($"Starting {this}...", Paths.BuildDir);

            // Build!
            Builder builder = new(paths, settings, Log);
            if (!await builder.TryBuildAsync())
                return false;

            // Done.
            Log.Success($"{this} has completed", Paths.BuildDir);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, Paths.BuildDir);
            return false;
        }
    }

    public override string ToString() => "Build";
}
