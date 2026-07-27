using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SH.Framework.Logging;

public sealed class VoidLogger : ILogger
{
    public event EventHandler<LogMessage> OnMessage;

    public string Name { get; set; } = nameof(VoidLogger);
    public IReadOnlyList<ILogger> Children => [];

    public ELogLevel LogLevel => ELogLevel.None;
    public void SetLogLevel(ELogLevel logLevel) { }
    public IReadOnlyList<(string, string)> Replacements { get; set; }

    public string Prefix { get; set; }
    public string Suffix { get; set; }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(object o = null) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(object o = null) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Info(object o = null) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Success(object o = null) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warn(object o = null) { }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(object o, string link) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(object o, string link) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Info(object o, string link) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Success(object o, string link) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warn(object o, string link) { }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(LogMessage m) { }


    public async ValueTask DisposeAsync() { }
}
