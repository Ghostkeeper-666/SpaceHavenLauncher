using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SH.Framework.Logging;

public sealed class LoggerCollection : ILogger
{
    public event EventHandler<LogMessage> OnMessage;


    private readonly List<ILogger> Children = [];

    public ELogLevel LogLevel { get; private set; } = ELogLevel.Debug;
    public void SetLogLevel(ELogLevel logLevel) => Warn($"Log level has changed to '{LogLevel = logLevel}'");

    public string Prefix { get; set; }
    public string Suffix { get; set; }


    public LoggerCollection() { }

    public LoggerCollection(params ILogger[] loggers)
    {
        if (loggers == null)
            return;
        foreach (ILogger logger in loggers)
            AddLogger(logger);
    }

    public void AddLogger(ILogger logger)
    {
        if (logger == null || logger is VoidLogger)
            return;
        Children.Add(logger);
    }

    public void RemoveLogger(ILogger logger)
    {
        if (logger == null)
            return;
        if (Children.FirstOrDefault(l => l == logger) == null)
            return;
        Children.Remove(logger);
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
        for (int i = 0; i < Children.Count; ++i)
            try { Children[i].Add(m); } catch { }
        try { OnMessage?.Invoke(this, m); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    public void Debug(object o = null)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Debug)
            return;
        Add(new(ELogLevel.Debug, o));
    }

    public void Info(object o = null)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Info)
            return;
        Add(new(ELogLevel.Info, o));
    }

    public void Success(object o = null)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Success)
            return;
        Add(new(ELogLevel.Success, o));
    }

    public void Warn(object o = null)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Warn)
            return;
        Add(new(ELogLevel.Warn, o));
    }

    public void Error(object o = null)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Error)
            return;
        Add(new(ELogLevel.Error, o));
    }


    public void Debug(object o, string link)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Debug)
            return;
        Add(new(ELogLevel.Debug, o, link));
    }

    public void Info(object o, string link)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Info)
            return;
        Add(new(ELogLevel.Info, o, link));
    }

    public void Success(object o, string link)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Success)
            return;
        Add(new(ELogLevel.Success, o, link));
    }

    public void Warn(object o, string link)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Warn)
            return;
        Add(new(ELogLevel.Warn, o, link));
    }

    public void Error(object o, string link)
    {
        if (IsDisposed)
            return;
        if (LogLevel > ELogLevel.Error)
            return;
        Add(new(ELogLevel.Error, o, link));
    }


    #region IAsyncDisposable
    public volatile bool IsDisposed;
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        foreach (ILogger logger in Children)
            try { await logger.DisposeAsync(); } catch { }
    }
    #endregion
}
