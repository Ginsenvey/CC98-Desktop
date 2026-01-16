using ColorCode;
using CommunityToolkit.WinUI.UI.Controls.Markdown.Render;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Text;
namespace CC98.Controls.UbbRenderer.Common;

// 渲染策略接口
public interface IRenderStrategy
{
    void Render(UbbNode node, RenderContext context);
}
public class TextRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TextNode textNode && !string.IsNullOrWhiteSpace(textNode.Content))
        {
            string content = textNode.Content;
            if (string.IsNullOrEmpty(content))
                return;

            // 检查是否只包含换行符
            if (IsOnlyNewLine(content))
            {
                HandleNewLineOnlyContent(node, context);
                return;
            }

            // 处理包含换行符的文本
            if (content.Contains("\r") || content.Contains("\n"))
            {
                HandleTextWithNewLines(content, node, context);
                return;
            }

            // 普通文本
            var run = new Run { Text = content };
            context.AddInline(run);

        }
    }
    private bool IsOnlyNewLine(string text)
    {
        return text == "\r" || text == "\n" || text == "\r\n" ||
               text == "\r\n\r\n" || text == "\n\n"; // 多个换行符
    }
    private void HandleNewLineOnlyContent(UbbNode node, RenderContext context)
    {
        // 获取父节点类型
        var parentType = node.Parent?.Type;

        // 规则1：在段落内的换行符 -> 转换为LineBreak
        if (parentType == UbbNodeType.Paragraph)
        {
            // 添加LineBreak节点
            var lineBreak = TagNode.Create(UbbNodeType.LineBreak);
            context.RenderNode(lineBreak);
            return;
        }

        // 规则2：在引用块、代码块等内部的换行符 -> 创建换行
        if (IsInsideBlockContainer(node))
        {
            // 块级容器内的换行符应该换行
            var lineBreak = TagNode.Create(UbbNodeType.LineBreak);
            context.RenderNode(lineBreak);
            return;
        }

        // 规则3：在文档根节点下的单独换行符 -> 忽略或小间距
        if (parentType == UbbNodeType.Document)
        {
            // 文档根节点下的单独换行符，忽略或添加很小的间距
            // 这取决于你想要的视觉效果
            context.FinalizeCurrentTextBlock(); // 结束当前文本块
                                                // 可选：添加一个小的分隔符
                                                // context.AddToContainer(new TextBlock { Height = 2 });
            return;
        }

        // 默认：转换为LineBreak
        var defaultLineBreak = TagNode.Create(UbbNodeType.LineBreak);
        context.RenderNode(defaultLineBreak);
    }

    private void HandleTextWithNewLines(string content, UbbNode node, RenderContext context)
    {
        // 分割文本
        var lines = SplitTextWithNewLines(content);

        for (int i = 0; i < lines.Count; i++)
        {
            var lineText = lines[i].text;
            var hasNewLine = lines[i].hasNewLine;

            // 添加文本
            if (!string.IsNullOrEmpty(lineText))
            {
                var run = new Run { Text = lineText };
                context.AddInline(run);
            }

            // 添加换行
            if (hasNewLine && i < lines.Count - 1)
            {
                // 根据上下文决定如何换行
                if (ShouldCreateLineBreakInContext(node))
                {
                    // 创建LineBreak节点
                    var lineBreak = TagNode.Create(UbbNodeType.LineBreak);
                    context.RenderNode(lineBreak);
                }
                else
                {
                    // 在非段落上下文中，可能需要结束当前文本块
                    context.FinalizeCurrentTextBlock();
                }
            }
        }
    }

    private List<(string text, bool hasNewLine)> SplitTextWithNewLines(string text)
    {
        var result = new List<(string, bool)>();
        var currentLine = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '\r' || c == '\n')
            {
                // 保存当前行
                result.Add((currentLine.ToString(), true));
                currentLine.Clear();

                // 跳过 \r\n 组合
                if (i + 1 < text.Length && c == '\r' && text[i + 1] == '\n')
                {
                    i++;
                }
            }
            else
            {
                currentLine.Append(c);
            }
        }

        // 添加最后一行
        if (currentLine.Length > 0)
        {
            result.Add((currentLine.ToString(), false));
        }
        // 处理以换行符结尾的情况
        else if (text.Length > 0 && (text[^1] == '\r' || text[^1] == '\n'))
        {
            result.Add(("", false));
        }

        return result;
    }

    private bool IsInsideBlockContainer(UbbNode node)
    {
        // 检查节点是否在块级容器内
        var current = node.Parent;
        while (current != null)
        {
            if (IsBlockContainer(current.Type))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private bool IsBlockContainer(UbbNodeType type)
    {
        return type == UbbNodeType.Quote ||
               type == UbbNodeType.Code ||
               type == UbbNodeType.List;
    }

    private bool ShouldCreateLineBreakInContext(UbbNode node)
    {
        // 检查当前上下文是否应该创建LineBreak
        var parent = node.Parent;

        // 在段落内 -> 创建LineBreak
        if (parent?.Type == UbbNodeType.Paragraph)
            return true;

        // 在引用块内 -> 创建LineBreak
        if (IsInsideBlockContainer(node))
            return true;

        // 其他情况 -> 不创建LineBreak
        return false;
    }
}

// 粗体渲染策略
public class BoldRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var bold = new Bold();
        context.BeginInlineContainer(bold);
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
        context.EndInlineContainer();
    }
}

// 斜体渲染策略
public class ItalicRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var italic = new Italic();
        context.BeginInlineContainer(italic);
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
        context.EndInlineContainer();
    }
}

// 下划线渲染策略
public class UnderlineRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var underline = new Underline();
        context.BeginInlineContainer(underline);
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
        context.EndInlineContainer();
    }
}

// 删除线渲染策略
public class StrikethroughRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var span = new Span();
        span.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
        context.BeginInlineContainer(span);
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
        context.EndInlineContainer();
    }
    private string CollectText(UbbNode node)
    {
        var text = "";
        foreach (var child in node.Children)
        {
            if (child is TextNode textNode)
            {
                text += textNode.Content;
            }
            else
            {
                text += CollectText(child);
            }
        }
        return text;
    }
}

// 字体大小渲染策略
public class SizeRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            
            var sizeStr = tagNode.GetAttribute("value");
            if(int.TryParse(sizeStr,out int sizeInt))
            {
                double pixels = sizeStr.Contains("px") ? sizeInt : ConvertUbbSizeToPixels(sizeInt);
                var span = new Span { FontSize = pixels };
                context.BeginInlineContainer(span);
                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }
                context.EndInlineContainer();
            }
            else
            {
                // 默认处理子节点
                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }
            }
        }
    }
    private double ConvertUbbSizeToPixels(int ubbSize)
    {
        // 简单的分段线性插值
        if (ubbSize <= 1) return 8;   // 最小尺寸
        if (ubbSize == 2) return 10;  // 较小
        if (ubbSize == 3) return 13;  
        if (ubbSize == 4) return 17;  // 插值
        if (ubbSize == 5) return 22;  
        if (ubbSize == 6) return 26;  // 插值
        if (ubbSize == 7) return 30;  // 插值
        if (ubbSize == 8) return 32;  // 插值
        if (ubbSize == 9) return 34;  // 插值
        if (ubbSize == 10) return 35; // 接近36
        if (ubbSize == 11) return 35.5;
        if (ubbSize == 12) return 35.8;
        if (ubbSize == 13) return 36; 

        // 对于更大的值，使用渐进增长
        if (ubbSize > 13)
        {
            // 超过13后缓慢增长
            return 36 + (ubbSize - 13) * 0.5;
        }

        return 14; // 默认值
    }
}

// 链接渲染策略
public class UrlRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var hyperlink = new Hyperlink();
            var url = tagNode.GetAttribute("value");
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    // 使用RelativeOrAbsolute，允许相对路径
                    hyperlink.NavigateUri = new Uri(url, UriKind.RelativeOrAbsolute);
                }
                catch { }
            }
            // 设置样式
            hyperlink.Foreground = new SolidColorBrush(Colors.Blue);
            hyperlink.TextDecorations = TextDecorations.Underline;
            // 点击事件
            hyperlink.Click += (sender, e) =>
            {
                if (hyperlink.NavigateUri != null)
                {
                    // TODO: 打开链接
                }
            };
            context.BeginInlineContainer(hyperlink);
            foreach (var child in node.Children)
            {
                context.RenderNode(child);
            }
            context.EndInlineContainer();
        }
    }
}

// 图片渲染策略
public class ImageRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var src = tagNode.GetAttribute("src");
            if (string.IsNullOrEmpty(src))
            {
                // 尝试从子节点获取URL（对于 [img]url[/img] 格式）
                foreach (var child in node.Children)
                {
                    if (child is TextNode textNode)
                    {
                        src = textNode.Content;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(src))
            {
                var image = new Image
                {
                    MaxWidth = (double)context.Properties["ImageMaxWidth"],
                    Stretch = Stretch.Uniform,
                    
                };
                var hyperlinkButton = new HyperlinkButton
                {
                    Content = image,
                    Padding = new Thickness(1),
                    Background = new SolidColorBrush(Colors.Transparent),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    HorizontalContentAlignment=HorizontalAlignment.Stretch
                };
                // 异步加载图片
                LoadImageAsync(image, src);
                context.AddToContainer(hyperlinkButton);
            }
        }
    }

    private async void LoadImageAsync(Image image, string src)
    {
        try
        {
            if (src.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var bitmapImage = new BitmapImage(new Uri(src));
                image.Source = bitmapImage;
            }
            else
            {
                // 加载本地图片
                var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(src);
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                await bitmapImage.SetSourceAsync(stream);
                image.Source = bitmapImage;
            }
        }
        catch
        {
            // 加载失败时显示占位符
            image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/ImageError.png"));
        }
    }
}

// 代码块渲染策略
public class CodeRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        var border = new Border
        {
            Background = (Brush)context.Properties["CodeBackground"],
            Padding = new Thickness(12),
            Margin = new Thickness(4, 2, 2, 0),
            CornerRadius = new CornerRadius(4)
        };

        var grid = new Grid();

        // 定义行：第一行用于显示语言标签，第二行用于代码
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 创建语言标签容器
        var languageContainer = new Grid
        {
            Margin = new Thickness(0, 0, 0, 0),
        };

        // 获取语言名称
        var languageName = node is TagNode tagNode ? tagNode.GetAttribute("language") : "cpp";
        if (string.IsNullOrEmpty(languageName))
            languageName = "cpp";

        // 获取友好的语言显示名称
        var language = ColorCode.Languages.FindById(languageName) ?? ColorCode.Languages.Cpp;
        var displayName = GetLanguageDisplayName(language, languageName);

        // 创建语言标签
        var languageTag = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x7A, 0xCC)), // 半透明蓝色
            Padding = new Thickness(8, 4, 8, 4),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var languageText = new TextBlock
        {
            Text = displayName,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC)), // 蓝色文字
            VerticalAlignment=VerticalAlignment.Center
        };

        languageTag.Child = languageText;
        languageContainer.Children.Add(languageTag);

        // 添加到 Grid 的第一行
        Grid.SetRow(languageContainer, 0);
        grid.Children.Add(languageContainer);

        // 2. 创建代码区域
        var codeContainer = new Grid();
        Grid.SetRow(codeContainer, 1);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var textBlock = new RichTextBlock
        {
            FontFamily = new FontFamily("Consolas, 'Cascadia Code', monospace"),
            FontSize = (double)context.Properties["FontSize"] * 0.9,
            IsTextSelectionEnabled = true
        };

        var codeText = CollectText(node);
        var formatter = new RichTextBlockFormatter();
        formatter.FormatRichTextBlock(codeText, language, textBlock);
        AddCopyButton(languageContainer, codeText);
        scrollViewer.Content = textBlock;
        codeContainer.Children.Add(scrollViewer);

        grid.Children.Add(codeContainer);

        border.Child = grid;
        context.AddToContainer(border);
    }

    private string CollectText(UbbNode node)
    {
        var text = "";
        foreach (var child in node.Children)
        {
            if (child is TextNode textNode)
            {
                text += textNode.Content;
            }
            else
            {
                text += CollectText(child);
            }
        }
        return text;
    }
    private string GetLanguageDisplayName(ColorCode.ILanguage language, string languageName)
    {
        // 如果语言对象有 Name 属性，使用它
        if (!string.IsNullOrEmpty(language.Name))
            return language.Name;

        // 否则根据语言 ID 返回友好名称
        return languageName.ToLowerInvariant() switch
        {
            "cpp" => "C++",
            "csharp" or "cs" => "C#",
            "javascript" or "js" => "JavaScript",
            "typescript" or "ts" => "TypeScript",
            "python" or "py" => "Python",
            "java" => "Java",
            "html" => "HTML",
            "css" => "CSS",
            "sql" => "SQL",
            "php" => "PHP",
            "ruby" or "rb" => "Ruby",
            "go" => "Go",
            "rust" => "Rust",
            "swift" => "Swift",
            "kotlin" => "Kotlin",
            "dart" => "Dart",
            "markdown" or "md" => "Markdown",
            "yaml" => "YAML",
            "json" => "JSON",
            "xml" => "XML",
            "bash" or "sh" => "Bash",
            "powershell" or "ps" => "PowerShell",
            "plaintext" or "text" => "Plain Text",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(languageName)
        };
    }
    private void AddCopyButton(Grid languageContainer, string codeText)
    {
        var copyButton = new Button
        {
            Content = new FluentIcons.WinUI.SymbolIcon { Symbol=FluentIcons.Common.Symbol.Copy,FontSize=16},
            Margin = new Thickness(0, 0, 8, 0),
            Padding=new Thickness(0,0,0,0),
            BorderThickness = new Thickness(1),
            Width = 32,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        copyButton.Click += (sender, e) =>
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(codeText);
            Clipboard.SetContent(dataPackage);

            // 可选：显示复制成功的提示
            ShowCopySuccessMessage(copyButton);
        };

        languageContainer.Children.Add(copyButton);
    }
    private void ShowCopySuccessMessage(Button copyButton)
    {
        var originalContent = copyButton.Content;

        copyButton.Content = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.Checkmark, FontSize = 16 };
        copyButton.Foreground = new SolidColorBrush(Colors.Green);

        // 2秒后恢复
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (s, e) =>
        {
            copyButton.Content = originalContent;
            copyButton.Foreground = null;
            timer.Stop();
        };
        timer.Start();
    }
}

// 引用渲染策略
public class QuoteRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();

        // 增加引用块嵌套层级
        int originalQuoteLevel = context.QuoteNestingLevel;
        context.QuoteNestingLevel++;
        bool isOutermostQuote = (context.QuoteNestingLevel == 1);

        var border = CreateQuoteBorder(context,isOutermostQuote);
        var contentPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0)
        };

        // 添加作者信息（如果有）
        AddAuthorInfo(node, contentPanel, context);

        // 保存当前容器状态以便恢复
        var previousContainer = context.Container;
        var previousPanelStack = context.PanelStack != null ?
            new Stack<Panel>(context.PanelStack) : new Stack<Panel>();

        // 切换到新的内容容器
        context.Container = contentPanel;
        if (context.PanelStack != null)
        {
            context.PanelStack.Clear();
            context.PanelStack.Push(contentPanel);
        }

        // 渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }

        // 确保最后的文本块被结束
        context.FinalizeCurrentTextBlock();

        // 恢复之前的容器状态
        context.Container = previousContainer;
        if (context.PanelStack != null)
        {
            context.PanelStack.Clear();
            foreach (var panel in previousPanelStack.Reverse())
            {
                context.PanelStack.Push(panel);
            }
        }

        // 将内容面板添加到边框
        border.Child = contentPanel;

        // 将整个引用块添加到容器
        context.AddToContainer(border);
    }
    private Border CreateQuoteBorder(RenderContext context, bool isOutermostQuote)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 196, 174)),
            BorderThickness = new Thickness(2, 0, 0, 0),
            Padding = new Thickness(6, 6, 6, 6),
        };
        // 只有最外层引用块有背景色
        if (isOutermostQuote)
        {
            border.Background = (Brush)context.Properties["QuoteBackground"] ?? new SolidColorBrush(Color.FromArgb(255, 232, 244, 249));
            border.Margin = new Thickness(4, 6, 4, 6);
        }
        else
        {
            border.Background = new SolidColorBrush(Colors.Transparent);
            border.BorderThickness = new Thickness(1, 0, 0, 0);
            border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 220, 176, 154));
            border.Margin = new Thickness(2, 2, 0, 2); // 内层缩进
        }
        return border;
    }

    private void AddAuthorInfo(UbbNode node, StackPanel contentPanel, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var author = tagNode.GetAttribute("value");
            if (!string.IsNullOrEmpty(author))
            {
                var authorPanel = CreateAuthorPanel(author, context);
                contentPanel.Children.Add(authorPanel);
            }
        }
    }

    private StackPanel CreateAuthorPanel(string author, RenderContext context)
    {
        var authorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 4)
        };

        // 作者图标
        var icon = new TextBlock
        {
            Text = "💬",
            FontSize = (double)context.Properties["FontSize"],
            VerticalAlignment = VerticalAlignment.Center
        };

        // 作者文本
        var authorText = new TextBlock
        {
            Text = $"{author} 说：",
            FontWeight = FontWeights.SemiBold,
            FontSize = (double)context.Properties["FontSize"],
            Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)),
            VerticalAlignment = VerticalAlignment.Center
        };

        authorPanel.Children.Add(icon);
        authorPanel.Children.Add(authorText);

        return authorPanel;
    }
}

// 段落渲染策略
public class ParagraphRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();

    }
}

// 换行渲染策略
public class LineBreakRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        // 使用空的Run而不是LineBreak
        // 这样不会创建新的行，只是添加一个换行符
        var run = new LineBreak();
        context.AddInline(run);
    }
}

// 对齐渲染策略
public class AlignRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        if (node is TagNode tagNode)
        {
            var align = tagNode.GetAttribute("value", "left").ToLower();

            // 使用 Border 作为外部容器，控制整体对齐
            var border = new Border
            {
                HorizontalAlignment = GetHorizontalAlignment(align),
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent)
            };

            // 内部仍然使用 StackPanel 或其他容器
            var innerPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // 设置内部元素的文本对齐
            switch (align)
            {
                case "center":
                    innerPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
                case "right":
                    innerPanel.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                default:
                    innerPanel.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
            }

            border.Child = innerPanel;

            // 保存当前容器
            var previousContainer = context.Container;
            context.Container = innerPanel;
            context.PanelStack.Push(innerPanel);

            // 渲染子节点
            foreach (var child in node.Children)
            {
                context.RenderNode(child);
            }

            // 恢复之前的容器
            context.Container = previousContainer;
            context.PanelStack.Pop();

            context.AddToContainer(border);
        }
    }
    private static HorizontalAlignment GetHorizontalAlignment(string align)
    {
        return align.ToLower() switch
        {
            "center" => HorizontalAlignment.Center,
            "right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
    }
}

// 左对齐渲染策略
public class LeftRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 保存当前容器
        var previousContainer = context.Container;
        context.Container = panel;
        context.PanelStack.Push(panel);

        // 渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }

        // 恢复之前的容器
        context.Container = previousContainer;
        context.PanelStack.Pop();

        context.AddToContainer(panel);
    }
}

// 居中渲染策略
public class CenterRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // 保存当前容器
        var previousContainer = context.Container;
        context.Container = panel;
        context.PanelStack.Push(panel);

        // 渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }

        // 恢复之前的容器
        context.Container = previousContainer;
        context.PanelStack.Pop();

        context.AddToContainer(panel);
    }
}

// 右对齐渲染策略
public class RightRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };

        // 保存当前容器
        var previousContainer = context.Container;
        context.Container = panel;
        context.PanelStack.Push(panel);

        // 渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }

        // 恢复之前的容器
        context.Container = previousContainer;
        context.PanelStack.Pop();

        context.AddToContainer(panel);
    }
}

public class ColorRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var colorStr = tagNode.GetAttribute("color");
            if (!string.IsNullOrEmpty(colorStr))
            {
                var span = new Span();

                // 尝试解析颜色
                try
                {
                    var color = ParseColor(colorStr);
                    span.Foreground = new SolidColorBrush(color);
                }
                catch
                {
                    // 解析失败，使用默认颜色
                }

                context.BeginInlineContainer(span);

                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }

                context.EndInlineContainer();
            }
            else
            {
                // 没有颜色值，直接渲染子节点
                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }
            }
        }
    }

    private Color ParseColor(string colorStr)
    {
        // 移除可能的#
        colorStr = colorStr.Trim().TrimStart('#');

        // 支持颜色名称
        var colorName = colorStr.ToLower();
        switch (colorName)
        {
            case "black": return Colors.Black;
            case "white": return Colors.White;
            case "red": return Colors.Red;
            case "green": return Colors.Green;
            case "blue": return Colors.Blue;
            case "gray":
            case "grey": return Colors.Gray;
            case "yellow": return Colors.Yellow;
            case "purple": return Colors.Purple;
            case "orange": return Colors.Orange;
            default:
                // 尝试解析十六进制颜色
                if (colorStr.Length == 6)
                {
                    var r = Convert.ToByte(colorStr.Substring(0, 2), 16);
                    var g = Convert.ToByte(colorStr.Substring(2, 2), 16);
                    var b = Convert.ToByte(colorStr.Substring(4, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                return Colors.Black; // 默认黑色
        }
    }
}

// 字体渲染策略
public class FontRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var fontName = tagNode.GetAttribute("value");
            if (!string.IsNullOrEmpty(fontName))
            {
                var span = new Span();

                try
                {
                    span.FontFamily = new FontFamily(fontName);
                }
                catch
                {
                    // 字体无效，使用默认字体
                }

                context.BeginInlineContainer(span);

                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }

                context.EndInlineContainer();
            }
            else
            {
                // 没有字体值，直接渲染子节点
                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }
            }
        }
    }
}