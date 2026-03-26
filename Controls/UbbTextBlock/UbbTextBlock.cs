using CC98.Share.Controls.Primitives;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UbbRender.Common;
using UbbRender.Parser;
using Windows.UI;
using Windows.UI.Text;
namespace UbbRender.Render
{
    public sealed partial class UbbTextBlock : Control
    {
        #region 依赖属性

        public static readonly DependencyProperty UbbTextProperty =
            DependencyProperty.Register(
                nameof(UbbText),
                typeof(string),
                typeof(UbbTextBlock),
                new PropertyMetadata(string.Empty, OnUbbTextChanged));

        public static new readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(UbbTextBlock),
                new PropertyMetadata(14.0, OnFontSizeChanged));
        public static readonly DependencyProperty BoldFontFamilyProperty =
            DependencyProperty.Register(
                nameof(BoldFontFamily),
                typeof(FontFamily),
                typeof(UbbTextBlock),
                new PropertyMetadata(null));

        public static new readonly DependencyProperty ForegroundProperty =
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
                new PropertyMetadata(400.0));

        
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

        
        private StackPanel _rootPanel;
        private UbbDocument _document;
        public Dictionary<UbbNodeType, IRenderStrategy> renderStrategies;
        public RenderContext context;
        #endregion

        
        public UbbTextBlock()
        {
            this.DefaultStyleKey = typeof(UbbTextBlock);
            InitializeRenderStrategies();
        }
        #region 事件
        public event EventHandler<MediaClickEventArgs> MediaClicked;
        public void OnMediaClicked(string src,MediaType mediaType)
        {
            MediaClicked?.Invoke(this, new MediaClickEventArgs(src,mediaType));
        }

        #endregion
        // 应用模板
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
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
                [UbbNodeType.Emoji]=new EmojiRenderStrategy(),
                [UbbNodeType.Latex]=new LatexRenderStrategy(),
                [UbbNodeType.Divider]=new DividerRenderStrategy(),
                [UbbNodeType.Markdown]=new MarkdownRenderStrategy()
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
                _document =UbbRender.Common.Parser.Parse(UbbText);
                context = new RenderContext
                {
                    Control = this,
                    Container = _rootPanel
                };

                context.Properties ??= [];
                context.PanelStack ??= new Stack<Panel>();

                // 填充默认属性
                context.Properties["FontSize"] = FontSize;
                context.Properties["Foreground"] = Foreground ?? new SolidColorBrush(Colors.Gray);
                context.Properties["BoldFontFamily"] = BoldFontFamily ?? new FontFamily("HarmonyOS Sans SC Bold");
                context.Properties["CodeBackground"] = CodeBackground ?? new SolidColorBrush(Color.FromArgb(0xff,0xe8,0xf4,0xf9));
                context.Properties["QuoteBackground"] = QuoteBackground ?? new SolidColorBrush(Color.FromArgb(20,0,120,215));
                context.Properties["ImageMaxWidth"] = ImageMaxWidth;

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
        public string GetSelectedText()
        {
            if (context != null)
            {
                return context.GetSelectedTextExternal();
            }
            return string.Empty;
        }

        /// <summary>
        /// 全选所有文本
        /// </summary>
        public void SelectAll()
        {
            if (context != null)
            {
                context.SelectAllExternal();
            }
        }
    }
}
