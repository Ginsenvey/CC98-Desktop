using CC98.Share.Controls;
using CC98.Share.Controls.Primitives;
using CC98.Share.Controls.Primitives.LatexBlock;
using CC98.Share.Extensions;
using ColorCode;
using CommunityToolkit.WinUI.UI.Controls;
using DevWinUI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UbbRender.Parser;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Text;
using static System.Net.WebRequestMethods;

namespace UbbRender.Common;


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
            if (node.Parent.Type == UbbNodeType.Document)
            {
                var prevIsBlock = node.PreviousSibling?.Type.IsBlock() ?? false;
                var nextIsBlock = node.NextSibling?.Type.IsBlock() ?? false;

                if (prevIsBlock)
                {
                    content = content.TrimStart('\r', '\n');
                }
                if (nextIsBlock)
                {
                    content = content.TrimEnd('\r', '\n');
                }
            }
            var run = new Run { Text = content };
            context.AddInline(run);
        }
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
            
            var sizeStr = tagNode.GetAttribute("size");
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
            var url = tagNode.GetAttribute("href");
            if (!url.IsValidUrl()&&node.Children.Count>0)
            {
                var child = node.Children[0] as TextNode;
                if (child != null) url = child.Content;
            }
            // 设置样式
            hyperlink.Foreground = new SolidColorBrush(Colors.LightSeaGreen);
            hyperlink.TextDecorations = TextDecorations.Underline;
            // 点击事件
            hyperlink.Click += (sender, e) =>
            {
                context.Control.OnMediaClicked(url, MediaType.Link);
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
            string src = "";
            var value = tagNode.GetAttribute("value");
            if (string.IsNullOrEmpty(value) || value == "1")
            {
                // 尝试从子节点获取URL（对于 [img]url[/img] 格式）
                var first=node.FirstChild;
                if (first is TextNode textNode)
                {
                    src = textNode.Content;
                }
            }
            else
            {
                src = value;
            }
            if (!string.IsNullOrEmpty(src))
            {
                var image = new Picture()
                {
                    //自定义图片加载器必须在Src设置之前赋值
                    //否则OnSrcChanged事件会触发，但此时将使用默认加载器
                    LoadImageCallback = new SmartImageLoader(),
                    //UBB隐藏图片语法
                    Hide= value=="1",
                    Src=src,
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
                hyperlinkButton.Click += (s, e) =>
                { 
                    context.Control.OnMediaClicked(src,MediaType.Image);
                };

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
        var languageName = node is TagNode tagNode ? tagNode.GetAttribute("language") : "PlainText";
        var viewer = new CodeBlock
        {
            LanguageName = languageName,
            Code = RenderHelper.CollectText(node),
            Background= (Brush)context.Properties["CodeBackground"],
            Padding =new Thickness(12),
            Margin=new Thickness(4,2,4,2),
            BorderThickness=new Thickness(1),
            CornerRadius=new CornerRadius(4),
        };
        context.AddToContainer(viewer);
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
            border.BorderThickness = new Thickness(2, 0, 0, 0);
            border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 220, 176, 154));
            border.Margin = new Thickness(2, 2, 0, 2); // 内层缩进
        }
        return border;
    }

    private void AddAuthorInfo(UbbNode node, StackPanel contentPanel, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var author = tagNode.GetAttribute("author");
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



// 对齐渲染策略
public class AlignRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        if (node is TagNode tagNode)
        {
            var align = tagNode.GetAttribute("value", "left").ToLower();
            RenderHelper.ApplyAlignToContext(align,node,context);
        }
    }
}

// 左对齐渲染策略
public class LeftRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        string align = "left";
        RenderHelper.ApplyAlignToContext(align, node, context);
    }
}

// 居中渲染策略
public class CenterRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        string align = "center";
        RenderHelper.ApplyAlignToContext(align, node, context);
    }
}

// 右对齐渲染策略
public class RightRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        string align = "right";
        RenderHelper.ApplyAlignToContext(align, node, context);
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
            case "blue": return Color.FromArgb(255,142,130,254);
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
            var fontName = tagNode.GetAttribute("font");
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
public class EmojiRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        // 不要FinalizeCurrentTextBlock(),保持在当前文本流
        if (node is TagNode tagNode)
        {
            var emoticonCode = tagNode.GetAttribute("code");
            if (!string.IsNullOrEmpty(emoticonCode))
            {
                var imageUrl = GetEmoticonUrl(emoticonCode);
                try
                {
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var image = new Image
                        {
                            Source = LoadImageFromUrl(imageUrl),
                            Margin = new Thickness(4, 0, 4, 0),
                            MaxWidth =32,
                            MaxHeight=32,
                            Stretch=Stretch.UniformToFill
                        };
                        var inlineContainer = new InlineUIContainer { Child = image };
                        context.AddInline(inlineContainer);
                    }
                    else
                    {
                        var run = new Run { Text = $"[{emoticonCode}]" };
                        context.AddInline(run);
                    }
                }
                catch(Exception ex)
                {
                    var run = new Run { Text = $"[{ex.Message}]" };
                    context.AddInline(run);
                }
            }
        }
    }
    private string GetEmoticonUrl(string code)
    {
        return EmoticonRules.GetEmoticonUrl(code);
    }
    private ImageSource LoadImageFromUrl(string url)
    {
        try
        {
            var bitmapImage = new BitmapImage(new Uri(url));
            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

}

public class LatexRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if(node is LatexNode latexNode)
        {
            var codeText = latexNode.Latex;
            context.AddInline(InlineLatex(codeText));
        }
        else
        {
            context.FinalizeCurrentTextBlock();
            var codeText = RenderHelper.CollectText(node);
            var block = BlockLatex(codeText);
            context.AddToContainer(block);
        }
        
        
    }
    private Inline InlineLatex(string latex)
    {
        var textBlock = new LatexBlock
        {
            FontSize = 14,
        };
        textBlock.LaTeX = latex;

        var container=new InlineUIContainer { Child=textBlock };
        return container;
    }
    private UIElement BlockLatex(string latex)
    {
         
        var textBlock = new LatexBlock
        {
            FontSize = 14,
        };
        textBlock.LaTeX = latex;
        return textBlock;
    }
}
public class DividerRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var border = new Border();
        var line= new Line
        {
            X1 = 0,
            Y1 = 0,
            X2 = 1,  // 相对坐标，Stretch.Fill会处理
            Y2 = 0,
            Stroke = new SolidColorBrush(Colors.Gray),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 2, 2 },
            Stretch = Stretch.Fill,  // 自动拉伸
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(5,10,5,10)
        };
        border.Child = line;
        context.AddToContainer(border);
    }
}

public class MarkdownRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var textBlock = new MarkdownTextBlock
        {
            FontSize = 14,
            Background= new SolidColorBrush(Colors.Transparent),
        };

        var codeText = RenderHelper.CollectText(node);
        textBlock.Text = codeText;
        
        scrollViewer.Content = textBlock;
        context.AddToContainer(scrollViewer);
    }
}
public class RenderHelper
{
    public static void ApplyAlignToContext(string align,UbbNode node, RenderContext context)
    {
        //使用Grid进行对齐控制
        var panel = new Grid();

        switch (align)
        {
            case "center":
                panel.HorizontalAlignment = HorizontalAlignment.Center;
                break;
            case "right":
                panel.HorizontalAlignment = HorizontalAlignment.Right;
                break;
            default:
                panel.HorizontalAlignment = HorizontalAlignment.Left;
                break;
        }

        // 保存当前容器
        var previousContainer = context.Container;
        var previousPanelStack = context.PanelStack != null ? new Stack<Panel>(context.PanelStack) : null;

        context.Container = panel;
        if (context.PanelStack == null)
        {
            context.PanelStack = new Stack<Panel>();
        }
        context.PanelStack.Push(panel);

        // 渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }

        // 确保当前文本块结束
        context.FinalizeCurrentTextBlock();

        // 恢复之前的容器
        context.Container = previousContainer;
        if (previousPanelStack != null)
        {
            context.PanelStack.Clear();
            foreach (var p in previousPanelStack.Reverse())
                context.PanelStack.Push(p);
        }
        else
        {
            // 如果之前没有 PanelStack，则清空当前栈
            context.PanelStack.Clear();
        }

        // 将 panel 添加回之前的容器
        context.AddToContainer(panel);
    }


    public static string CollectText(UbbNode node)
    {
        var sb = new StringBuilder();
        CollectTextRecursive(node, sb);
        return sb.ToString();
    }

    private static void CollectTextRecursive(UbbNode node, StringBuilder sb)
    {
        foreach (var child in node.Children)
        {
            if (child is TextNode textNode)
            {
                sb.Append(textNode.Content);
            }
            else
            {
                CollectTextRecursive(child, sb);
            }
        }
    }
}

public class FlatQuoteRenderStrategy : IRenderStrategy
{
    private const int MaxVisibleDepth = 2;

    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();

        // 提取引用链（所有层级）
        var quoteChain = ExtractQuoteChain(node);

        if (quoteChain.Count == 0) return;

        // 创建主容器
        var mainContainer = CreateQuoteContainer(context, quoteChain);

        // 添加到渲染上下文
        context.AddToContainer(mainContainer);
    }

    // 提取所有引用层级
    private List<UbbNode> ExtractQuoteChain(UbbNode node)
    {
        var chain = new List<UbbNode>();
        ExtractAllQuotes(node, chain);
        return chain;
    }

    private void ExtractAllQuotes(UbbNode node, List<UbbNode> chain)
    {
        if (node.Type == UbbNodeType.Quote)
        {
            chain.Add(node);

            // 查找所有直接子引用
            foreach (var child in node.Children)
            {
                if (child.Type == UbbNodeType.Quote)
                {
                    ExtractAllQuotes(child, chain);
                }
            }
        }
    }

    // 创建引用容器
    private FrameworkElement CreateQuoteContainer(RenderContext context, List<UbbNode> quoteChain)
    {
        // 判断是否需要折叠
        bool needCollapse = quoteChain.Count > MaxVisibleDepth;
        int visibleCount = Math.Min(quoteChain.Count, MaxVisibleDepth);

        var container = new StackPanel
        {
            Spacing = 0,
            Margin = new Thickness(0)
        };

        // 如果需要折叠
        if (needCollapse)
        {
            // 创建折叠部分（第3层及以后）
            var collapsedSection = CreateCollapsedSection(
                quoteChain.Skip(MaxVisibleDepth).ToList(),
                context
            );
            container.Children.Add(collapsedSection);

            // 在按钮和下方内容之间添加分割线
            container.Children.Add(CreateSeparator());
        }

        // 添加可见部分（前两层）
        for (int i = visibleCount - 1; i >= 0; i--)
        {
            // 渲染单个引用
            var quoteContainer = RenderSingleQuote(quoteChain[i], context, i == 0);
            container.Children.Add(quoteContainer);

            // 在引用之间添加分割线（除了最后一个）
            if (i > 0)
            {
                container.Children.Add(CreateSeparator());
            }
        }

        // 添加外部边框
        return new Border
        {
            Background = (Brush)context.Properties["QuoteBackground"] ?? new SolidColorBrush(Color.FromArgb(255, 232, 244, 249)),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 8, 0, 8),
            CornerRadius = new CornerRadius(4),
            Child = container
        };
    }

    // 创建折叠部分
    private FrameworkElement CreateCollapsedSection(List<UbbNode> collapsedQuotes, RenderContext context)
    {
        var section = new StackPanel
        {
            Spacing = 4
        };

        // 创建展开按钮
        var expandButton = new ToggleButton
        {
            Content = $"展开{collapsedQuotes.Count}条引用",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(12, 6, 12, 6),
            FontSize = 12,
            CornerRadius = new CornerRadius(4),
            IsChecked = false
        };

        // 创建折叠内容容器
        var collapsedContent = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // 添加折叠的引用（按从深到浅的顺序）
        for (int i = collapsedQuotes.Count - 1; i >= 0; i--)
        {
            var quoteContainer = RenderSingleQuote(collapsedQuotes[i], context, false);
            collapsedContent.Children.Add(quoteContainer);

            // 在折叠的引用之间也添加分割线
            if (i > 0)
            {
                collapsedContent.Children.Add(CreateSeparator());
            }
        }

        // 按钮点击事件
        expandButton.Click += (sender, e) =>
        {
            ToggleCollapsedContent(expandButton, collapsedContent, collapsedQuotes.Count, context.Control);
        };

        section.Children.Add(expandButton);
        section.Children.Add(collapsedContent);

        return section;
    }

    // 切换折叠状态
    private void ToggleCollapsedContent(ToggleButton button, StackPanel content, int quoteCount, Control control)
    {
        if (content.Visibility == Visibility.Collapsed)
        {
            button.IsChecked = true;
            content.Visibility = Visibility.Visible;
            button.Content = "收起引用";
        }
        else
        {
            button.IsChecked = false;
            content.Visibility = Visibility.Collapsed;
            button.Content = $"展开{quoteCount}条引用";
        }
    }


    // 创建分割线
    private Border CreateSeparator()
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00)),
            Margin = new Thickness(0, 5, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    // 渲染单个引用
    private FrameworkElement RenderSingleQuote(UbbNode quoteNode, RenderContext context, bool isFirstLevel)
    {
        var contentPanel = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0)
        };

        // 创建临时渲染上下文，确保复制 Control
        var tempContext = new RenderContext
        {
            Control = context.Control, // 关键：复制 Control 引用
            Container = contentPanel,
            Properties = context.Properties,
            PanelStack = new Stack<Panel>(),
            QuoteNestingLevel = context.QuoteNestingLevel
        };

        // 渲染引用内容（跳过嵌套的引用，因为它们已经被提取出来了）
        RenderQuoteContent(quoteNode, tempContext);

        // 添加左侧边框作为视觉指示
        return new Border
        {
            Background = new SolidColorBrush(Colors.Transparent),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 2, 0, 2),
            Child = contentPanel
        };
    }

    // 渲染引用内容（跳过嵌套引用）
    private void RenderQuoteContent(UbbNode node, RenderContext context)
    {
        foreach (var child in node.Children)
        {
            // 跳过嵌套的引用节点（它们已经被提取出来单独渲染）
            if (child.Type != UbbNodeType.Quote)
            {
                context.RenderNode(child);
            }
        }

        context.FinalizeCurrentTextBlock();
    }
}
public class AudioRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var src = tagNode.GetAttribute("src");
            if (!src.IsValidUrl())
            {
                // 尝试从子节点获取URL
                foreach (var child in node.Children)
                {
                    if (child is TextNode textNode)
                    {
                        src = textNode.Content;
                        break;
                    }
                }
            }

            if (src.IsValidUrl())
            {
                var player = new MusicPlayer
                {
                    Title = "音频",
                    LoadMediaCallback=new SmartMediaLoader(),
                    Src = src,
                    Margin = new Thickness(10),
                };
                player.DownloadStarted += (s, e) =>
                {
                    context.Control.OnMediaClicked(src, MediaType.Audio);
                };
                context.AddToContainer(player);
            }
        }
    }
}

public class VideoRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var src = tagNode.GetAttribute("src");
            if (string.IsNullOrEmpty(src))
            {
                // 尝试从子节点获取URL
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
                var player = new VideoPlayer
                {
                    LoadVideoCallback = new SmartMediaLoader(),
                    Src = src,
                    MaxHeight = 400,
                    Margin = new Thickness(10),
                    AutoPlay = false,
                };

                context.AddToContainer(player);
            }
        }
    }
}

public class TableRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();

        if (node is TagNode tagNode)
        {
            // 解析表格属性（边框、宽度、对齐等）
            var border = tagNode.GetAttribute("border", "1");
            var width = tagNode.GetAttribute("width", "auto");
            var align = tagNode.GetAttribute("align", "left");

            // 创建 Grid 作为表格容器
            var grid = CreateTableGrid(node, context);

            // 设置表格样式
            ApplyTableStyle(grid, border, width, align, context);

            context.AddToContainer(grid);
        }
    }

    private Grid CreateTableGrid(UbbNode tableNode, RenderContext context)
    {
        // 解析表格结构
        var rows = ExtractRows(tableNode);
        var columns = GetMaxColumns(rows);

        if (rows.Count == 0 || columns == 0)
            return new Grid();

        var grid = new Grid();

        // 添加列定义
        for (int i = 0; i < columns; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star) // 默认等宽
            });
        }

        // 添加行定义并填充内容
        for (int i = 0; i < rows.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var cells = rows[i];
            for (int j = 0; j < cells.Count; j++)
            {
                var cell = cells[j];
                var cellContent = RenderCellContent(cell, context);

                Grid.SetRow(cellContent, i);
                Grid.SetColumn(cellContent, j);
                grid.Children.Add(cellContent);
            }
        }

        return grid;
    }

    private List<List<UbbNode>> ExtractRows(UbbNode tableNode)
    {
        var rows = new List<List<UbbNode>>();

        foreach (var child in tableNode.Children)
        {
            if (child.Type == UbbNodeType.TableRow)
            {
                var cells = new List<UbbNode>();
                foreach (var cell in child.Children)
                {
                    if (cell.Type == UbbNodeType.TableCell)
                    {
                        cells.Add(cell);
                    }
                }
                rows.Add(cells);
            }
        }

        return rows;
    }

    private int GetMaxColumns(List<List<UbbNode>> rows)
    {
        int max = 0;
        foreach (var row in rows)
        {
            max = Math.Max(max, row.Count);
        }
        return max;
    }

    private FrameworkElement RenderCellContent(UbbNode cellNode, RenderContext context)
    {
        var container = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 4, 8, 4)
        };

        var contentPanel = new StackPanel();
        container.Child = contentPanel;

        // 创建临时上下文渲染单元格内容
        var tempContext = new RenderContext
        {
            Control = context.Control,
            Container = contentPanel,
            Properties = context.Properties,
            PanelStack = new Stack<Panel>()
        };

        foreach (var child in cellNode.Children)
        {
            tempContext.RenderNode(child);
        }
        tempContext.FinalizeCurrentTextBlock();

        return container;
    }

    private void ApplyTableStyle(Grid grid, string border, string width, string align, RenderContext context)
    {
        // 设置表格宽度
        if (width != "auto" && width.EndsWith("%"))
        {
            if (double.TryParse(width.TrimEnd('%'), out double percent))
            {
                grid.Width = context.Container?.ActualWidth * percent / 100 ?? 0;
            }
        }

        // 设置对齐方式
        switch (align.ToLower())
        {
            case "center":
                grid.HorizontalAlignment = HorizontalAlignment.Center;
                break;
            case "right":
                grid.HorizontalAlignment = HorizontalAlignment.Right;
                break;
            default:
                grid.HorizontalAlignment = HorizontalAlignment.Left;
                break;
        }

        // 设置边框（Grid 本身不显示边框，边框在单元格上）
        grid.Margin = new Thickness(0, 8, 0, 8);

        // 如果有需要，可以为整个表格添加外边框
        if (border != "0")
        {
            // 已经在单元格上设置了边框
        }
    }
}

public class TableRowRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        // TableRow 由 TableRenderStrategy 统一处理
        // 这里不需要做任何事情，因为表格行是在表格上下文中整体渲染的
        // 如果独立出现，则渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
    }
}

public class TableCellRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        // TableCell 由 TableRenderStrategy 统一处理
        // 如果独立出现，则渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
    }
}