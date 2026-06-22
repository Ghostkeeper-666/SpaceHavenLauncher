using System;

namespace SH.Framework.Progress;

public interface IProgressInfo : IDisposable
{
    public event EventHandler<ProgressEventArgs> ProgressChanged;

    public int Max { get; set; }
    public int Min { get; set; }
    public string Name { get; }
    public double NormalizedValue { get; }
    public double Range { get; }
    public double RemainingNormalizedValue { get; }
    public double RemainingValue { get; }
    public double TotalWeight { get; }
    public int Value { get; }
    public bool HasStarted { get; }

    public void Start();
    public IProgressInfo CreateChild(string childName, double weight = 1.0);
    public IProgressInfo AddChild(IProgressInfo child, double weight = 1);
    public void Complete();
    public void Increment();
    public void Increment(int incrementValue);
    public void IncrementNormalized(double normalizedIncrementValue);
    public void Remove(IProgressInfo child);
    public void RemoveAll();
    public void Reset();
    public void Set(double value);
    public void SetNormalized(double normalizedValue);
}