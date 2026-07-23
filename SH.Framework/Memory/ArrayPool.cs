using System;
using System.Collections.Concurrent;

namespace SH.Framework.Memory;

public sealed class ArrayPool<T> : IDisposable
{
    public int ArraySize { get; }

    private ConcurrentBag<T[]> Pool = new();

    public ArrayPool(int arraySize, int initialCapacity)
    {
        ArraySize = arraySize > 0 ? arraySize : 4096;
        for (int i = 0; i < initialCapacity; ++i)
            Pool.Add(new T[ArraySize]);
    }

    public T[] Get() =>
        IsDisposed ? null :
        Pool.TryTake(out T[] buffer) ? buffer : new T[ArraySize];

    public void Return(T[] buffer)
    {
        if(IsDisposed)
            return;
        if (buffer == null)
            return;
        if (buffer.Length != ArraySize)
            return;
        Pool.Add(buffer);
    }

    #region IDisposable
    public volatile bool IsDisposed;
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        Pool?.Clear();
        Pool = null;
    }
    #endregion
}