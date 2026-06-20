using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SH.Framework.Extensions;

public static class ParallelOptionsX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParallelOptions Clone(this ParallelOptions options) =>
        options is null ? new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, } : new ParallelOptions
        {
            CancellationToken = options.CancellationToken,
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
            TaskScheduler = options.TaskScheduler
        };
}
