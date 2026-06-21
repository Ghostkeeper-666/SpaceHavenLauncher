using System;

namespace SH.Framework.Logging;

public interface ILogger : IAsyncDisposable
{
    public event EventHandler<LogMessage> OnMessage;
    public ELogLevel LogLevel { get; }
    public void SetLogLevel(ELogLevel logLevel);
    
    public string Prefix { get; set; }
    public string Suffix { get; set; }
    
    public void Debug(object o = null);
    public void Error(object o = null);
    public void Info(object o = null);
    public void Success(object o = null);
    public void Warn(object o = null);

    public void Debug(object o, string link);
    public void Error(object o, string link);
    public void Info(object o, string link);
    public void Success(object o, string link);
    public void Warn(object o, string link);
    
    public void Add(LogMessage m);
}