using SH.Framework.Cryptography;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Framework.Progress;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Content.Modding.Build;

public sealed class BuildSettings : IAsyncDisposable
{
    public BuildSettings(CancellationToken ct = default)
    {
        ExternalCT = ct;
        InternalCTS = new();
        LinkedCTS = CancellationTokenSource.CreateLinkedTokenSource(InternalCTS.Token, ExternalCT);
        ParallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = LinkedCTS.Token,
        };
        ForceSpritesheetSize2048 = false;
        Initialization = new ProgressInfo(nameof(Initialization));
        XmlBuild = new ProgressInfo(nameof(XmlBuild));
        JavaBuild = new ProgressInfo(nameof(JavaBuild));
    }

    public VersionInfo AppVersion { get; set; }
    public VersionInfo SpaceHavenVersion { get; set; }
    public bool ForceSpritesheetSize2048 { get; set; }

    public List<ModData> Mods { get; } = [];
    private CancellationToken ExternalCT { get; }
    private CancellationTokenSource InternalCTS { get; }
    private CancellationTokenSource LinkedCTS { get; }
    internal ParallelOptions ParallelOptions { get; }
    internal CancellationToken CT => ParallelOptions.CancellationToken;

    public IProgressInfo Initialization { get; set; }
    public IProgressInfo XmlBuild { get; set; }
    public IProgressInfo JavaBuild { get; set; }

    internal bool BuildFailure { get; private set; }
    internal void Fail()
    {
        BuildFailure = true;
        try { InternalCTS.Cancel(); } catch { }
    }


    internal string Hash { get; private set; }
    internal async Task<bool> ComputeHash(ILogger logger)
    {
        try
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(ForceSpritesheetSize2048)}={ForceSpritesheetSize2048}");

            Hash = XxHash64Calculator.ComputeFromString(sb.ToString(), logger);
            return true;
        }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        try { InternalCTS.Dispose(); } catch { }

        return ValueTask.CompletedTask;
    }
}
