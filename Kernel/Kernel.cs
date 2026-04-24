using CC98.Objects;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Streams;
using CC98.Services;

namespace CC98.Kernel;

/// <summary>
/// 对Http请求做二次封装，提供给UI层。
/// </summary>
public static class RequestSender
{
    /// <summary>
    /// 封装通用GET请求，并实现自动错误处理。
    /// </summary>
    public static async Task<ApiResponse<T>> Fetch<T>(string endpoint)
    {
        try
        {
            var res = await LoginService.Vpn.GetAsync(endpoint);
            return await Deserialize<T>(res);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"网络错误: {ex.Message}", 0);
        }
        catch (JsonException ex)
        {
            return ApiResponse<T>.Fail($"数据解析错误: {ex.Message}", 0);
        }
        catch (TaskCanceledException)
        {
            return ApiResponse<T>.Fail("请求超时", 0);
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("AOT", "其他错误", ex.Message);
            return ApiResponse<T>.Fail($"系统错误: {ex.Message}", 0);
        }

    }
    /// <summary>
    /// 适用于PUT。
    /// </summary>
    public static async Task<ApiResponse> Put(string endpoint, HttpContent? content)
    {
        try
        {
            var res = await LoginService.Vpn.PutAsync(endpoint, content);
            var json = await res.Content.ReadAsStringAsync();
            return res.IsSuccessStatusCode ?
                ApiResponse.Success(json) :
                ApiResponse.Fail((res.ReasonPhrase ?? "响应失败") + ":" + json, (int)res.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse.Fail($"网络错误: {ex.Message}", 0);
        }
        catch (JsonException ex)
        {
            return ApiResponse.Fail($"数据解析错误: {ex.Message}", 0);
        }
        catch (TaskCanceledException)
        {
            return ApiResponse.Fail("请求超时", 0);
        }
        catch (Exception ex)
        {
            return ApiResponse.Fail($"系统错误: {ex.Message}", 0);
        }
    }
    /// <summary>
    /// 适用于DELETE。
    /// </summary>
    public static async Task<ApiResponse> Delete(string endpoint)
    {
        try
        {
            var res = await LoginService.Vpn.DeleteAsync(endpoint);
            var json = await res.Content.ReadAsStringAsync();
            return res.IsSuccessStatusCode ?
                ApiResponse.Success(json) :
                ApiResponse.Fail((res.ReasonPhrase ?? "响应失败") + ":" + json, (int)res.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse.Fail($"网络错误: {ex.Message}", 0);
        }
        catch (JsonException ex)
        {
            return ApiResponse.Fail($"数据解析错误: {ex.Message}", 0);
        }
        catch (TaskCanceledException)
        {
            return ApiResponse.Fail("请求超时", 0);
        }
        catch (Exception ex)
        {
            return ApiResponse.Fail($"系统错误: {ex.Message}", 0);
        }
    }
    /// <summary>
    /// T是返回类型，不是提交数据类型。提交数据类型由HttpContent的具体实现决定
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="endpoint"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static async Task<ApiResponse<T>> Submit<T>(string endpoint, HttpContent content)
    {
        try
        {
            var res = await LoginService.Vpn.PostAsync(endpoint, content);
            return await Deserialize<T>(res);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"网络错误: {ex.Message}", 0);
        }
        catch (JsonException ex)
        {
            return ApiResponse<T>.Fail($"数据解析错误: {ex.Message}", 0);
        }
        catch (TaskCanceledException)
        {
            return ApiResponse<T>.Fail("请求超时", 0);
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("AOT", "其他错误", ex.Message);
            return ApiResponse<T>.Fail($"系统错误: {ex.Message}", 0);
        }

    }
    public static async Task<ApiResponse<T>> Deserialize<T>(HttpResponseMessage res, CancellationToken cancellationToken = default)
    {
        if (!res.IsSuccessStatusCode)
        {
            return ApiResponse<T>.Fail(res.ReasonPhrase ?? "响应失败", (int)res.StatusCode);
        }

        try
        {
            var obj = await res.Content.ReadFromJsonAsync(typeof(T), Cc98JsonContext.Default, cancellationToken);
            return ApiResponse<T>.Success((T?)obj!);
        }
        catch (JsonException ex)
        {
            return ApiResponse<T>.Fail(ex.Message);

        }


    }


    public static async Task<string> SendVoteResult(string id, List<int> list)
    {
        var url = $"https://api.cc98.org/topic/{id}/vote";
        var post = new Dictionary<string, object>()
            {
                {"items",list}
            };
        var postText = JsonSerialize.Serialize(post);
        var requestBody = new StringContent(postText, Encoding.UTF8, "application/json");
        var r = await LoginService.Vpn.PostAsync(url, requestBody);
        if (r.IsSuccessStatusCode)
        {
            return "1";
        }
        else
        {
            return "0";
        }
    }


    public static async Task<ApiResponse<string>> GetBoardTags(string bid)
    {
        List<string> tags = [];
        var url = $"https://api.cc98.org/board/{bid}/tag";
        return await Fetch<string>(url);
    }
}



public static class ValidationHelper
{
    public static async Task CopyStreamToRandomAccessStream(Stream input, IRandomAccessStream output)
    {
        var buffer = new byte[16 * 1024];
        int bytesRead;

        // 获取输出流的写入器
        using var outputStream = output.GetOutputStreamAt(0);
        using var writer = new DataWriter(outputStream);
        while ((bytesRead = await input.ReadAsync(buffer)) > 0)
        {
            writer.WriteBytes(buffer.AsSpan(0, bytesRead).ToArray());
            await writer.StoreAsync();
            await outputStream.FlushAsync();
        }
        await writer.FlushAsync();
    }


    public static string GetValue(ApplicationDataContainer container, string key)
    {
        if (container.Values.TryGetValue(key, out var token))
        {
            if (token != null)
            {
                var tokenString = token.ToString();
                if (!string.IsNullOrEmpty(tokenString))
                {
                    return tokenString;
                }
            }
        }
        return "0";
    }


    public static string GetKey(Dictionary<string, object> dic, string key)//值不可为"0".
    {
        if (dic == null) return "0";
        if (dic.TryGetValue(key, out var value))
        {
            if (value != null)
            {
                var valueString = value.ToString();
                if (valueString != null)
                {
                    return valueString;
                }
            }
        }
        return "0";
    }
    public static int GetKeyAsInt(Dictionary<string, object>? dic, string key)//值不可为"0".
    {
        if (dic == null) return 0;

        if (!dic.TryGetValue(key, out var value))
        {
            return 0;
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            string s when int.TryParse(s, out var result) => result,
            _ => 0,
        };
    }
    public static string GetValue(NameValueCollection collection, string key)
    {
        if (collection.AllKeys.Contains(key))
        {
            var value = collection[key];
            if (value is string str)
            {
                return str;
            }
        }
        return "0";
    }
}



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
        {
            while (row.Count < maxCols)
            {
                row.Add("");
            }
        }

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

        return "  \n" + markdown.ToString();
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
                output.Append(new string(input.Substring(lastIndex, matchStart - lastIndex).Replace("\n", "  \n" + new string('>', currentLevel)).Replace("\r", "  \n" + new string('>', currentLevel)) + "  \n" + new string('>', currentLevel - 1) + "  \n" + new string('>', currentLevel - 1)));
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
        return string.Join("\n", items.Cast<Match>()
            .Select(m => $"* {m.Groups[1].Value.Trim()}"));
    }

    private static string ConvertImages(string input, bool mode)
    {
        if (mode)
        {
            return Regex.Replace(input,
    @"\[img\](.*?)\[/img\]",
    "![#**图片**#]($1)",
    RegexOptions.IgnoreCase);
        }
        else
        {
            return Regex.Replace(input,
    @"\[img\](.*?)\[/img\]",
    "[#**图片**#]($1)",
    RegexOptions.IgnoreCase);
        }


    }
    private static string ConvertColor(string input)
    {
        var pattern = @"\[color=[^\]]*\](.*?)\[/color\]";

        // 循环处理，逐层去除嵌套的 color 标签
        var result = input;
        while (Regex.IsMatch(result, pattern))
        {
            result = Regex.Replace(result, pattern, "$1");
        }
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
                    (@"\[ac(\d{2,4})\]","![#ac$1#](ms-appx:///Assets/Emoji/ac-white/ac$1.png)"),//ac娘
                    (@"\[em(\d{2})\]","![#em$1#](ms-appx:///Assets/Emoji/em/em$1.gif)"),//经典
                    (@"\[([a-zA-Z]{2})(\d{2})\]","![#$1$2#](ms-appx:///Assets/Emoji/$1/$1$2.png)"),//贴吧，雀魂
                    (@"\[cc98(\d{2})\]","![#cc98$1#](ms-appx:///Assets/Emoji/CC98/CC98$1.png)")//cc98

                };
        foreach (var (pattern, replacement) in replacements)
        {
            input = Regex.Replace(input, pattern, replacement,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }
        return input;
    }

    private static string ConvertTrimTextStyles(string input)
    {
        var trimList = new List<KeyValuePair<string, string>>()
            {
                new(@"\[b\](.*?)\[/b\]","**"),
                new(@"\[i\](.*?)\[/i\]","*"),
                new(@"\[u\](.*?)\[/u\]",""),
                new(@"\[del\](.*?)\[/del\]","~~")
            };
        foreach (var kvp in trimList)
        {
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
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        paragraphs[i] = $"{kvp.Value}{trimmed}{kvp.Value}";
                    }
                }

                // 重新组合段落
                return string.Join("  \n", paragraphs);
            }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }
        return input;



    }

    private static string ConvertTextStyles(string input)
    {
        var replacements = new[]
    {
            (@"\[color=[^\]]*\](.*?)\[/color\]", "$1"),
            (@"\[font=.*?\](.*?)\[/font\]", "$1"),
            (@"\[size=\d{1,2}\]",""),
            (@"\[/size\]",""),
            (@"\[center\](.*?)\[/center\]","$1"),
            (@"\<center\>(.*?)\</center\>","$1"),
            (@"\[right\](.*?)\[/right\]","$1"),
            (@"\[left\](.*?)\[/left\]","$1"),
            (@"<p[^>]*>(.*?)</p>","$1"),
            (@"\[align=[^\]]+\](.*?)\[/align\]","$1"),
            (@"<img\s+[^>]*src=""([^""]+)""[^>]*>","[#**图片**#]($1)"),
            (@"@(\S+)\s","[@ $1 ](https://api.cc98.org/user/name/$1)"),
            (@"\[audio\](.*?)\[/audio\]","[#**音频**#]($1)"),
            (@"\[video\](.*?)\[/video\]","[#**视频**#]($1)"),
            (@"\[upload\](.*?)\[/upload\]","[#**文件**#]($1)"),
            (@"\[bili\](.*?)\[/bili\]","[#**哔哩**#](https://www.bilibili.com/video/$1/)"),

        };

        foreach (var (pattern, replacement) in replacements)
        {
            input = Regex.Replace(input, pattern, replacement,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

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

public static class LinkAnalyzer
{
    public static KeyValuePair<string, string> Parse(string link)
    {
        if (!string.IsNullOrEmpty(link))
        {

            if (link.Contains("user/name"))
            {

                var pattern = @"https:\/\/api\.cc98\.org\/user\/name\/([^\/\s]+)";
                var matches = Regex.Matches(link, pattern);
                if (matches.Count == 1)
                {
                    var username = matches[0].Groups[1].Value;
                    return new("user", username);
                }
                else
                {
                    return new("null", link);
                }

            }
            else if (link.Contains("/topic/") && (!link.Contains("#")))
            {
                var pattern = @"\/topic\/([^\/\s]+)";
                var matches = Regex.Matches(link, pattern);
                if (matches.Count == 1)
                {
                    var pid = matches[0].Groups[1].Value;
                    return new("topic", pid);
                }
                else
                {
                    return new("null", link);
                }
            }
            else if (link.Contains("#"))
            {
                var match = Regex.Match(link, @"/topic/(\d{7})/(\d+)#(\d+)");
                if (match.Success)
                {
                    var numberAfterHash = match.Groups[1].Value;
                    return new("anchor", link);//返回索引楼层
                }
                return new("null", link);
            }

            else if (link.Contains("https://www.bilibili.com/video"))
            {
                return new("backlink", "bili");
            }
            else if (link.Contains("board"))
            {
                var match = Regex.Match(link, @"\/board\/(\d{2,3})");
                if (match.Success)
                {
                    var boardId = match.Groups[1].Value;
                    return new("board", boardId);
                }
                return new("null", link);
            }
            else
            {
                return new("null", link);
            }
        }
        else
        {
            return new("null", link);
        }

    }
}







