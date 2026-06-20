using SH.Framework.Logging;

namespace SH.Launcher.Models;

public enum ELogVerbosity : int
{
    Minimal = ELogLevel.Success,
    Normal = ELogLevel.Info,
    Verbose = ELogLevel.Debug,
}


public static class LogVerbosityX
{
    public static ELogLevel ToLogLevel(this ELogVerbosity verbosity) => (ELogLevel)verbosity;
    public static ELogVerbosity ToLogVerbosity(this ELogLevel level) => level switch
	{
        ELogLevel.Debug => ELogVerbosity.Verbose,
        ELogLevel.Info => ELogVerbosity.Normal,
        _ => ELogVerbosity.Minimal,
    };
}
