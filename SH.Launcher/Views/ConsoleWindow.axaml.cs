using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Console;
using SH.Launcher.Extensions;
using SH.Launcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SH.Launcher.Views;

public partial class ConsoleWindow : Window
{
    public SpaceHavenLauncherConsole Console { get; }

    private readonly IClassicDesktopStyleApplicationLifetime Lifetime;

    private ScrollViewer TextEditorScrollViewer;

    private readonly List<LogMessage> LogMessages = new(100000);

    private readonly DispatchQueue DispatchQueue = new();

    public ConsoleWindow()
    {
        InitializeComponent();

        Lifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        TextEditor.Background = Brushes.Black;
        TextEditor.Foreground = Brushes.LightGray;

        Console = new();
        BatchLogger batchLogger = Console.Log as BatchLogger ?? Console.Log.Children.FirstOrDefault(log => log is BatchLogger) as BatchLogger;
        if (batchLogger == null)
            Console.Log.OnMessage += OnLogMessage;
        else batchLogger.OnMessages += OnLogMessages;
        TextEditor.TextArea.TextView.LineTransformers.Add(new ConsoleWindowLineColorizer(LogMessages));
        TextEditor.TextArea.AddHandler(PointerPressedEvent, TextArea_PointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        Loaded += OnLoadedAsync;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ConsoleWindowViewModel vm)
            return;

#if DEBUG
        try
        {
            WindowState = WindowState.Maximized;
            this.RestoreToMonitor(0);
        }
        catch { }
#endif

        Lifetime?.Shutdown(await Console.RunAsync(Lifetime?.Args ?? [], vm.CTS));
    }

    private void OnLogMessage(object sender, LogMessage m)
    {
        DispatchQueue.Run(() =>
        {
            TextEditor.AppendText(m.Text);
            TextEditorScrollViewer ??= GetScrollViewer();
            if (TextEditorScrollViewer != null)
                ScrollLineToBottom();
        });
    }

    private void OnLogMessages(object sender, IReadOnlyList<LogMessage> messages)
    {
        DispatchQueue.Run(() =>
        {
            StringBuilder sb = new();
            foreach (LogMessage m in messages)
            {
                string text = m.Text;
                int newLineCount = text.Count(c => c == '\n');
                if (newLineCount <= 0)
                    LogMessages.Add(m);
                else while (newLineCount-- >= 0)
                    LogMessages.Add(m);

                sb.AppendLine(m.Text);
            }
            TextEditor.AppendText(sb.ToString());

            TextEditorScrollViewer ??= GetScrollViewer();
            if (TextEditorScrollViewer != null)
                ScrollLineToBottom();
        });
    }

    private ScrollViewer GetScrollViewer()
    {
        PropertyInfo property = typeof(TextEditor).GetProperty("ScrollViewer", BindingFlags.Instance | BindingFlags.NonPublic);
        return property?.GetValue(TextEditor) as ScrollViewer;
    }

    private void ScrollLineToBottom()
    {
        double lineHeight = TextEditor.TextArea.TextView.DefaultLineHeight;
        double lineY = TextEditor.Document.LineCount * lineHeight;
        double targetY = lineY - TextEditorScrollViewer.Bounds.Height + lineHeight;
        TextEditorScrollViewer.Offset = new Vector(TextEditorScrollViewer.Offset.X, Math.Max(0, targetY));
    }

    private void TextArea_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2)
            return;
        try
        {

            int position = TextEditor.TextArea.Caret.Offset;
            DocumentLine line = TextEditor.Document.GetLineByOffset(position);

            Debug.WriteLine($"Double clicked line {line.LineNumber}");

            if (line.LineNumber > 0 && line.LineNumber <= LogMessages.Count)
            {
                LogMessage m = LogMessages[line.LineNumber - 1];
                _ = OS.OpenLinkAsync(m.Link, null);
            }
        }
        catch (Exception ex) { Debug.WriteLine(ex); }
    }
}



