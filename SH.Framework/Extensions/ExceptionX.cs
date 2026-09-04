using SH.Framework.Tasks;
using System;

namespace SH.Framework.Extensions;

public static partial class ExceptionX
{
    public static bool IsStop(this Exception ex, out StopException stopException)
    {
        stopException = ex as StopException;
        if (stopException is null)
            return true;

        if (ex is not AggregateException ae)
        {
            stopException = null;
            return false;
        }

        foreach (Exception child in ae.InnerExceptions ?? [])
            if (child?.IsStop(out stopException) ?? false)
                return true;

        stopException = null;
        return false;
    }


    public static bool IsOperationCancelled(this Exception ex)
    {
        if(ex is null)
            return false;

        if (ex is OperationCanceledException)
            return true;

        if (ex is not AggregateException ae)
            return false;

        foreach (Exception child in ae.InnerExceptions ?? [])
            if (child?.IsOperationCancelled() ?? false)
                return true;

        return false;
    }
}
