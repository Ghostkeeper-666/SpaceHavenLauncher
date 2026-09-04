using SH.Content.Enums;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Framework.Progress;
using SH.Modding.Annotation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class XmlAnnotationService
{
    private readonly ILogger Log;

    public XmlAnnotationService(ILogger log)
    {
        Log = log ?? new VoidLogger();
    }

    public async Task<bool> TryRunAsync(string baseDir, ELanguage language, CancellationToken ct, IProgressInfo progress) =>
        await Task.Run(() => TryRunInternalAsync(baseDir, language, ct, progress));
    private async Task<bool> TryRunInternalAsync(string baseDir, ELanguage language, CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            progress?.Start();
            XmlAnnotator xmlAnnotator = new(Log);
            if (!await xmlAnnotator.TryRunAsync(baseDir, language, ct, progress))
                return false;
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




