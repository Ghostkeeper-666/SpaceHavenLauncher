using System;
using System.Threading.Tasks;

namespace SH.Framework.Logging;

public sealed class Logger : ILogger
{
    public Logger() { }
    public Logger(ELogLevel logLevel) =>
        LogLevel = logLevel;

    public ELogLevel LogLevel { get; set; } = ELogLevel.Debug;
    public string Prefix { get; set; }
    public string Suffix { get; set; }


    public event EventHandler<LogMessage> OnMessage;

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
        if (m == null || m.Level < LogLevel) return;
        if (Prefix != null) m.Prefix = Prefix;
        if (Suffix != null) m.Suffix = Suffix;
        try { OnMessage?.Invoke(this, m); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    #region IAsyncDisposable
    public async ValueTask DisposeAsync() { }
    #endregion
}
