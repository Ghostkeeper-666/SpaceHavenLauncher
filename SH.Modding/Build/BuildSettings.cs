using SH.Content.Enums;
using SH.Framework.IO;
using SH.Framework.Progress;
using SH.Modding.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Modding.Build;

public sealed class BuildSettings : IDisposable
{
    public BuildSettings(PathData paths, CancellationToken ct = default)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        ExternalCT = ct;
        InternalCTS = new();
        LinkedCTS = CancellationTokenSource.CreateLinkedTokenSource(InternalCTS.Token, ExternalCT);
        ParallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = LinkedCTS.Token,
        };
    }

    public required VersionInfo AppVersion { get; init; }
    public required string AppDir { get; init; }
    public required string WorkDir { get; init; }

    public required VersionInfo SpaceHavenVersion { get; init; }
    public required string SpaceHavenDir { get; init; }
    public required string SpaceHavenJarDir { get; init; }
    public required EGamePlatform GamePlatform { get; init; }

    public required bool GenerateAdditionalIntermediateBuildFiles { get; init; }
    public required bool AutoArrangeTechTreeLayout { get; init; }
    public required bool SkipRebuilding { get; init; }
    public List<ModData> Mods { get; private set; } = [];

    public IProgressInfo BuildProgress { get; set; }

    public PathData Paths { get; }

    private CancellationToken ExternalCT { get; }
    public CancellationTokenSource InternalCTS { get; private set; }
    private CancellationTokenSource LinkedCTS;

    internal ParallelOptions ParallelOptions;
    internal CancellationToken CT => ParallelOptions?.CancellationToken ?? default;

    public bool HasFailed => Failed;
    private volatile bool Failed;
    internal void Fail()
    {
        Failed = true;
        try { InternalCTS?.Cancel(); } catch { }
    }

    #region IDisposable
    public volatile bool IsDisposed;
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        BuildProgress = null;

        ParallelOptions = null;

        InternalCTS?.Dispose();
        InternalCTS = null;

        LinkedCTS?.Dispose();
        LinkedCTS = null;

        Mods?.Clear();
        Mods = null;
    }
    #endregion
}
