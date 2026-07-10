using SH.Framework.Logging;
using SH.Modding.Build;
using System;
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
            // Build!
            ModBuilder builder = new(settings, Log);

            // A cancellation token is already passed using build settings:
            if (!await Task.Run(() => builder.TryBuildAsync()))
                return false;

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
