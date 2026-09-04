using SH.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SH.Framework.Logging;

public sealed class BatchLogger : ILogger
{
    public BatchLogger(TimeSpan interval)
    {
        Interval = interval;

        Messages = Channel.CreateUnbounded<LogMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        LogTask = Task.Run(LoopAsync);
    }

    public string Name { get; set; } = nameof(BatchLogger);
    public IReadOnlyList<ILogger> Children => [];

    public TimeSpan Interval { get; set; }

    public event EventHandler<IReadOnlyList<LogMessage>> OnMessages;

    public event EventHandler<LogMessage> OnMessage;

    public ELogLevel LogLevel { get; private set; } = ELogLevel.Debug;
    public void SetLogLevel(ELogLevel logLevel) =>
        Info($"{nameof(BatchLogger)} level = '{LogLevel = logLevel}'");
    public IReadOnlyList<(string, string)> Replacements { get; set; }

    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public string LogPath { get; private set; }

    private readonly Task LogTask;
    private readonly Channel<LogMessage> Messages;
    private readonly CancellationTokenSource CTS = new();


    #region ILogger

    public void Debug(object o = null)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Debug) 
            return;
        Add(new(ELogLevel.Debug, o));
    }

    public void Info(object o = null)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Info) 
            return;
        Add(new(ELogLevel.Info, o));
    }

    public void Success(object o = null)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Success) 
            return;
        Add(new(ELogLevel.Success, o));
    }

    public void Warn(object o = null)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Warn)
            return;
        Add(new(ELogLevel.Warn, o));
    }

    public void Error(object o = null)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Error)
            return;
        Add(new(ELogLevel.Error, o));
    }


    public void Debug(object o, string link)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Debug)
            return;
        Add(new(ELogLevel.Debug, o, link));
    }

    public void Info(object o, string link)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Info)
            return;
        Add(new(ELogLevel.Info, o, link));
    }

    public void Success(object o, string link)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Success)
            return;
        Add(new(ELogLevel.Success, o, link));
    }

    public void Warn(object o, string link)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Warn)
            return;
        Add(new(ELogLevel.Warn, o, link));
    }

    public void Error(object o, string link)
    {
        if (o == null)
            return;
        if (LogLevel > ELogLevel.Error)
            return;
        Add(new(ELogLevel.Error, o, link));
    }


    public void Add(LogMessage m)
    {
        if (IsDisposed)
            return;
        if (m == null || m.Level < LogLevel || m.RawText == null)
            return;
        if (Prefix != null)
            m.Prefix = Prefix;
        if (Suffix != null)
            m.Suffix = Suffix;
        IReadOnlyList<(string, string)> replacements = Replacements;
        if (replacements?.Count > 0)
            foreach ((string value, string token) in Replacements)
                if (m.RawText.Contains(value))
                    m.RawText = m.RawText.Replace(value, token, StringComparison.OrdinalIgnoreCase);
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
                // Dequeue all
                batch.Add(message);

                await Task.Delay(Interval, CTS.Token);

                while (Messages.Reader.TryRead(out LogMessage extra))
                    batch.Add(extra);

                if (batch.Count > 0)
                {
                    try
                    {
                        OnMessages?.Invoke(this, new List<LogMessage>(batch));
                        if (OnMessage != null)
                            foreach (LogMessage m in batch)
                                OnMessage?.Invoke(this, m);
                    }
                    catch (Exception ex) when (ex.IsOperationCancelled()) { throw; }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                }
                batch.Clear();
            }
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    private void PublishBatchAsync(List<LogMessage> messages)
    {
        try
        {
            OnMessages?.Invoke(this, messages);

            if (OnMessage != null)
                foreach (LogMessage m in messages)
                    OnMessage?.Invoke(this, m);
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { }
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