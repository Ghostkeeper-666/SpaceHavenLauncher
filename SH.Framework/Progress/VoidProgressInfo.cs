using System;
using System.Diagnostics;

namespace SH.Framework.Progress;

public sealed class VoidProgressInfo : IProgressInfo
{
    public event EventHandler<ProgressEventArgs> ProgressChanged;

    public Stopwatch Clock { get; } = new();
    public int Max { get; set; }
    public int Min { get; set; }
    public string Name { get; set; }
    public double NormalizedValue => 0.0;
    public double Range => Max - Min;
    public double RemainingNormalizedValue => 1.0;
    public double RemainingValue => Max - Min;
    public double TotalWeight => 0.0;
    public int Value => Min;
    public bool HasStarted => false;
    public bool HasCompleted => false;

    public IProgressInfo AddChild(IProgressInfo child, double weight = 1) => child;
    public void Complete() { }
    public IProgressInfo CreateChild(string childName, double weight = 1) => null;
    public void Dispose() { }
    public void Increment() { }
    public void Increment(int incrementValue) { }
    public void IncrementNormalized(double normalizedIncrementValue) { }
    public void Remove(IProgressInfo child) { }
    public void RemoveAll() { }
    public void Reset() { }
    public void Set(double value) { }
    public void SetNormalized(double normalizedValue) { }
    public void Start() { }
}
