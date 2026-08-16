using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CC98.Controls.UbbTextBlock.Parser;

namespace CC98.Services.Helpers;

/// <summary>
///     基于 UBB AST 解析器的 UBB→Markdown 转换器。
///     相比旧的正则实现(<see cref="UbbToMd"/>),基于 AST 能正确处理嵌套标签。
///     转换效果与旧实现保持一致。
/// </summary>
public static class UbbToMarkdown
{
    public static string Convert(string ubbText, bool isImageVisible, bool escapeMarkdown = false)
    {
        if (string.IsNullOrEmpty(ubbText)) return string.Empty;

        var doc = Parser.Parse(ubbText);
        // TrimStart 只去除开头冗余空白(旧版 ConvertQuotes 引入),保留 quote/块级节点尾部的结构性换行
        var result = ConvertChildren(doc.Root, isImageVisible, 0).TrimStart();
        // list 兼容:AST 解析器不支持 [list](回退为文本),在完整结果上做转换,保持与旧版一致
        result = ConvertListFallback(result);
        return escapeMarkdown ? EscapeMarkdown(result) : result;
    }

    #region 递归转换

    private static string ConvertChildren(UbbNode parent, bool isImageVisible, int quoteDepth)
    {
        var sb = new StringBuilder();
        foreach (var child in parent.Children)
        {
            var converted = ConvertNode(child, isImageVisible, quoteDepth);
            if (IsBlockElement(child.Type))
            {
                // 块级节点(仅 code/table/quote/分隔线):与前文用硬换行分隔,块后补空行
                if (sb.Length > 0 && !EndsWithLineBreak(sb)) sb.Append("  \n");
                sb.Append(converted);
                if (!converted.EndsWith("\n")) sb.Append("  \n");
                sb.Append("  \n");
            }
            else
            {
                sb.Append(converted);
            }
        }

        return sb.ToString();
    }

    // 旧版转换语义中的块级元素:其余(Audio/Video/Image/Align/ReplyView等)按行内处理
    private static readonly HashSet<UbbNodeType> BlockElements =
    [
        UbbNodeType.Code,
        UbbNodeType.Table,
        UbbNodeType.Quote,
        UbbNodeType.Divider
    ];

    private static bool IsBlockElement(UbbNodeType type) => BlockElements.Contains(type);

    private static bool EndsWithLineBreak(StringBuilder sb)
    {
        return sb.Length > 0 && sb[^1] == '\n';
    }

    private static string ConvertNode(UbbNode node, bool isImageVisible, int quoteDepth)
    {
        switch (node.Type)
        {
            case UbbNodeType.Text:
                return ConvertText(((TextNode)node).Content);

            // 行内样式:按段落包裹(对齐旧版 ConvertTrimTextStyles 的分段处理)
            case UbbNodeType.Bold:
                return WrapParagraphs(ConvertChildren(node, isImageVisible, quoteDepth), "**");
            case UbbNodeType.Italic:
                return WrapParagraphs(ConvertChildren(node, isImageVisible, quoteDepth), "*");
            case UbbNodeType.Strikethrough:
                return WrapParagraphs(ConvertChildren(node, isImageVisible, quoteDepth), "~~");

            // 去掉标签,仅保留内容
            case UbbNodeType.Underline:
            case UbbNodeType.Size:
            case UbbNodeType.Font:
            case UbbNodeType.Color:
            case UbbNodeType.Align:
            case UbbNodeType.Left:
            case UbbNodeType.Center:
            case UbbNodeType.Right:
                return ConvertChildren(node, isImageVisible, quoteDepth);

            case UbbNodeType.Url:
                return ConvertUrl((TagNode)node, isImageVisible, quoteDepth);
            case UbbNodeType.Image:
                return ConvertImage((TagNode)node, isImageVisible);
            case UbbNodeType.Audio:
                return $"[#**音频**#]({GetNodeUrl(node)})";
            case UbbNodeType.Video:
                return $"[#**视频**#]({GetNodeUrl(node)})";
            case UbbNodeType.Upload:
                return $"[#**文件**#]({GetNodeUrl(node)})";
            case UbbNodeType.Bilibili:
                return $"[#**哔哩**#](https://www.bilibili.com/video/{GetNodeUrl(node)}/)";
            case UbbNodeType.Code:
                return ConvertCode(node);
            case UbbNodeType.Quote:
                return ConvertQuote(node, isImageVisible, quoteDepth);
            case UbbNodeType.Table:
                return ConvertTable(node, isImageVisible);
            case UbbNodeType.Divider:
                return "  \n  \n---";
            case UbbNodeType.Emoji:
                // 表情映射为原名,如 [ac01] 就是 [ac01]
                return $"[{((TagNode)node).GetAttribute("code")}]";
            case UbbNodeType.At:
                return $"[@ {((AtNode)node).Username} ](https://api.cc98.org/user/name/{((AtNode)node).Username})";
            case UbbNodeType.Latex:
                return ConvertLatex(node);
            // [md]/[noubb] 逐字内容:保留原标签(对齐旧版"正则不匹配=原样保留")
            case UbbNodeType.Markdown:
            case UbbNodeType.NoUbb:
                return RebuildTag(node, isImageVisible, quoteDepth);

            // 未映射的已知标签:重建原标签(与旧版"正则不匹配=保留原文"一致)
            case UbbNodeType.Topic:
            case UbbNodeType.NeedReply:
            case UbbNodeType.ReplyView:
            case UbbNodeType.PosterOnly:
                return RebuildTag(node, isImageVisible, quoteDepth);

            default:
                return ConvertChildren(node, isImageVisible, quoteDepth);
        }
    }

    private static string ConvertText(string content)
    {
        // 对齐旧版 Preprocess:换行转 Markdown 硬换行
        return content
            .Replace("\r\n", "  \n")
            .Replace("\r", "  \n")
            .Replace("\n", "  \n")
            .Replace("<br>", "  \n");
    }

    /// <summary>
    ///     [list][*]项[/list] → Markdown 无序列表。AST 未定义 List 节点,解析器将 [list] 回退为文本。
    /// </summary>
    private static string ConvertListFallback(string input)
    {
        return Regex.Replace(input,
            @"\[list\](.*?)\[/list\]",
            m =>
            {
                var items = Regex.Matches(m.Groups[1].Value, @"\[\*\]([^\[]+)");
                return string.Join("\n", items
                    .Select(x => $"* {x.Groups[1].Value.Trim()}"));
            },
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
    }

    /// <summary>
    ///     按 Markdown 硬换行分段,每段各自包裹标记(对齐旧版 ConvertTrimTextStyles 行为)。
    /// </summary>
    private static string WrapParagraphs(string content, string marker)
    {
        var paragraphs = content.Split(["  \n"], StringSplitOptions.None);
        for (var i = 0; i < paragraphs.Length; i++)
        {
            var trimmed = paragraphs[i].Trim();
            if (!string.IsNullOrEmpty(trimmed)) paragraphs[i] = $"{marker}{trimmed}{marker}";
        }

        return string.Join("  \n", paragraphs);
    }

    private static string ConvertUrl(TagNode node, bool isImageVisible, int quoteDepth)
    {
        var content = ConvertChildren(node, isImageVisible, quoteDepth);
        var href = node.GetAttribute("href");
        if (string.IsNullOrEmpty(href)) href = content;
        return $"[{content}]({href})";
    }

    private static string ConvertImage(TagNode node, bool isImageVisible)
    {
        var url = GetNodeUrl(node);
        return isImageVisible
            ? $"![#**图片**#]({url})"
            : $"[#**图片**#]({url})";
    }

    private static string ConvertCode(UbbNode node)
    {
        var content = CollectVerbatimText(node);
        return $"```\n{content.Trim()}\n```";
    }

    private static string ConvertQuote(UbbNode node, bool isImageVisible, int quoteDepth)
    {
        // 子节点内容(子引用的深度+1)
        var content = ConvertChildren(node, isImageVisible, quoteDepth + 1);
        // 内容中的换行转为引用续行;末尾硬换行使块级逻辑补空行,退出引用块
        var body = content.Replace("  \n", "  \n" + new string('>', quoteDepth + 1));
        return new string('>', quoteDepth + 1) + " " + body + "  \n";
    }

    private static string ConvertTable(UbbNode node, bool isImageVisible)
    {
        var rows = new List<List<string>>();
        foreach (var row in node.Children.Where(c => c.Type == UbbNodeType.TableRow))
        {
            var cells = new List<string>();
            foreach (var cell in row.Children.Where(c => c.Type == UbbNodeType.TableCell))
            {
                var cellText = ConvertChildren(cell, isImageVisible, 0);
                cellText = cellText.Replace("|", "\\|").Replace("  \n", " ").Replace("\n", " ");
                cells.Add(cellText);
            }

            rows.Add(cells);
        }

        if (rows.Count == 0) return string.Empty;
        var maxCols = rows.Max(r => r.Count);
        if (maxCols == 0) return string.Empty;
        foreach (var row in rows)
            while (row.Count < maxCols)
                row.Add("");

        var md = new StringBuilder();
        for (var i = 0; i < rows.Count; i++)
        {
            md.Append("| ").Append(string.Join(" | ", rows[i])).AppendLine(" |");
            if (i == 0)
            {
                md.Append("| ");
                for (var j = 0; j < maxCols; j++)
                {
                    md.Append("---");
                    if (j < maxCols - 1) md.Append(" | ");
                }

                md.AppendLine(" |");
            }
        }

        return "  \n" + md;
    }

    private static string ConvertLatex(UbbNode node)
    {
        // $...$ / $$...$$ 行内公式:原样重建; [math]...[/math] 标签形式:原样保留
        if (node is LatexNode latex)
        {
            var marker = latex.IsBlock ? "$$" : "$";
            return marker + latex.Latex + marker;
        }

        if (node is TagNode mathTag)
        {
            var content = CollectVerbatimText(node);
            return $"[math]{content}[/math]";
        }

        return ConvertChildren(node, true, 0);
    }

    private static string ConvertChildren(UbbNode parent, bool isImageVisible)
    {
        return ConvertChildren(parent, isImageVisible, 0);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     重建未映射标签的原文,如 [topic]...[/topic]。
    /// </summary>
    private static string RebuildTag(UbbNode node, bool isImageVisible, int quoteDepth)
    {
        var name = GetTagName(node.Type);
        if (string.IsNullOrEmpty(name)) return ConvertChildren(node, isImageVisible, quoteDepth);
        return $"[{name}]{ConvertChildren(node, isImageVisible, quoteDepth)}[/{name}]";
    }

    private static string GetTagName(UbbNodeType type)
    {
        return type switch
        {
            UbbNodeType.Topic => "topic",
            UbbNodeType.NeedReply => "needreply",
            UbbNodeType.ReplyView => "replyview",
            UbbNodeType.PosterOnly => "posteronly",
            UbbNodeType.TableRow => "tr",
            UbbNodeType.TableCell => "td",
            UbbNodeType.Markdown => "md",
            UbbNodeType.NoUbb => "noubb",
            _ => ""
        };
    }

    /// <summary>
    ///     取节点文本形式的 URL(子文本节点拼接)。
    /// </summary>
    private static string GetNodeUrl(UbbNode node)
    {
        var sb = new StringBuilder();
        foreach (var child in node.Children)
        {
            if (child is TextNode text) sb.Append(text.Content);
            else sb.Append(CollectVerbatimText(child));
        }

        return sb.ToString();
    }

    /// <summary>
    ///     拼接逐字节点的原始文本(不做换行转换)。
    /// </summary>
    private static string CollectVerbatimText(UbbNode node)
    {
        var sb = new StringBuilder();
        foreach (var child in node.Children)
        {
            if (child is TextNode text) sb.Append(text.Content);
            else sb.Append(CollectVerbatimText(child));
        }

        return sb.ToString();
    }

    private static string EscapeMarkdown(string input)
    {
        var charsToEscape = new[] { '\\', '_', '+', '-', '.' };
        return charsToEscape.Aggregate(input, (current, c) =>
            current.Replace(c.ToString(), $"\\{c}"));
    }

    #endregion
}
