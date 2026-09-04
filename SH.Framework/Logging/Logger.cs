using SH.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SH.Framework.Logging;

public sealed class Logger : ILogger
{
    public Logger() { }

    public string Name { get; set; } = nameof(Logger);
    public IReadOnlyList<ILogger> Children => [];

    public ELogLevel LogLevel { get; private set; } = ELogLevel.Debug;
    public void SetLogLevel(ELogLevel logLevel) =>
        Info($"{nameof(Logger)} level = '{LogLevel = logLevel}'");
    public IReadOnlyList<(string, string)> Replacements { get; set; }

    public string Prefix { get; set; }
    public string Suffix { get; set; }


    public event EventHandler<LogMessage> OnMessage;

    public void Debug(object o = null)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Debug)
            return;
        Add(new(ELogLevel.Debug, o));
    }

    public void Info(object o = null)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Info)
            return;
        Add(new(ELogLevel.Info, o));
    }

    public void Success(object o = null)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Success)
            return;
        Add(new(ELogLevel.Success, o));
    }

    public void Warn(object o = null)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Warn)
            return;
        Add(new(ELogLevel.Warn, o));
    }

    public void Error(object o = null)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Error)
            return;
        Add(new(ELogLevel.Error, o));
    }


    public void Debug(object o, string link)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Debug)
            return;
        Add(new(ELogLevel.Debug, o, link));
    }

    public void Info(object o, string link)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Info)
            return;
        Add(new(ELogLevel.Info, o, link));
    }

    public void Success(object o, string link)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Success)
            return;
        Add(new(ELogLevel.Success, o, link));
    }

    public void Warn(object o, string link)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Warn)
            return;
        Add(new(ELogLevel.Warn, o, link));
    }

    public void Error(object o, string link)
    {
        if (IsDisposed || o == null)
            return;
        if (LogLevel > ELogLevel.Error)
            return;
        Add(new(ELogLevel.Error, o, link));
    }


    public void Add(LogMessage m)
    {
        if (IsDisposed || m is null)
            return;
        if (m.Level < LogLevel || m.RawText is null)
            return;
        if (Prefix != null)
            m.Prefix = Prefix;
        if (Suffix != null)
            m.Suffix = Suffix;
        try
        {
            IReadOnlyList<(string, string)> rep = Replacements;
            if (rep != null && rep.Count > 0)
                foreach ((string value, string token) in rep ?? [])
                    if (!value.IsNullOrEmpty() && (m?.RawText?.Contains(value) ?? false))
                        m.RawText = m.RawText?.Replace(value ?? string.Empty, token ?? string.Empty, StringComparison.OrdinalIgnoreCase) ?? string.Empty;

            OnMessage?.Invoke(this, m);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    #region IAsyncDisposable
    public volatile bool IsDisposed;
    public async ValueTask DisposeAsync() { IsDisposed = true; }
    #endregion
}
