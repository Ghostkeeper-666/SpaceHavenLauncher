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
        SkipRebuilding = true;
        InitializationProgress = new ProgressInfo(nameof(InitializationProgress));
        XmlBuildProgress = new ProgressInfo(nameof(XmlBuildProgress));
        JavaBuildProgress = new ProgressInfo(nameof(JavaBuildProgress));
    }

    public VersionInfo AppVersion { get; set; }
    public string AppDir { get; set; }
    public string WorkDir { get; set; }

    public VersionInfo SpaceHavenVersion { get; set; }
    public string SpaceHavenDir { get; set; }
    public string SpaceHavenJarDir { get; set; }
    public EGamePlatform GamePlatform { get; set; }

    public bool SkipRebuilding { get; set; }
    public List<ModData> Mods { get; } = [];

    public IProgressInfo InitializationProgress { get; set; }
    public IProgressInfo XmlBuildProgress { get; set; }
    public IProgressInfo JavaBuildProgress { get; set; }

    public PathData Paths { get; }

    private CancellationToken ExternalCT { get; }
    private CancellationTokenSource InternalCTS { get; }
    private CancellationTokenSource LinkedCTS { get; }
    internal ParallelOptions ParallelOptions { get; }
    internal CancellationToken CT => ParallelOptions.CancellationToken;


    internal bool BuildFailure { get; private set; }
    internal void Fail()
    {
        BuildFailure = true;
        try { InternalCTS.Cancel(); } catch { }
    }

    #region IDisposable
    public volatile bool IsDisposed;
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        InternalCTS?.Dispose();
    }
    #endregion
}
