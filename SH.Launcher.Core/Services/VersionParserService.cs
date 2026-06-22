using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Core.Services;

public sealed class VersionParserService
{
    public async Task<VersionInfo> TryReadVersion(string path, ILogger logger, CancellationToken ct)
    {
        try
        {
            string content = await IOUtils.TryReadAllTextAsync(path, logger, ct);
            if(content == null)
                return null;
            string versionStr = content.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).JoinToString(".") ?? string.Empty;
            return new(versionStr);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(path); } catch { }
            logger?.Error(ex, parent);
            return null;
        }
    }
}
