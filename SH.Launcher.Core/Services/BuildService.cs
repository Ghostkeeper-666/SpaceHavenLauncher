using SH.Framework.Logging;
using SH.Modding.Build;
using System;
using System.Runtime;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class BuildService
{
    private readonly LoggerCollection Log;

    public BuildService(ILogger logger) =>
        Log = new LoggerCollection(logger);

    public async Task<bool> TryBuildAsync(BuildSettings settings)
    {
        try
        {
            // Build scoped:
            {
                ModBuilder builder = new(settings, Log);
                if (!await Task.Run(() => builder.TryBuildAsync()))
                    return false;
            }

            // Force Garbage Collection:
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            // Done.
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }
}
