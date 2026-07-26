using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using SH.Framework.Logging;
using System.Collections.Generic;

namespace SH.Launcher.Views;

public sealed class ConsoleWindowLineColorizer : DocumentColorizingTransformer
{
    private readonly IReadOnlyList<LogMessage> LogMessages;

    public ConsoleWindowLineColorizer(IReadOnlyList<LogMessage> logMessages)
    {
        LogMessages = logMessages;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        int msgIdx = line.LineNumber - 1;
        IBrush color =
            msgIdx >= LogMessages.Count ? Brushes.Magenta :
            LogMessages[line.LineNumber - 1].Level switch
            {
                ELogLevel.Debug => Brushes.SkyBlue,
                ELogLevel.Success => Brushes.Lime,
                ELogLevel.Warn => Brushes.Yellow,
                ELogLevel.Error => new SolidColorBrush(Color.Parse("#FF3F1F")),
                ELogLevel.Info or _ => Brushes.LightGray,
            };

        ChangeLinePart(line.Offset, line.EndOffset, element => element.TextRunProperties.SetForegroundBrush(color));
    }

    public int CurrentLineCount { get; set; }
}
