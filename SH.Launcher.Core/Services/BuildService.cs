using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Modding.Build;
using System;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class BuildService
{
    private readonly ILogger Log;

    public BuildService(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    public async Task<bool> TryBuildAsync(BuildSettings settings)
    {
        bool success = await Task.Run(() => TryBuildInternalAsync(settings));

        // Force Garbage Collection:
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);

        return success;
    }
    public async Task<bool> TryBuildInternalAsync(BuildSettings settings)
    {
        try
        {
            // Build scoped:
            await using ModBuilder builder = new(settings, Log);
            if (!await builder.TryBuildAsync())
                return false;

            // Done.
            return true;
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }
}
