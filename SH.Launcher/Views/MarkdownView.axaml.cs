using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SH.Framework.Extensions;
using SH.Framework.IO;
using SH.Framework.Logging;
using SH.Launcher.Models;
using SH.Launcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SH.Launcher.Views;

/// <summary>
/// This simple MD class was implemented because the MIT Licensed project 'Markdown.Avalonia' was not compatible with 'Avalonia 12.0' yet.
/// Avalonia's official Markdown control is paid, so no chance for a community based project like this.
/// </summary>
public partial class MarkdownView : UserControl
{
    public SharedState State => SharedState.State;
    public ILogger Log => State.Log;
    public PathViewModel Paths => State.Paths;
    public AppSettingsViewModel AppSettings => State.AppSettings;


    private static readonly FontFamily NormalFont =
        new("avares://SpaceHavenLauncher/Assets/Fonts/Roboto/#Roboto Condensed");

    private static readonly FontFamily CodeFont =
        new("avares://SpaceHavenLauncher/Assets/Fonts/Cascadia Mono/#Cascadia Mono");


    public static readonly StyledProperty<string> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownView, string>(nameof(Markdown));

    public static readonly StyledProperty<IBrush> HeaderColorProperty =
        AvaloniaProperty.Register<MarkdownView, IBrush>(nameof(HeaderColor), Brushes.DeepSkyBlue);

    public static readonly StyledProperty<IBrush> LinkColorProperty =
        AvaloniaProperty.Register<MarkdownView, IBrush>(nameof(LinkColor), Brushes.DarkOrange);

    public static readonly StyledProperty<IBrush> CodeColorProperty =
        AvaloniaProperty.Register<MarkdownView, IBrush>(nameof(CodeColor), Brushes.SkyBlue);

    public static readonly StyledProperty<IBrush> NormalColorProperty =
        AvaloniaProperty.Register<MarkdownView, IBrush>(nameof(NormalColor), Brushes.Honeydew);

    public static readonly StyledProperty<IBrush> EmphasisColorProperty =
        AvaloniaProperty.Register<MarkdownView, IBrush>(nameof(EmphasisColor), new SolidColorBrush(Color.Parse("#CFFF00")));


    public MarkdownView()
    {
        InitializeComponent();
        AddHandler(PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
    }

    public string Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public IBrush HeaderColor
    {
        get => GetValue(HeaderColorProperty);
        set => SetValue(HeaderColorProperty, value);
    }

    public IBrush LinkColor
    {
        get => GetValue(LinkColorProperty);
        set => SetValue(LinkColorProperty, value);
    }

    public IBrush CodeColor
    {
        get => GetValue(CodeColorProperty);
        set => SetValue(CodeColorProperty, value);
    }

    public IBrush NormalColor
    {
        get => GetValue(NormalColorProperty);
        set => SetValue(NormalColorProperty, value);
    }

    public IBrush EmphasisColor
    {
        get => GetValue(EmphasisColorProperty);
        set => SetValue(EmphasisColorProperty, value);
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkdownProperty)
        {
            string markdownText = change.NewValue as string;
            PART_Content.IsVisible = false;
            SetMarkdown(markdownText);
            MarkDownScrollViewer.ScrollToHome();
            PART_Content.IsVisible = true;
        }
    }

    public void SetMarkdown(string markdown)
    {
        PART_Content.Children.Clear();
        PART_Content.Children.Add(Render(markdown));
    }

    private readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public Control Render(string markdown)
    {
        StackPanel stackPanel = new()
        {
            Spacing = 6
        };
        MarkdownDocument document = Markdig.Markdown.Parse(markdown ?? string.Empty, Pipeline);
        foreach (Block block in document)
        {
            Control control = RenderBlock(block);
            control?.VerticalAlignment = VerticalAlignment.Bottom;
            if (control != null)
                stackPanel.Children.Add(control);
        }
        return stackPanel;
    }

    public Control RenderBlock(Block block)
    {
        if (block is HeadingBlock headingBlock)
        {
            string headingText = ExtractPlainText(headingBlock.Inline);
            TextBlock textBlock = new()
            {
                Name = nameof(HeadingBlock),
                FontSize = headingBlock.Level switch
                {
                    1 => 24,
                    2 => 20,
                    3 => 18,
                    _ => 15,
                },
                Margin = new Thickness(0, 32, 0, 12),
                Foreground = GetValue(HeaderColorProperty),
                FontFamily = NormalFont,
                FontWeight = FontWeight.Bold,
                Text = headingText,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            return textBlock;
        }

        if (block is ParagraphBlock paragraphBlock)
        {
            TextBlock textBlock = new()
            {
                Name = nameof(ParagraphBlock),
                TextWrapping = TextWrapping.Wrap,
                Foreground = GetValue(NormalColorProperty),
                FontFamily = NormalFont,
                FontWeight = FontWeight.Regular,
                LineSpacing = 8,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            textBlock.Inlines.AddRange(RenderInline(paragraphBlock.Inline, GetValue(NormalColorProperty)));
            return textBlock;
        }

        if (block is ListBlock listBlock)
        {
            StackPanel stackPanel = new()
            {
                Name = nameof(ListBlock),
                Margin = new Thickness(0),
                Spacing = 0,
            };
            foreach (ListItemBlock item in listBlock)
            {
                Control control = RenderBlock(item);
                control?.VerticalAlignment = VerticalAlignment.Bottom;
                if (control != null)
                    stackPanel.Children.Add(control);
            }
            return stackPanel;
        }

        if (block is ListItemBlock listItemBlock)
        {
            StackPanel stackPanel = new()
            {
                Name = nameof(ListItemBlock),
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            TextBlock bullet = new()
            {
                Name = $"{nameof(ListItemBlock)}.Bullet",
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 4),
                Text = "•",
                Foreground = GetValue(NormalColorProperty),
                VerticalAlignment = VerticalAlignment.Center,
            };
            StackPanel contentPanel = new()
            {
                Name = $"{nameof(ListItemBlock)}.Content",
                Spacing = 0,
            };
            foreach (Block innerBlock in listItemBlock)
            {
                Control control = RenderBlock(innerBlock);
                control?.VerticalAlignment = VerticalAlignment.Bottom;
                if (control != null)
                    contentPanel.Children.Add(control);
            }
            stackPanel.Children.Add(bullet);
            stackPanel.Children.Add(contentPanel);
            return stackPanel;
        }

        if (block is FencedCodeBlock fencedCodeBlock)
        {
            string codeText = fencedCodeBlock.Lines.ToString();
            TextBlock codeBlock = new()
            {
                Name = nameof(FencedCodeBlock),
                Text = codeText,
                Foreground = GetValue(CodeColorProperty),
                Background = new SolidColorBrush(Color.Parse("#7F1F1F1F")),
                Padding = new Thickness(8),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                FontFamily = CodeFont,
                FontWeight = FontWeight.Regular,
                LineSpacing = 0,
                VerticalAlignment = VerticalAlignment.Bottom,
                [ToolTip.TipProperty] = "Click to copy",
            };
            codeBlock.PointerPressed += async (_, _) => await SharedState.State.CopyToClipboardAsync(codeText);
            return codeBlock;
        }
        return null;
    }

    public List<Avalonia.Controls.Documents.Inline> RenderInline(ContainerInline inline, IBrush foreground)
    {
        List<Avalonia.Controls.Documents.Inline> result = new();
        if (inline == null)
            return result;

        foreach (Inline child in inline)
        {
            if (child is LiteralInline literalInline)
            {
                Avalonia.Controls.Documents.Run run = new(literalInline.Content.ToString())
                {
                    Name = nameof(LiteralInline),
                    Foreground = foreground,
                    BaselineAlignment = BaselineAlignment.Center,
                    FontWeight = FontWeight.Regular,
                    FontFamily = NormalFont,
                    FontSize = 15,
                };
                result.Add(run);
                continue;
            }

            if (child is LineBreakInline)
            {
                result.Add(new Avalonia.Controls.Documents.LineBreak()
                {
                    Name = nameof(LineBreakInline),
                });
                continue;
            }

            if (child is LinkInline linkInline)
            {
                string text = linkInline.FirstChild != null ? linkInline.FirstChild.ToString() : (linkInline.Url ?? string.Empty);
                TextBlock linkTextBlock = new()
                {
                    Name = $"{nameof(LinkInline)}.TextBlock",
                    Text = text,
                    Foreground = GetValue(LinkColorProperty),
                    //TextDecorations = TextDecorations.Underline,
                    Padding = new Thickness(0, 0, 0, 4),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    FontFamily = NormalFont,
                    FontWeight = FontWeight.SemiBold,
                    FontSize = 15,
                    [ToolTip.TipProperty] = $"Click to open \n\n{linkInline.Url}",
                };

                linkTextBlock.PointerPressed += OpenLink(SpaceHavenLauncher.GetAppDir(), linkInline?.Url, State.Log);

                Avalonia.Controls.Documents.InlineUIContainer container = new(linkTextBlock)
                {
                    Name = nameof(LinkInline),
                    BaselineAlignment = BaselineAlignment.Bottom,
                };
                result.Add(container);
                continue;
            }

            if (child is CodeInline codeInline)
            {
                TextBlock codeTextBlock = new()
                {
                    Name = $"{nameof(CodeInline)}.TextBlock",
                    Text = codeInline.Content,
                    Foreground = GetValue(EmphasisColorProperty), // yes, EmphasisColor, not CodeColor
                    Background = new SolidColorBrush(Color.Parse("#7F1F1F1F")),
                    Margin = new Thickness(0, 0, 0, 7),
                    Padding = new Thickness(4, 0, 4, 0),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    FontFamily = CodeFont,
                    FontWeight = FontWeight.Regular,
                    FontSize = 13,
                    [ToolTip.TipProperty] = "Click to copy",
                };

                codeTextBlock.PointerPressed += async (_, _) =>
                    await SharedState.State.CopyToClipboardAsync(codeInline.Content);

                Avalonia.Controls.Documents.InlineUIContainer container = new(codeTextBlock)
                {
                    Name = nameof(CodeInline),
                    BaselineAlignment = BaselineAlignment.Bottom
                };
                result.Add(container);
                continue;
            }

            if (child is EmphasisInline emphasisInline)
            {
                List<Avalonia.Controls.Documents.Inline> inner = RenderInline(emphasisInline, foreground);
                foreach (Avalonia.Controls.Documents.Inline innerInline in inner)
                {
                    if (innerInline is Avalonia.Controls.Documents.Run run)
                    {
                        if (emphasisInline.DelimiterCount == 1 || emphasisInline.DelimiterCount == 3)
                        {
                            run.FontFamily = NormalFont;
                            run.FontStyle = FontStyle.Italic;
                            run.Foreground = EmphasisColor;
                        }
                        if (emphasisInline.DelimiterCount == 2 || emphasisInline.DelimiterCount == 3)
                        {
                            run.FontFamily = NormalFont;
                            run.FontWeight = FontWeight.Bold;
                            run.Foreground = EmphasisColor;
                        }
                    }
                    result.Add(innerInline);
                }
                continue;
            }
        }
        return result;
    }

    private static EventHandler<PointerPressedEventArgs> OpenLink(string baseDir, string url, ILogger logger) =>
        (s, e) => SharedState.State.DispatchQueue.TryEnqueue(async () =>
        {
            url = url?.Trim('"')?.Trim();

            if (url.IsNullOrWhiteSpace())
                return;

            if (!url.StartsWith("tab://"))
            {
                await OS.OpenLinkAsync(baseDir, url, logger);
                return;
            }

            string[] parts = url.Substring(6).Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (!Enum.TryParse(parts.FirstOrDefault() ?? string.Empty, true, out EPageType page))
                return;

            switch (page)
            {
                case EPageType.LearningComputer:
                case EPageType.NavigationConsole:
                case EPageType.SystemCore:
                case EPageType.Airlock:
                    LeftPaneItem item = SharedState.State.LeftPaneItems.FirstOrDefault(item => item.Type == page);
                    if (item != null) SharedState.State.SelectedLeftPaneItem = item;
                    return;

                case EPageType.Mod:
                    if (parts.Length < 2)
                        return;
                    LeftPaneItem mod = SharedState.State.LeftPaneItems.FirstOrDefault(item => item.Type == EPageType.Mod && (item?.Mod?.Name?.Replace(" ", string.Empty).Equals(parts[1].Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase) ?? false));
                    if (mod != null) SharedState.State.SelectedLeftPaneItem = mod;
                    return;

                default:
                    return;
            }
        });

    private string ExtractPlainText(ContainerInline inline)
    {
        if (inline == null)
            return string.Empty;
        StringBuilder builder = new();
        foreach (Inline child in inline)
        {
            if (child is LiteralInline literal)
                builder.Append(System.Net.WebUtility.HtmlDecode(literal.Content.ToString()));
            else if (child is HtmlEntityInline html)
                builder.Append(html.Transcoded.Text);
            else if (child is LineBreakInline)
                builder.Append(' ');
            else if (child is ContainerInline container)
                builder.Append(ExtractPlainText(container));
        }
        return builder.ToString();
    }


    private void OnWheel(object sender, PointerWheelEventArgs e)
    {
        // Scroll Faster!
        const double multiplier = 2.0;
        const double step = 60.0;
        Vector offset = MarkDownScrollViewer.Offset;
        double newY = MarkDownScrollViewer.Offset.Y - (e.Delta.Y * multiplier * step);
        MarkDownScrollViewer.SetValue(ScrollViewer.OffsetProperty, new Vector(offset.X, newY));
        e.Handled = true;
    }

}