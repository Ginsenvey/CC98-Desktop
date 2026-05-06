using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CC98.Services.Helpers;

//在基于UbbParser的新转换器完成后，此类将被废弃。
public static class UbbToMd
{
    public static string Convert(string ubbText, bool isImageVisible, bool escapeMarkdown = false)
    {
        var text = Preprocess(ubbText);
        // 处理块级元素（优先级从高到低）
        text = ConvertCodeBlocks(text);
        text = ConvertUbbTable(text);
        text = ConvertQuotes(text);
        text = ConvertLists(text);

        // 处理行内元素
        text = ConvertImages(text, isImageVisible);
        text = ConvertLinks(text);
        text = ConvertEmoji(text);
        text = ConvertColor(text);
        text = ConvertTrimTextStyles(text);
        text = ConvertTextStyles(text);

        return escapeMarkdown ? EscapeMarkdown(text) : text;
    }

    private static string Preprocess(string input)
    {
        return input.Replace("\r\n", "  \n")
            .Replace("\r", "  \n")
            .Replace("\n", "  \n")
            .Replace("<br>", "  \n")
            .Replace("[line]", "  \n  \n---")
            .Trim();
    }

    //两个空格加\n是markdown控件的换行格式。\n和\r是操作系统回车键的格式。字符串"\n"是cc98传输文本的换行格式。
    private static string ConvertCodeBlocks(string input)
    {
        return Regex.Replace(input,
            @"\[code\](.*?)\[/code\]",
            m => $"```\n{m.Groups[1].Value.Trim()}\n```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
    }

    public static string ConvertUbbTable(string input)
    {
        // 匹配UBB表格标签
        var tableRegex = new Regex(@"\[table\](.*?)\[/table\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return tableRegex.Replace(input, ConvertTable);
    }

    private static string ConvertTable(Match tableMatch)
    {
        var tableContent = tableMatch.Groups[1].Value;
        var rowRegex = new Regex(@"\[tr\](.*?)\[/tr\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var rows = new List<List<string>>();

        // 提取所有行
        foreach (Match rowMatch in rowRegex.Matches(tableContent))
        {
            var rowContent = rowMatch.Groups[1].Value;
            var cellRegex = new Regex(@"\[(th|td)\](.*?)\[/\1\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var rowCells = new List<string>();

            // 提取行内所有单元格
            foreach (Match cellMatch in cellRegex.Matches(rowContent))
            {
                var cellValue = cellMatch.Groups[2].Value;
                // 转义Markdown特殊字符 | 和换行符
                cellValue = cellValue.Replace("|", "\\|").Replace("\r\n", " ").Replace("\n", " ");
                rowCells.Add(cellValue);
            }

            rows.Add(rowCells);
        }

        if (rows.Count == 0) return string.Empty;

        // 确定最大列数
        var maxCols = rows.Max(row => row.Count);
        if (maxCols == 0) return string.Empty;

        // 补齐空单元格
        foreach (var row in rows)
            while (row.Count < maxCols)
                row.Add("");

        // 构建Markdown表格
        var markdown = new StringBuilder();
        for (var i = 0; i < rows.Count; i++)
        {
            markdown.Append("| ");
            markdown.Append(string.Join(" | ", rows[i]));
            markdown.AppendLine(" |");

            // 添加表头分隔行
            if (i == 0)
            {
                markdown.Append("| ");
                for (var j = 0; j < maxCols; j++)
                {
                    markdown.Append("---");
                    if (j < maxCols - 1) markdown.Append(" | ");
                }

                markdown.AppendLine(" |");
            }
        }

        return "  \n" + markdown;
    }

    private static string ConvertQuotes(string input)
    {
        var pattern = @"(\[quote\])|(\[/quote\])";

        // 使用栈来处理嵌套层级。实际上栈不起作用，只是为了借用栈的思想。

        var output = new StringBuilder();
        var currentLevel = 0;

        // 当前处理的文本
        var lastIndex = 0;

        // 正则匹配标签并进行替换
        foreach (Match match in Regex.Matches(input, pattern))
        {
            // 获取标签的开始位置
            var matchStart = match.Index;
            // 获取标签的结束位置
            var matchEnd = match.Index + match.Length;

            // 先处理标签之前的文本


            if (match.Value == "[quote]")
            {
                output.Append(input.Substring(lastIndex, matchStart - lastIndex));
                // 处理 [quote] 标签：增加层级并推入栈

                currentLevel++;
                // 添加 Markdown 格式的引用
                output.Append(new string('>', currentLevel) + " ");
            }
            else if (match.Value == "[/quote]")
            {
                output.Append(new string(
                    input.Substring(lastIndex, matchStart - lastIndex)
                        .Replace("\n", "  \n" + new string('>', currentLevel))
                        .Replace("\r", "  \n" + new string('>', currentLevel)) + "  \n" +
                    new string('>', currentLevel - 1) + "  \n" + new string('>', currentLevel - 1)));
                // 处理 [/quote] 标签：减少层级并弹出栈.
                //将系统换行符替换为引用符号非常关键，构造正确的换行结构和引用的续引用。
                currentLevel--;

                // 不添加文本，只需要关闭当前层级的引用
            }

            // 更新 lastIndex
            lastIndex = matchEnd;
        }

        // 处理最后一个标签之后的文本
        output.Append("  \n" + input.Substring(lastIndex));


        return output.ToString();
    }

    private static string ConvertLists(string input)
    {
        return Regex.Replace(input,
            @"\[list\](.*?)\[/list\]",
            m => ProcessListItems(m.Groups[1].Value),
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
    }

    private static string ProcessListItems(string content)
    {
        var items = Regex.Matches(content, @"\[\*\]([^\[]+)");
        return string.Join("\n", items
            .Select(m => $"* {m.Groups[1].Value.Trim()}"));
    }

    private static string ConvertImages(string input, bool mode)
    {
        if (mode)
            return Regex.Replace(input,
                @"\[img\](.*?)\[/img\]",
                "![#**图片**#]($1)",
                RegexOptions.IgnoreCase);

        return Regex.Replace(input,
            @"\[img\](.*?)\[/img\]",
            "[#**图片**#]($1)",
            RegexOptions.IgnoreCase);
    }

    private static string ConvertColor(string input)
    {
        var pattern = @"\[color=[^\]]*\](.*?)\[/color\]";

        // 循环处理，逐层去除嵌套的 color 标签
        var result = input;
        while (Regex.IsMatch(result, pattern)) result = Regex.Replace(result, pattern, "$1");
        return result;
    }

    private static string ConvertLinks(string input)
    {
        // 带标题的链接 [url=...]...[/url]
        input = Regex.Replace(input,
            @"\[url=(.*?)\](.*?)\[/url\]",
            "[$2]($1)",
            RegexOptions.IgnoreCase);

        // 无标题链接 [url]...[/url]
        return Regex.Replace(input,
            @"\[url\](.*?)\[/url\]",
            "[$1]($1)",
            RegexOptions.IgnoreCase);
    }

    private static string ConvertEmoji(string input)
    {
        var replacements = new[]
        {
            (@"\[ac(\d{2,4})\]", "![#ac$1#](ms-appx:///Assets/Emoji/ac-white/ac$1.png)"), //ac娘
            (@"\[em(\d{2})\]", "![#em$1#](ms-appx:///Assets/Emoji/em/em$1.gif)"), //经典
            (@"\[([a-zA-Z]{2})(\d{2})\]", "![#$1$2#](ms-appx:///Assets/Emoji/$1/$1$2.png)"), //贴吧，雀魂
            (@"\[cc98(\d{2})\]", "![#cc98$1#](ms-appx:///Assets/Emoji/CC98/CC98$1.png)") //cc98
        };
        foreach (var (pattern, replacement) in replacements)
            input = Regex.Replace(input, pattern, replacement,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return input;
    }

    private static string ConvertTrimTextStyles(string input)
    {
        var trimList = new List<KeyValuePair<string, string>>
        {
            new(@"\[b\](.*?)\[/b\]", "**"),
            new(@"\[i\](.*?)\[/i\]", "*"),
            new(@"\[u\](.*?)\[/u\]", ""),
            new(@"\[del\](.*?)\[/del\]", "~~")
        };
        foreach (var kvp in trimList)
            input = Regex.Replace(input, kvp.Key, match =>
            {
                var content = match.Groups[1].Value;
                // 分割内容为多个段落    
                var paragraphs = content.Split(["  \n"], StringSplitOptions.None);

                // 为每个段落单独添加加粗标记
                for (var i = 0; i < paragraphs.Length; i++)
                {
                    // 移除每段前后的空白，但保留内部格式
                    var trimmed = paragraphs[i].Trim();
                    if (!string.IsNullOrEmpty(trimmed)) paragraphs[i] = $"{kvp.Value}{trimmed}{kvp.Value}";
                }

                // 重新组合段落
                return string.Join("  \n", paragraphs);
            }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return input;
    }

    private static string ConvertTextStyles(string input)
    {
        var replacements = new[]
        {
            (@"\[color=[^\]]*\](.*?)\[/color\]", "$1"),
            (@"\[font=.*?\](.*?)\[/font\]", "$1"),
            (@"\[size=\d{1,2}\]", ""),
            (@"\[/size\]", ""),
            (@"\[center\](.*?)\[/center\]", "$1"),
            (@"\<center\>(.*?)\</center\>", "$1"),
            (@"\[right\](.*?)\[/right\]", "$1"),
            (@"\[left\](.*?)\[/left\]", "$1"),
            (@"<p[^>]*>(.*?)</p>", "$1"),
            (@"\[align=[^\]]+\](.*?)\[/align\]", "$1"),
            (@"<img\s+[^>]*src=""([^""]+)""[^>]*>", "[#**图片**#]($1)"),
            (@"@(\S+)\s", "[@ $1 ](https://api.cc98.org/user/name/$1)"),
            (@"\[audio\](.*?)\[/audio\]", "[#**音频**#]($1)"),
            (@"\[video\](.*?)\[/video\]", "[#**视频**#]($1)"),
            (@"\[upload\](.*?)\[/upload\]", "[#**文件**#]($1)"),
            (@"\[bili\](.*?)\[/bili\]", "[#**哔哩**#](https://www.bilibili.com/video/$1/)")
        };

        foreach (var (pattern, replacement) in replacements)
            input = Regex.Replace(input, pattern, replacement,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return input;
    }

    private static string Cleanup(string input)
    {
        // 合并多余空行
        return Regex.Replace(input, @"\n{3,}", "\n\n");
    }

    private static string EscapeMarkdown(string input)
    {
        var charsToEscape = new[] { '\\', '_', '+', '-', '.' };
        return charsToEscape.Aggregate(input, (current, c) =>
            current.Replace(c.ToString(), $"\\{c}"));
    }
}