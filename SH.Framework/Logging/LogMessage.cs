using System.Threading;

namespace SH.Framework.Logging;

public sealed class LogMessage
{
    private static ulong GlobalSeqNum;
    public static LogMessage Empty { get; } = new(ELogLevel.None, string.Empty);


    public LogMessage(ELogLevel level, object o)
    {
        Level = level;
        SeqNum = Interlocked.Increment(ref GlobalSeqNum);
        RawText = $"{o}";
    }

    public LogMessage(ELogLevel level, object o, string link)
    {
        Level = level;
        SeqNum = Interlocked.Increment(ref GlobalSeqNum);
        RawText = $"{o}";
        Link = link;
    }

    public ulong SeqNum { get; }
    public ELogLevel Level { get; }

    public string Prefix { get; set; }
    public string Suffix { get; set; }

    public string Text => $"{Prefix}{RawText}{Suffix}";
    private readonly string RawText;

    public string Link { get; set; }

    public override string ToString() => Text;
}
