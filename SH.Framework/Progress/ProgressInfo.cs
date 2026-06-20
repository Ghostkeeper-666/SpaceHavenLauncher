using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SH.Framework.Progress;

public sealed class ProgressInfo : IProgressInfo
{
    public event EventHandler<ProgressEventArgs> ProgressChanged;

    private ulong SeqNum = 0;
    private void OnProgressChanged(object sender, ProgressEventArgs e) =>
        ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));

    public ProgressInfo(string name) =>
        Name = name;

    public ProgressInfo(string name, IEnumerable<IProgressInfo> weightlessChildren)
    {
        Name = name;
        if (weightlessChildren == null) return;
        foreach (IProgressInfo c in weightlessChildren)
            Add(c, 1.0);
    }

    public ProgressInfo(string name, IEnumerable<(IProgressInfo, double)> weightedChildren)
    {
        Name = name;
        if (weightedChildren == null) return;
        foreach ((IProgressInfo p, double weight) in weightedChildren)
            Add(p, weight);
    }

    public ProgressInfo(string name, IReadOnlyDictionary<IProgressInfo, double> weightedChildren)
    {
        Name = name;
        if (weightedChildren == null) return;
        foreach (KeyValuePair<IProgressInfo, double> kvp in weightedChildren)
            Add(kvp.Key, kvp.Value);
    }

    public void Add(IProgressInfo child, double weight = 1.0)
    {
        if (child == null) return;
        ProgressArgumentException.ThrowIfNotFiniteAndPositive(weight);
        lock (Lock)
        {
            if (LocalNormalizedValue != 0.0)
                child.SetNormalized(LocalNormalizedValue);
            ChildrenDict.Add(child, weight);
            TotalWeight += weight;
            child.ProgressChanged -= OnProgressChanged;
            child.ProgressChanged += OnProgressChanged;
        }
    }

    public void Remove(IProgressInfo child)
    {
        lock (Lock)
        {
            if (!ChildrenDict.Remove(child, out double childWeight))
                return;
            TotalWeight = Math.Max(0.0, TotalWeight - childWeight);
            child.ProgressChanged -= OnProgressChanged;
        }
    }

    public void RemoveAll()
    {
        lock (Lock)
        {
            double normalizedValue = NormalizedValue;
            foreach (IProgressInfo child in ChildrenDict.Keys)
                child.ProgressChanged -= OnProgressChanged;
            ChildrenDict.Clear();
            TotalWeight = 0.0;
            LocalNormalizedValue = normalizedValue;
        }
    }

    private readonly Lock Lock = new();

    private readonly OrderedDictionary<IProgressInfo, double> ChildrenDict = [];

    public string Name { get; }
    public double Range => Max - Min;
    public double Min { get; set; } = 0.0;
    public double Max { get; set; } = 100.0;
    public double TotalWeight { get; private set; } = 0.0;
    private double LocalNormalizedValue { get; set; } = 0.0;
    public double NormalizedValue
    {
        get
        {
            return ChildrenDict.Count <= 0 ? LocalNormalizedValue :
            ChildrenDict.Sum(kvp => kvp.Key.NormalizedValue * kvp.Value) / TotalWeight;
        }
    }
    public double Value =>
        Range * NormalizedValue + Min;
    public double RemainingNormalizedValue =>
        1.0 - NormalizedValue;
    public double RemainingValue =>
        Max - Value;




    public void Reset()
    {
        lock (Lock)
        {
            if (IsDisposed)
                return;

            if (ChildrenDict.Count > 0)
            {
                foreach (IProgressInfo c in ChildrenDict.Keys)
                    c.Reset();
                return;
            }

            LocalNormalizedValue = 0.0;
        }
        ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }

    public void Complete()
    {
        lock (Lock)
        {
            if (IsDisposed)
                return;

            if (ChildrenDict.Count > 0)
            {
                foreach (IProgressInfo c in ChildrenDict.Keys)
                    c.Complete();
                return;
            }

            LocalNormalizedValue = 1.0;
        }
        ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }

    public void Set(double value) =>
        SetNormalized((value - Min) / Range);

    public void SetNormalized(double normalizedValue)
    {
        lock (Lock)
        {
            if (IsDisposed)
                return;

            normalizedValue = Math.Clamp(normalizedValue, 0.0, 1.0);

            if (ChildrenDict.Count > 0)
            {
                foreach (IProgressInfo c in ChildrenDict.Keys)
                    c.SetNormalized(normalizedValue);
                return;
            }

            if (LocalNormalizedValue != normalizedValue)
                LocalNormalizedValue = normalizedValue;
        }
        ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }

    public void Increment() =>
        IncrementNormalized(1.0 / Range);

    public void Increment(double incrementValue) =>
        IncrementNormalized(incrementValue / Range);

    public void IncrementNormalized(double normalizedIncrementValue)
    {
        lock (Lock)
        {
            if (IsDisposed)
                return;

            if (normalizedIncrementValue <= 0.0)
                return;

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
        }
        ProgressChanged?.Invoke(this, new(Interlocked.Increment(ref SeqNum), this));
    }


    public override string ToString() => $"{Name} {NormalizedValue.ToString("0.0%")}";


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
            TotalWeight = 0.0;
            foreach (IProgressInfo c in ChildrenDict.Keys)
            {
                try
                {
                    c?.ProgressChanged -= OnProgressChanged;
                    c?.Dispose();
                }
                catch { }
            }
            ChildrenDict.Clear();
        }
    }
    #endregion



}