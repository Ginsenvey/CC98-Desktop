using System;
using System.Collections.Generic;
using Windows.UI;
using CC98.Controls.Primitives;
using CC98.Controls.UbbTextBlock.Common;
using CC98.Controls.UbbTextBlock.Common.Events;
using CC98.Controls.UbbTextBlock.Parser;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CC98.Controls.UbbTextBlock;

public sealed partial class UbbTextBlock : Control
{
    #region 依赖属性

    public static readonly DependencyProperty UbbTextProperty =
        DependencyProperty.Register(
            nameof(UbbText),
            typeof(string),
            typeof(UbbTextBlock),
            new(string.Empty, OnUbbTextChanged));

    public new static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(
            nameof(FontSize),
            typeof(double),
            typeof(UbbTextBlock),
            new(14.0, OnFontSizeChanged));

    public static readonly DependencyProperty BoldFontFamilyProperty =
        DependencyProperty.Register(
            nameof(BoldFontFamily),
            typeof(FontFamily),
            typeof(UbbTextBlock),
            new(null));

    public new static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(
            nameof(Foreground),
            typeof(Brush),
            typeof(UbbTextBlock),
            new(null, OnForegroundChanged));

    public static readonly DependencyProperty CodeBackgroundProperty =
        DependencyProperty.Register(
            nameof(CodeBackground),
            typeof(Brush),
            typeof(UbbTextBlock),
            new(null));

    public static readonly DependencyProperty QuoteBackgroundProperty =
        DependencyProperty.Register(
            nameof(QuoteBackground),
            typeof(Brush),
            typeof(UbbTextBlock),
            new(null));

    public static readonly DependencyProperty ImageMaxWidthProperty =
        DependencyProperty.Register(
            nameof(ImageMaxWidth),
            typeof(double),
            typeof(UbbTextBlock),
            new(double.PositiveInfinity));
    public static readonly DependencyProperty ImageMaxHeightProperty =
        DependencyProperty.Register(
            nameof(ImageMaxHeight),
            typeof(double),
            typeof(UbbTextBlock),
            new(double.PositiveInfinity));

    public static readonly DependencyProperty HideImageProperty =
        DependencyProperty.Register(
            nameof(HideImage),
            typeof(bool),
            typeof(UbbTextBlock),
            new(false, OnHideImageChanged));


    public string UbbText
    {
        get => (string)GetValue(UbbTextProperty);
        set => SetValue(UbbTextProperty, value);
    }

    public new double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontFamily BoldFontFamily
    {
        get => (FontFamily)GetValue(BoldFontFamilyProperty);
        set => SetValue(BoldFontFamilyProperty, value);
    }

    public new Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public Brush CodeBackground
    {
        get => (Brush)GetValue(CodeBackgroundProperty);
        set => SetValue(CodeBackgroundProperty, value);
    }

    public Brush QuoteBackground
    {
        get => (Brush)GetValue(QuoteBackgroundProperty);
        set => SetValue(QuoteBackgroundProperty, value);
    }

    public double ImageMaxWidth
    {
        get => (double)GetValue(ImageMaxWidthProperty);
        set => SetValue(ImageMaxWidthProperty, value);
    }
    public double ImageMaxHeight
    {
        get => (double)GetValue(ImageMaxHeightProperty);
        set => SetValue(ImageMaxHeightProperty, value);
    }

    public bool HideImage
    {
        get => (bool)GetValue(HideImageProperty);
        set => SetValue(HideImageProperty, value);
    }

    private StackPanel _rootPanel;
    private UbbDocument _document;

    public static readonly IReadOnlyDictionary<UbbNodeType, IRenderStrategy> RenderStrategies =
        new Dictionary<UbbNodeType, IRenderStrategy>
        {
            [UbbNodeType.Text] = new TextRenderStrategy(),
            [UbbNodeType.Bold] = new BoldRenderStrategy(),
            [UbbNodeType.Italic] = new ItalicRenderStrategy(),
            [UbbNodeType.Underline] = new UnderlineRenderStrategy(),
            [UbbNodeType.Strikethrough] = new StrikethroughRenderStrategy(),
            [UbbNodeType.Size] = new SizeRenderStrategy(),
            [UbbNodeType.Font] = new FontRenderStrategy(),
            [UbbNodeType.Color] = new ColorRenderStrategy(),
            [UbbNodeType.Url] = new UrlRenderStrategy(),
            [UbbNodeType.Topic] = new TopicRenderStrategy(),
            [UbbNodeType.Image] = new ImageRenderStrategy(),
            [UbbNodeType.Audio] = new AudioRenderStrategy(),
            [UbbNodeType.Video] = new VideoRenderStrategy(),
            [UbbNodeType.Upload] = new FileRenderStrategy(),
            [UbbNodeType.Code] = new CodeRenderStrategy(),
            [UbbNodeType.Quote] = new FlatQuoteRenderStrategy(),
            [UbbNodeType.Align] = new AlignRenderStrategy(),
            [UbbNodeType.Left] = new LeftRenderStrategy(),
            [UbbNodeType.Center] = new CenterRenderStrategy(),
            [UbbNodeType.Right] = new RightRenderStrategy(),
            [UbbNodeType.Table] = new TableRenderStrategy(),
            [UbbNodeType.TableRow] = new TableRowRenderStrategy(),
            [UbbNodeType.TableCell] = new TableCellRenderStrategy(),
            [UbbNodeType.Paragraph] = new ParagraphRenderStrategy(),
            [UbbNodeType.Emoji] = new EmojiRenderStrategy(),
            [UbbNodeType.Latex] = new LatexRenderStrategy(),
            [UbbNodeType.Divider] = new DividerRenderStrategy(),
            [UbbNodeType.Markdown] = new MarkdownRenderStrategy(),
            [UbbNodeType.At] = new AtRenderStrategy(),
            [UbbNodeType.PosterOnly] = new PosterOnlyRenderStrategy(),
            [UbbNodeType.ReplyView] = new ReplyViewRenderStrategy(),
            [UbbNodeType.NeedReply] = new ReplyViewRenderStrategy()
        };

    public RenderContext Context;

    #endregion

    #region 属性变更处理

    private static void OnUbbTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UbbTextBlock control && control._rootPanel != null) control.RenderContent();
    }

    private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UbbTextBlock control && control._rootPanel != null) control.RenderContent();
    }

    private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UbbTextBlock control && control._rootPanel != null) control.RenderContent();
    }

    private static void OnHideImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UbbTextBlock control && control._rootPanel != null) control.RenderContent();
    }

    #endregion

    #region 功能实现

    public UbbTextBlock()
    {
        DefaultStyleKey = typeof(UbbTextBlock);
    }

    #region 事件

    public event EventHandler<MediaClickEventArgs> MediaClicked;

    public void OnMediaClicked(string src, MediaType mediaType)
    {
        MediaClicked?.Invoke(this, new(src, mediaType));
    }

    #endregion

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _rootPanel = GetTemplateChild("PART_RootPanel") as StackPanel;

        if (_rootPanel != null) RenderContent();
    }


    // 渲染内容
    private void RenderContent()
    {
        if (UbbText == null || _rootPanel == null)
            return;

        // 清空现有内容
        _rootPanel.Children.Clear();

        try
        {
            _document = Parser.Parser.Parse(UbbText);
            Context = new()
            {
                Control = this,
                Container = _rootPanel
            };

            Context.Properties ??= [];
            Context.PanelStack ??= new();

            // 填充默认属性
            Context.Properties["FontSize"] = FontSize;
            Context.Properties["Foreground"] = Foreground ?? new SolidColorBrush(Colors.Gray);
            Context.Properties["BoldFontFamily"] = BoldFontFamily ?? new FontFamily("HarmonyOS Sans SC Bold");
            Context.Properties["CodeBackground"] =
                CodeBackground ?? new SolidColorBrush(Color.FromArgb(0xff, 0xe8, 0xf4, 0xf9));
            Context.Properties["QuoteBackground"] =
                QuoteBackground ?? new SolidColorBrush(Color.FromArgb(20, 0, 120, 215));
            Context.Properties["ImageMaxWidth"] = ImageMaxWidth;
            Context.Properties["ImageMaxHeight"] = ImageMaxHeight;

            // 渲染文档
            Context.RenderNode(_document.Root);

            // 结束最后一个文本块
            Context.FinalizeCurrentTextBlock();
        }
        catch (Exception ex)
        {
            // 显示错误
            var errorText = new TextBlock
            {
                Text = $"渲染错误: {ex.Message}",
                Foreground = new SolidColorBrush(Colors.Red)
            };
            _rootPanel.Children.Add(errorText);
        }
    }

    #endregion
}