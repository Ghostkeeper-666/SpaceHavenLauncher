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

public static partial class ExceptionX
{
    public static bool IsStop(this Exception ex, out StopException stopException)
    {
        stopException = ex as StopException;
        if (stopException != null)
            return true;

        if (ex is not AggregateException ae)
        {
            stopException = null;
            return false;
        }

        foreach (Exception child in ae.InnerExceptions ?? [])
            if (child.IsStop(out stopException))
                return true;

        stopException = null;
        return false;
    }


    public static bool IsOperationCancelled(this Exception ex)
    {
        if (ex is OperationCanceledException)
            return true;

        if (ex is not AggregateException ae)
            return false;

        foreach (Exception child in ae.InnerExceptions ?? [])
            if (child.IsOperationCancelled())
                return true;

        return false;
    }
}
