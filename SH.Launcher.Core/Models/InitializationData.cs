using SH.Content.Enums;
using SH.Framework.IO;

namespace SH.Launcher.Core.Models;

public sealed class InitializationData
{
    public VersionInfo SpaceHavenVersion { get; internal set; }
    public EGamePlatform GamePlatform { get; internal set; }
    public string JavaMainClass { get; internal set; }
    public string JavaVMArgs { get; internal set; }

    public bool BackupChanged { get; internal set; }
    public bool TemplateChanged { get; internal set; }
    public bool CacheChanged { get; internal set; }
}

