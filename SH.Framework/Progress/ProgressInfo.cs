using SH.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SH.Framework.Progress;

public sealed class ProgressInfo : IProgressInfo
{
    public event EventHandler<ProgressEventArgs> ProgressChanged;

    private ulong SeqNum = 0;

    private int CachedValue = int.MinValue;

    private void OnChildProgressChanged(object sender, ProgressEventArgs e)
    {
        int value = Value;
        if (CachedValue != value || HasStarted == false)
        {
            HasStarted = true;
            CachedValue = value;
            ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
        }
    }

    public ProgressInfo(string name = null)
    {
        Name = name ?? string.Empty;
    }

    public ProgressInfo(string name, IEnumerable<IProgressInfo> weightlessChildren) : this(name)
    {
        if (weightlessChildren == null) return;
        foreach (IProgressInfo c in weightlessChildren)
            AddChild(c, 1.0);
    }

    public ProgressInfo(string name, IEnumerable<(IProgressInfo, double)> weightedChildren) : this(name)
    {
        if (weightedChildren == null) return;
        foreach ((IProgressInfo p, double weight) in weightedChildren)
            AddChild(p, weight);
    }

    public ProgressInfo(string name, IReadOnlyDictionary<IProgressInfo, double> weightedChildren) : this(name)
    {
        if (weightedChildren == null) return;
        foreach (KeyValuePair<IProgressInfo, double> kvp in weightedChildren)
            AddChild(kvp.Key, kvp.Value);
    }

    public IProgressInfo CreateChild(string childName, double weight = 1.0) =>
        AddChild(new ProgressInfo(childName), weight);

    public IProgressInfo AddChild(IProgressInfo child, double weight = 1.0)
    {
        if (child == null)
            return child;
        ProgressArgumentException.ThrowIfNotFiniteAndPositive(weight);
        lock (Lock)
        {
            if (LocalNormalizedValue != 0.0)
                child.SetNormalized(LocalNormalizedValue);
            ChildrenDict.Add(child, weight);
            TotalWeight += weight;
            child.ProgressChanged -= OnChildProgressChanged;
            child.ProgressChanged += OnChildProgressChanged;
        }
        return child;
    }

    public void Remove(IProgressInfo child)
    {
        lock (Lock)
        {
            if (!ChildrenDict.Remove(child, out double childWeight))
                return;
            TotalWeight = Math.Max(0.0, TotalWeight - childWeight);
            child.ProgressChanged -= OnChildProgressChanged;
        }
    }

    public void RemoveAll()
    {
        lock (Lock)
        {
            double normalizedValue = NormalizedValue;
            foreach (IProgressInfo child in ChildrenDict.Keys)
                child.ProgressChanged -= OnChildProgressChanged;
            ChildrenDict.Clear();
            TotalWeight = 0.0;
            LocalNormalizedValue = normalizedValue;
        }
    }

    private readonly Lock Lock = new();

    private readonly OrderedDictionary<IProgressInfo, double> ChildrenDict = [];

    public string Name { get; }
    public double Range => Max - Min;
    public int Min { get; set; } = 0;
    public int Max { get; set; } = 100;
    public double TotalWeight { get; private set; } = 0.0;
    private double LocalNormalizedValue { get; set; } = 0.0;
    public double NormalizedValue =>
        ChildrenDict.Count <= 0 ? LocalNormalizedValue : LocalNormalizedValue = ChildrenDict.Sum(kvp => kvp.Key.NormalizedValue * kvp.Value) / TotalWeight;
    public int Value =>
        (int)(Range * NormalizedValue + Min);
    public double RemainingNormalizedValue =>
        1.0 - NormalizedValue;
    public double RemainingValue =>
        Max - Value;
    public bool HasStarted { get; private set; }
    public bool HasCompleted => HasStarted && NormalizedValue >= 1.0;

    /// <summary>
    /// This method only serves to signal that a process has started, for UI display purpose.
    /// Its children should not be started, since not every child process may have started yet.
    /// </summary>
    public void Start()
    {
        lock (Lock)
        {
            if (IsDisposed || HasStarted)
                return;
            HasStarted = true;
        }
        ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }

    public void Reset()
    {
        lock (Lock)
        {
            if (IsDisposed)
                return;

            HasStarted = false;
            LocalNormalizedValue = 0.0;
            CachedValue = Value;

            if (ChildrenDict.Count > 0)
                foreach (IProgressInfo c in ChildrenDict.Keys)
                    c.Reset();
        }
        ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }

    public void Complete()
    {
        lock (Lock)
        {
            if (IsDisposed)
                return;

            HasStarted = true;
            LocalNormalizedValue = 1.0;
            CachedValue = Value;

            if (ChildrenDict.Count > 0)
                foreach (IProgressInfo c in ChildrenDict.Keys)
                    c.Complete();
        }
        ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }

    public void Set(double value) =>
        SetNormalized((value - Min) / Range);

    public void SetNormalized(double normalizedValue)
    {
        int value;
        bool hasStarted;
        lock (Lock)
        {
            if (IsDisposed)
                return;

            hasStarted = HasStarted;
            HasStarted = true;

            value = CachedValue;

            normalizedValue = Math.Clamp(normalizedValue, 0.0, 1.0);

            if (ChildrenDict.Count > 0)
            {
                foreach (IProgressInfo c in ChildrenDict.Keys)
                    c.SetNormalized(normalizedValue);
                return;
            }

            if (LocalNormalizedValue != normalizedValue)
                LocalNormalizedValue = normalizedValue;

            CachedValue = Value;
        }
        if (value != CachedValue || hasStarted != HasStarted)
            ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }

    public void Increment() =>
        IncrementNormalized(1.0 / Range);

    public void Increment(int incrementValue) =>
        IncrementNormalized(incrementValue / Range);

    public void IncrementNormalized(double normalizedIncrementValue)
    {
        int value;
        bool hasStarted;
        lock (Lock)
        {
            if (IsDisposed)
                return;

            if (normalizedIncrementValue <= 0.0)
                return;

            hasStarted = HasStarted;
            HasStarted = true;

            value = CachedValue;

            if (ChildrenDict.Count > 0)
            {
                foreach (IProgressInfo c in ChildrenDict.Keys)
                    c.IncrementNormalized(normalizedIncrementValue);
                return;
            }

            if (LocalNormalizedValue >= 1.0)
                return;

            double normalizedValue = Math.Clamp(LocalNormalizedValue + normalizedIncrementValue, 0.0, 1.0);
            if (LocalNormalizedValue != normalizedValue)
                LocalNormalizedValue = normalizedValue;

            CachedValue = Value;
        }
        if (value != CachedValue || hasStarted != HasStarted)
            ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }


    public override string ToString() =>
        $"{(Name.IsNullOrEmpty() ? string.Empty : $"{Name} ")}{NormalizedValue.ToString("0.0%")}";


    #region IDisposable
    public volatile bool IsDisposed;
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        lock (Lock)
        {
            ProgressChanged = null;
            LocalNormalizedValue = NormalizedValue;
            foreach (IProgressInfo c in ChildrenDict.Keys)
            {
                try
                {
                    c?.ProgressChanged -= OnChildProgressChanged;
                    c?.Dispose();
                }
                catch { }
            }
            ChildrenDict.Clear();
        }
    }
    #endregion



}