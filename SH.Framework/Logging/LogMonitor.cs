using SH.Framework.Extensions;
using SH.Framework.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Framework.Logging;

public sealed class LogMonitor : IDisposable
{
    public event EventHandler<string> OnLog;

    public string Name { get; set; } = string.Empty;
    public string LogFilePath { get; }
    public FileShare FileShare { get; }

    private readonly CancellationTokenSource CTS = new();

    public LogMonitor(string logFilePath, FileShare fileShare = FileShare.ReadWrite)
    {
        LogFilePath = !logFilePath.IsNullOrWhiteSpace() ? logFilePath.AsOSPath() : throw new ArgumentNullException(nameof(logFilePath));
        FileShare = fileShare;
    }

    public async Task RunAsync()
    {
        long lastLength = 0;
        while (!CTS.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(100, CTS.Token);

                if (!IOUtils.FileExists(LogFilePath))
                    continue;

                FileInfo fi = new(LogFilePath);
                long length = fi.Length;
                if (length == lastLength)
                    continue;
                if (length < lastLength)
                    lastLength = 0;

                string newContent;
                using (FileStream stream = new(LogFilePath, FileMode.Open, FileAccess.Read, FileShare, 64 * 1024, useAsync: true))
                {
                    stream.Seek(lastLength, SeekOrigin.Begin);
                    using StreamReader reader = new(stream);
                    newContent = await reader.ReadToEndAsync(CTS.Token);
                    lastLength = stream.Length;
                }

                if (newContent.IsNullOrEmpty())
                    continue;

                try { OnLog?.Invoke(this, newContent); }
                catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
            }

            catch (OperationCanceledException)
            { return; }

            catch (Exception ex)
            { Debug.WriteLine(ex.ToString()); }
        }
    }

    public void Stop()
    {
        try { CTS.Cancel(); }
        catch { } // when disposed
    }

    #region IDisposable
    public volatile bool IsDisposed;
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        CTS.Cancel();
        OnLog = null;
        CTS.Dispose();
    }
    #endregion
}
