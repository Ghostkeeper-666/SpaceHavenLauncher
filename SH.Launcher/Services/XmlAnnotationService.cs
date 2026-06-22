using SH.Content.Enums;
using SH.Content.Modding.Annotation;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Services;

public sealed class XmlAnnotationService
{
    private readonly ILogger Log;

    public XmlAnnotationService(ILogger logger)
    {
        Log = logger ?? new VoidLogger();
    }

    public async Task<bool> TryRunAsync(string baseDir, ELanguage language, CancellationToken ct, IProgressInfo progress)
    {
        try
        {
            XmlAnnotator xmlAnnotator = new(Log);
            if (!await xmlAnnotator.TryRunAsync(baseDir, language, ct, progress))
                return false;
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




