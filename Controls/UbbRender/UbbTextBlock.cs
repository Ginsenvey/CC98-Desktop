using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.UI.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using CC98.Controls.UbbRenderer.Common;
namespace CC98.Controls
{
    public sealed partial class UbbTextBlock : Control
    {
        // 依赖属性

        public static readonly DependencyProperty UbbTextProperty =
            DependencyProperty.Register(
                nameof(UbbText),
                typeof(string),
                typeof(UbbTextBlock),
                new PropertyMetadata(string.Empty, OnUbbTextChanged));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(UbbTextBlock),
                new PropertyMetadata(14.0, OnFontSizeChanged));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(UbbTextBlock),
                new PropertyMetadata(null, OnForegroundChanged));

        public static readonly DependencyProperty CodeBackgroundProperty =
            DependencyProperty.Register(
                nameof(CodeBackground),
                typeof(Brush),
                typeof(UbbTextBlock),
                new PropertyMetadata(null));

        public static readonly DependencyProperty QuoteBackgroundProperty =
            DependencyProperty.Register(
                nameof(QuoteBackground),
                typeof(Brush),
                typeof(UbbTextBlock),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ImageMaxWidthProperty =
            DependencyProperty.Register(
                nameof(ImageMaxWidth),
                typeof(double),
                typeof(UbbTextBlock),
                new PropertyMetadata(300.0));

        // 属性
        public string UbbText
        {
            get => (string)GetValue(UbbTextProperty);
            set => SetValue(UbbTextProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
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

        // 内部控件
        private ScrollViewer _scrollViewer;
        private StackPanel _rootPanel;
        private UbbDocument _document;
        public Dictionary<UbbNodeType, IRenderStrategy> renderStrategies;

        // 构造函数
        public UbbTextBlock()
        {
            this.DefaultStyleKey = typeof(UbbTextBlock);
            InitializeRenderStrategies();
        }

        // 应用模板
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
            _rootPanel = GetTemplateChild("PART_RootPanel") as StackPanel;

            if (_rootPanel != null)
            {
                RenderContent();
            }
        }

        // 初始化渲染策略
        private void InitializeRenderStrategies()
        {
            renderStrategies = new Dictionary<UbbNodeType, IRenderStrategy>
            {
                //[UbbNodeType.Document] = new DocumentRenderStrategy(),
                [UbbNodeType.Text] = new TextRenderStrategy(),
                [UbbNodeType.Bold] = new BoldRenderStrategy(),
                [UbbNodeType.Italic] = new ItalicRenderStrategy(),
                [UbbNodeType.Underline] = new UnderlineRenderStrategy(),
                [UbbNodeType.Strikethrough] = new StrikethroughRenderStrategy(),
                [UbbNodeType.Size] = new SizeRenderStrategy(),
                [UbbNodeType.Font] = new FontRenderStrategy(),
                [UbbNodeType.Color] = new ColorRenderStrategy(),
                [UbbNodeType.Url] = new UrlRenderStrategy(),
                [UbbNodeType.Image] = new ImageRenderStrategy(),
                //[UbbNodeType.Audio] = new AudioRenderStrategy(),
                //[UbbNodeType.Video] = new VideoRenderStrategy(),
                [UbbNodeType.Code] = new CodeRenderStrategy(),
                [UbbNodeType.Quote] = new QuoteRenderStrategy(),
                [UbbNodeType.Align] = new AlignRenderStrategy(),
                [UbbNodeType.Left] = new LeftRenderStrategy(),
                [UbbNodeType.Center] = new CenterRenderStrategy(),
                [UbbNodeType.Right] = new RightRenderStrategy(),
                //[UbbNodeType.List] = new ListRenderStrategy(),
                //[UbbNodeType.ListItem] = new ListItemRenderStrategy(),
                [UbbNodeType.Paragraph] = new ParagraphRenderStrategy(),
                [UbbNodeType.LineBreak] = new LineBreakRenderStrategy(),
                [UbbNodeType.Emoji]=new EmojiRenderStrategy()
            };
        }

        // 属性变更处理
        private static void OnUbbTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UbbTextBlock control && control._rootPanel != null)
            {
                control.RenderContent();
            }
        }

        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UbbTextBlock control && control._rootPanel != null)
            {
                control.RenderContent();
            }
        }

        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UbbTextBlock control && control._rootPanel != null)
            {
                control.RenderContent();
            }
        }

        // 渲染内容
        private void RenderContent()
        {
            if ( UbbText==null || _rootPanel == null)
                return;

            // 清空现有内容
            _rootPanel.Children.Clear();

            try
            {
                // 解析UBB为文档树
                _document = UbbParser.Parse(UbbText);

                // 创建渲染上下文
                var context = new RenderContext
                {
                    Control = this,
                    Container = _rootPanel,
                    Properties =
                    {
                        ["FontSize"] = FontSize,
                        ["Foreground"] = Foreground ?? new SolidColorBrush(Colors.Gray),
                        ["CodeBackground"] = CodeBackground ?? new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)),
                        ["QuoteBackground"] = QuoteBackground ?? new SolidColorBrush(Color.FromArgb(20, 0, 120, 215)),
                        ["ImageMaxWidth"] = ImageMaxWidth
                    }
                };

                // 渲染文档
                context.RenderNode(_document.Root);

                // 结束最后一个文本块
                context.FinalizeCurrentTextBlock();
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




    }
}
