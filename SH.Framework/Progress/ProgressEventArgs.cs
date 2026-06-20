using System;

namespace SH.Framework.Progress;

public sealed class ProgressEventArgs
{
    public ProgressEventArgs(ulong seqNum, IProgressInfo progress)
    {
        SeqNum = seqNum;
        Progress = progress ?? throw new ArgumentNullException(nameof(progress));
    }

    public ulong SeqNum { get; }
    public IProgressInfo Progress { get; }
}
