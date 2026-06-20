using SH.Framework.Extensions;
using SH.Framework.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SH.Framework.Logging;

public sealed class FileLogger : ILogger
{
    public FileLogger(string path)
    {
        LogPath = !string.IsNullOrWhiteSpace(path) ? path : throw new ArgumentNullException(nameof(path));

        string dir = Path.GetDirectoryName(LogPath);
        if (!dir.IsNullOrWhiteSpace() && !IOUtils.TryCreateDirectory(dir, out string error))
            throw new Exception(error);

        if(!IOUtils.TryWriteAllText(LogPath, string.Empty, out string writeError))
            throw new Exception(writeError);

        Messages = Channel.CreateUnbounded<LogMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        LogTask = Task.Run(LoopAsync);
    }


    public event EventHandler<LogMessage> OnMessage;
    public ELogLevel LogLevel { get; set; } = ELogLevel.Debug;
    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public string LogPath { get; }

    private readonly Task LogTask;
    private readonly Channel<LogMessage> Messages;
    private readonly CancellationTokenSource CTS = new();


    #region ILogger

    public void Debug(object o = null)
    {
        if (LogLevel > ELogLevel.Debug) return;
        Add(new(ELogLevel.Debug, o));
    }

    public void Info(object o = null)
    {
        if (LogLevel > ELogLevel.Info) return;
        Add(new(ELogLevel.Info, o));
    }

    public void Success(object o = null)
    {
        if (LogLevel > ELogLevel.Success) return;
        Add(new(ELogLevel.Success, o));
    }

    public void Warn(object o = null)
    {
        if (LogLevel > ELogLevel.Warn) return;
        Add(new(ELogLevel.Warn, o));
    }

    public void Error(object o = null)
    {
        if (LogLevel > ELogLevel.Error) return;
        Add(new(ELogLevel.Error, o));
    }


    public void Debug(object o, string link)
    {
        if (LogLevel > ELogLevel.Debug) return;
        Add(new(ELogLevel.Debug, o, link));
    }

    public void Info(object o, string link)
    {
        if (LogLevel > ELogLevel.Info) return;
        Add(new(ELogLevel.Info, o, link));
    }

    public void Success(object o, string link)
    {
        if (LogLevel > ELogLevel.Success) return;
        Add(new(ELogLevel.Success, o, link));
    }

    public void Warn(object o, string link)
    {
        if (LogLevel > ELogLevel.Warn) return;
        Add(new(ELogLevel.Warn, o, link));
    }

    public void Error(object o, string link)
    {
        if (LogLevel > ELogLevel.Error) return;
        Add(new(ELogLevel.Error, o, link));
    }


    public void Add(LogMessage m)
    {
        if (IsDisposed)
            return;
        if (m == null || m.Level < LogLevel)
            return;
        if (Prefix != null)
            m.Prefix = Prefix;
        if (Suffix != null)
            m.Suffix = Suffix;
        if (!Messages.Writer.TryWrite(m))
            return;
        try { OnMessage?.Invoke(this, m); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    #endregion

    private async Task LoopAsync()
    {
        try
        {
            List<LogMessage> batch = new(1024);
            await foreach (LogMessage message in Messages.Reader.ReadAllAsync(CTS.Token))
            {
                batch.Add(message);
                while (Messages.Reader.TryRead(out LogMessage extra))
                    batch.Add(extra);
                await WriteAsync(batch);
                batch.Clear();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    private async Task WriteAsync(List<LogMessage> messages)
    {
        if (messages == null || messages.Count <= 0)
            return;
        try
        {
            StringBuilder sb = new(messages.Count * 256);
            foreach (LogMessage m in messages)
                sb.AppendLine(m.ToString());
            await File.AppendAllTextAsync(LogPath, sb.ToString(), Encoding.UTF8);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    #region IAsyncDisposable
    public volatile bool IsDisposed;
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        try
        {
            Messages.Writer.TryComplete();
            CTS.Cancel();
            await LogTask.ConfigureAwait(false);
        }
        finally
        {
            CTS.Dispose();
        }
    }
    #endregion
}