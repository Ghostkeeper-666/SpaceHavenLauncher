using System.Collections.Concurrent;

public sealed class ArrayPool<T>
{
    public int ArraySize { get; }

    private readonly ConcurrentBag<T[]> Pool = new();

    public ArrayPool(int arraySize, int initialCapacity)
    {
        ArraySize = arraySize > 0 ? arraySize : 4096;
        for (int i = 0; i < initialCapacity; ++i)
            Pool.Add(new T[ArraySize]);
    }

    public T[] Get() =>
        Pool.TryTake(out T[] buffer) ? buffer : new T[ArraySize];

    public void Return(T[] buffer)
    {
        if (buffer == null)
            return;
        if (buffer.Length != ArraySize)
            return;
        Pool.Add(buffer);
    }
}
