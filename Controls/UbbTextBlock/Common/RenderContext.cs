using CC98.Share.Extensions;
using Microsoft.UI;
using DevWinUI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UbbRender.Parser;
using UbbRender.Render;
using Windows.ApplicationModel.DataTransfer;
namespace UbbRender.Common;
public class RenderContext
{
    public UbbTextBlock Control { get; set; }
    public Panel Container { get; set; }
    // Use RichTextBlock to support InlineUIContainer children
    public RichTextBlock CurrentRichTextBlock { get; set; }
    private Paragraph CurrentParagraph { get; set; }

    public Stack<Panel> PanelStack { get; set; } = new Stack<Panel>();
    public Stack<Inline> InlineStack = new();
    // 临时存储当前正在构建的内联容器
    private Inline CurrentInline;
    public int QuoteNestingLevel { get; set; }
    public Dictionary<string, object> Properties { get; set; } = [];

    public void RenderNode(UbbNode node)
    {
        if (Control.renderStrategies.TryGetValue(node.Type, out var strategy))
        {
            strategy.Render(node, this);
        }
        else
        {
            //当未匹配到渲染策略时，忽略此层级并渲染子节点
            foreach (var child in node.Children)
            {
                RenderNode(child);
            }
        }
    }
    /// <summary>
    /// 向当前文本块或内联容器添加内联元素。
    /// </summary>
    /// <remarks>
    /// 此方法会续上接当前正在构建的内联容器（如 Bold、Italic 等）。
    /// </remarks>
    /// <param name="inline"></param>
    public void AddInline(Inline inline)
    {
        //如果还没有文本块，就新建一个
        if (CurrentParagraph == null || CurrentRichTextBlock == null)
        {
            StartNewTextBlock();
        }
        //如果有当前正在构建的内联容器，根据容器类型添加到其中
        if (CurrentInline != null)
        {
            if (CurrentInline is Span span)
            {
                span.Inlines.Add(inline);
            }
            else if (CurrentInline is Bold bold)
            {
                bold.Inlines.Add(inline);
            }
            else if (CurrentInline is Italic italic)
            {
                italic.Inlines.Add(inline);
            }
            else if (CurrentInline is Underline underline)
            {
                underline.Inlines.Add(inline);
            }
            else if (CurrentInline is Hyperlink hyperlink)
            {
                hyperlink.Inlines.Add(inline);
            }

        }
        else
        {
            // 否则添加到当前段落
            CurrentParagraph?.Inlines.Add(inline);
        }
    }
    /// <summary>
    /// 开始构建新的内联容器（如 Bold、Italic 等）
    /// </summary>
    /// <param name="container"></param>
    public void BeginInlineContainer(Inline container)
    {
        if (CurrentParagraph == null || CurrentRichTextBlock == null)
        {
            StartNewTextBlock();
        }
        if (CurrentInline != null)
        {
            InlineStack.Push(CurrentInline);
        }
        CurrentInline = container;
    }

    /// <summary>
    ///结束当前内联容器的构建
    /// </summary>
    public void EndInlineContainer()
    {
        if (CurrentInline == null)
        {
            return;
        }
        var completedInline = CurrentInline;
        if (InlineStack.Count >0)
        {
            // 从栈中取出父容器
            var parentContainer = InlineStack.Pop();

            // 将当前容器添加到父容器
            if (parentContainer is Span parentSpan)
            {
                parentSpan.Inlines.Add(completedInline);
                CurrentInline = parentSpan;
            }
            else if (parentContainer is Bold parentBold)
            {
                parentBold.Inlines.Add(completedInline);
                CurrentInline = parentBold;
            }
            else if (parentContainer is Italic parentItalic)
            {
                parentItalic.Inlines.Add(completedInline);
                CurrentInline = parentItalic;
            }
            //其他容器类型
            else if (parentContainer is Underline parentUnderline)
            {
                parentUnderline.Inlines.Add(completedInline);
                CurrentInline = parentUnderline;
            }
            else if (parentContainer is Hyperlink parentHyperlink)
            {
                parentHyperlink.Inlines.Add(completedInline);
                CurrentInline = parentHyperlink;
            }
        }
        else
        {
            // 如果没有父容器，添加到当前段落
            if (CurrentParagraph == null || CurrentRichTextBlock == null)
            {
                //直接创建新的 RichTextBlock，避免循环调用 StartNewTextBlock()
                CurrentRichTextBlock = new RichTextBlock
                {
                    FontSize = Control.FontSize,
                    TextWrapping = TextWrapping.Wrap
                };
                CurrentParagraph = new Paragraph();
                CurrentRichTextBlock.Blocks.Add(CurrentParagraph);
            }
            CurrentParagraph?.Inlines.Add(completedInline);
            CurrentInline = null;
        }
    }


    public void AddToContainer(UIElement element)
    {
        while (CurrentInline != null)
        {
            EndInlineContainer();
        }
        FinalizeCurrentTextBlock();
        Container.Children.Add(element);
    }
    //结束当前文本块的构建
    public void FinalizeCurrentTextBlock()
    {
        while (CurrentInline != null)
        {
            EndInlineContainer();
        }
        if (CurrentRichTextBlock != null && CurrentParagraph != null && CurrentParagraph.Inlines.Count != 0)
        {
            Container.Children.Add(CurrentRichTextBlock);
            CurrentRichTextBlock = null;
            CurrentParagraph = null;
        }
        //清理栈
        InlineStack.Clear();
        CurrentInline = null;
    }
    
    public void StartNewTextBlock()
    {
        FinalizeCurrentTextBlock();

        CurrentRichTextBlock = new RichTextBlock
        {
            FontSize = Control.FontSize,
            Foreground = Control.Foreground ?? new SolidColorBrush(Colors.Black),
            TextWrapping = TextWrapping.Wrap,
            ContextFlyout= CreateCustomContextMenu()
        };

        CurrentParagraph = new Paragraph();
        CurrentRichTextBlock.Blocks.Add(CurrentParagraph);
    }
    #region 自定义右键菜单
    private MenuFlyout CreateCustomContextMenu()
    {
        var menuFlyout = new MenuFlyout();

        // 添加复制菜单项
        var copyItem = new AppBarButton
        {
            Label = "复制",
            Icon = new SymbolIcon(Symbol.Copy)
        };
        var separator1 = new AppBarSeparator();
        var selectAllItem = new AppBarButton 
        {
            Label="全选",
            Icon = new SymbolIcon(Symbol.SelectAll)
        };
        var separator2 = new AppBarSeparator();
        var searchItem = new AppBarButton
        {
            Label = "搜索",
            Icon = new FluentIcons.WinUI.SymbolIcon() { Symbol= FluentIcons.Common.Symbol.GlobeSearch }
        };
        var browseItem = new MenuFlyoutItem
        {
            Text="前往此页面",
            Icon= new FluentIcons.WinUI.SymbolIcon() { Symbol = FluentIcons.Common.Symbol.WindowNew }
        };
        
        
        MenuFlyoutAttach.SetAutoCloseByClickOnSecondaryMenuItems(menuFlyout, true);

        // 设置附加属性 SecondaryMenuPlacement
        MenuFlyoutAttach.SetSecondaryMenuPlacement(menuFlyout, MenuFlyoutSecondaryMenuPlacement.Top);
        var secondaryItems = new MenuFlyoutSecondaryItems();
        secondaryItems.Items.Add(copyItem);
        secondaryItems.Items.Add(separator1);
        secondaryItems.Items.Add(selectAllItem);
        secondaryItems.Items.Add(separator2);
        secondaryItems.Items.Add(searchItem);
        MenuFlyoutAttach.SetSecondaryMenu(menuFlyout, secondaryItems);
        menuFlyout.Items.Add(browseItem);
        copyItem.Click += OnCopyClicked;
        selectAllItem.Click += SelectAllItem_Click;
        return menuFlyout;
    }

    private void SelectAllItem_Click(object sender, RoutedEventArgs e)
    {
        SelectAll();
    }

    private void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        string selectedText = GetSelectedText();
        if (!string.IsNullOrEmpty(selectedText))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(selectedText);
            Clipboard.SetContent(dataPackage);
        }
        
    }
    private string GetSelectedText()
    {
        var selectedText = new StringBuilder();

        // 检查当前正在编辑的 RichTextBlock
        if (CurrentRichTextBlock != null && !string.IsNullOrEmpty(CurrentRichTextBlock.SelectedText))
        {
            selectedText.Append(CurrentRichTextBlock.SelectedText);
        }

        // 检查容器中已完成的 RichTextBlock
        if (Container != null)
        {
            foreach (var child in Container.Children)
            {
                if (child is RichTextBlock richTextBlock && richTextBlock != CurrentRichTextBlock)
                {
                    if (!string.IsNullOrEmpty(richTextBlock.SelectedText))
                    {
                        if (selectedText.Length > 0)
                        {
                            selectedText.AppendLine(); // 不同块之间添加换行
                        }
                        selectedText.Append(richTextBlock.SelectedText);
                    }
                }
            }
        }

        return selectedText.ToString();
    }
    private void SelectAll()
    {
        if (Container == null) return;

        // 对每个 RichTextBlock 执行全选
        foreach (var child in Container.Children)
        {
            if (child is RichTextBlock richTextBlock)
            {
                richTextBlock.SelectAll();
            }
        }
    }

    /// <summary>
    /// 获取当前选中的文本（对外暴露的方法）
    /// </summary>
    public string GetSelectedTextExternal()
    {
        return GetSelectedText();
    }

    /// <summary>
    /// 全选所有文本（对外暴露的方法）
    /// </summary>
    public void SelectAllExternal()
    {
        SelectAll();
    }
    #endregion
}