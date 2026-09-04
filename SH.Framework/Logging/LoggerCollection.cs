using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SH.Framework.Logging;

public sealed class LoggerCollection : ILogger
{
    public event EventHandler<LogMessage> OnMessage;

    public string Name { get; set; } = nameof(LoggerCollection);
    public IReadOnlyList<ILogger> Children => ChildrenList.ToArray();
    private List<ILogger> ChildrenList = [];

    public ELogLevel LogLevel { get; private set; } = ELogLevel.Debug;
    public void SetLogLevel(ELogLevel logLevel) =>
        Info($"{nameof(LoggerCollection)} level = '{LogLevel = logLevel}'");
    public IReadOnlyList<(string, string)> Replacements { get; set; }

    public string Prefix { get; set; }
    public string Suffix { get; set; }


    public LoggerCollection() { }

    public LoggerCollection(params ILogger[] children)
    {
        if (children == null)
            return;
        foreach (ILogger log in children)
            AddLogger(log);
    }

    public void AddLogger(ILogger log)
    {
        if (log == null || log is VoidLogger)
            return;
        ChildrenList.Add(log);
    }

    public void RemoveLogger(ILogger log)
    {
        if (log == null)
            return;
        if (ChildrenList.FirstOrDefault(l => l == log) == null)
            return;
        ChildrenList.Remove(log);
    }

    public void RemoveAll() =>
        ChildrenList.Clear();

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
        if(replacements?.Count > 0)
            foreach((string value, string token) in Replacements)
                if(m.RawText.Contains(value))
                    m.RawText = m.RawText.Replace(value, token, StringComparison.OrdinalIgnoreCase);
        for (int i = 0; i < ChildrenList.Count; ++i)
            try { ChildrenList[i].Add(m); } catch { }
        try { OnMessage?.Invoke(this, m); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

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


    #region IAsyncDisposable
    public volatile bool IsDisposed;
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        ChildrenList.Clear();
        ChildrenList = null;
    }
    #endregion
}
