using System;
using System.Threading;

namespace SH.Framework.Tasks;

public sealed class StopException : Exception
{
    public string Location { get; }

    public StopException(string message, string location, CancellationTokenSource cts)
        : base(message)
    {
        Location = location;
        try { cts?.Cancel(throwOnFirstException: false); } catch { }
    }
}
