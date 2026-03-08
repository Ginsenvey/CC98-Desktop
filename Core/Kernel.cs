using CC98.Kernel.ApiScope;
using CC98.Kernel.Network;
using CC98.Objects;
using CC98.Services;
using FluentIcons.Common;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.Security.AccessControl;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.AppBroadcasting;
using Windows.Media.Core;
using Windows.Media.Protection.PlayReady;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Streams;
using static CC98.Kernel.ApiScope.ApiEndpoints;
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
            var res = await LoginService.vpn.GetAsync(endpoint);
            return await Deserialize<T>(res);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"网络错误: {ex.Message}",0);
        }
        catch (JsonException ex)
        {
            return ApiResponse<T>.Fail($"数据解析错误: {ex.Message}",0);
        }
        catch (TaskCanceledException)
        {
            return ApiResponse<T>.Fail("请求超时",0);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail($"系统错误: {ex.Message}",0);
        }

    }
    /// <summary>
    /// 适用于PUT。
    /// </summary>
    public static async Task<ApiResponse> Put(string endpoint,HttpContent? content)
    {
        try
        {
            var res = await LoginService.vpn.PutAsync(endpoint,content);
            var json=await res.Content.ReadAsStringAsync();
            return res.IsSuccessStatusCode? 
                ApiResponse.Success(json): 
                ApiResponse.Fail(res.ReasonPhrase ?? "请求失败", (int)res.StatusCode);   
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
            var res = await LoginService.vpn.DeleteAsync(endpoint);
            var json = await res.Content.ReadAsStringAsync();
            return res.IsSuccessStatusCode ?
                ApiResponse.Success(json) :
                ApiResponse.Fail(res.ReasonPhrase ?? "请求失败", (int)res.StatusCode);
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
    public static async Task<ApiResponse<T>> Submit<T>(string endpoint,HttpContent content)
    {
        try
        {
            var res = await LoginService.vpn.PostAsync(endpoint,content);
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
    public static async Task<ApiResponse<T>> Deserialize<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        //await App.Logger.WriteAsync("Request", res.StatusCode.ToString(),json);
        if (res.IsSuccessStatusCode)
        {
            var obj = JsonSerializer.Deserialize(json, typeof(T), CC98JsonContext.Default);
            var data = (T?)obj;
            return ApiResponse<T>.Success(data!);
        }
        else
        {
            return ApiResponse<T>.Fail(res.ReasonPhrase ?? "请求失败", (int)res.StatusCode);
        }
    }
   
    public static async Task<bool> AddFavorites(string topicId, string GroupId)//话题Id,收藏夹的组Id
    {
        if (!string.IsNullOrEmpty(GroupId) && (!string.IsNullOrEmpty(topicId)))
        {
            try
            {
                string favoriteurl = $"https://api.cc98.org/me/favorite/{topicId}?groupid={GroupId}";
                var request = new HttpRequestMessage(HttpMethod.Put, favoriteurl);
                var content = new StringContent("", Encoding.UTF8, "application/json");
                request.Content = content;//按照此格式发送空的put请求并设置请求头
                var res = await LoginService.vpn.SendAsync(favoriteurl, request);

                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }
    
    public static async Task<string> SignIn()
    {
        string SignInUrl = "https://api.cc98.org/me/signin";
        var request = new HttpRequestMessage(HttpMethod.Post, SignInUrl);
        var content = new StringContent("", Encoding.UTF8, "application/json");
        request.Content = content;//按照此格式发送空的post请求并设置请求头
        var res = await LoginService.vpn.SendAsync(SignInUrl, request);
        if (res.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            string restext = await res.Content.ReadAsStringAsync();

            if (restext == "has_signed_in_today")
            {
                return "2";
            }
            else
            {
                return "1";
            }
        }
        else if (res.StatusCode == HttpStatusCode.OK)
        {
            return "1";
        }
        else
        {
            return "0";
        }

    }
    
    public static async Task<string> SendPost(string board_id, string content, string title, int content_type, bool notify_poster, int post_type, bool is_anonymous)
    {
        string url = "https://api.cc98.org/board/" + board_id + "/topic";
        var post = new Dictionary<string, object>()
            {
                {"clientType",1 },
                {"content",content },
                {"contentType",content_type },
                {"isAnonymous",is_anonymous },
                {"notifyPoster",notify_poster },
                {"title",title },
                {"type",post_type }
            };
        string post_text =JsonSerializer.Serialize(post);
        var request_body = new StringContent(post_text, Encoding.UTF8, "application/json");
        var r = await LoginService.vpn.PostAsync(url, request_body);
        return await ValidationHelper.AutoResponse(r);
    }
    public static async Task<string> SendReplyToTopic(string replyid, string content, bool is_anonymous, bool notify_replier, int content_type, bool canbe_traced, string parent_id)//canbe_traced表明这是一个楼中楼，可以被追踪
    {
        string url = "https://api.cc98.org/topic/" + replyid + "/post";
        var reply = new Dictionary<string, object>();
        if (canbe_traced)
        {
            reply = new Dictionary<string, object>()
            {
                {"clientType",1 },
                {"content",content },
                {"contentType",content_type },
                {"isAnonymous",is_anonymous },
                {"notifyAllReplier",notify_replier },
                {"title","" },
                {"parentId",parent_id }

            };
        }
        else
        {
            reply = new Dictionary<string, object>()
            {
                {"clientType",1 },
                {"content",content },
                {"contentType",content_type },
                {"isAnonymous",is_anonymous },
                {"notifyAllReplier",notify_replier },
                {"title","" }
            };
        }
        string reply_text = JsonSerializer.Serialize(reply);
        var request_body = new StringContent(reply_text, Encoding.UTF8, "application/json");
        try
        {
            var r = await LoginService.vpn.PostAsync(url, request_body);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return await r.Content.ReadAsStringAsync();
            }
            else
            {
                return "101:" + r.StatusCode.ToString();
            }
        }
        catch (Exception ex)
        {
            return "400:" + ex.Message;
        }
    }
    public static async Task<bool> EditReply(string replyid, string content, string title, int content_type, bool notifyPoster)
    {
        string url = $"https://api.cc98.org/post/{replyid}";
        var reply = new Dictionary<string, object>()
            {
                {"type",0 },
                {"content",content },
                {"contentType",content_type },
                {"notifyPoster",notifyPoster },//常为true
                {"title",title }
            };
        string reply_text = JsonSerializer.Serialize(reply);
        var request_body = new StringContent(reply_text, Encoding.UTF8, "application/json");
        try
        {
            var r = await LoginService.vpn.PutAsync(url, request_body);
            return r.IsSuccessStatusCode;
        }
        catch
        {
            //ValidationHelper.Log("编辑回复失败", $"请求地址：{url}\r\n请求内容：{reply_text}");
            return false;
        }
    }
    public static async Task<string> SendPrivateMsg(int receiver_id, string content)
    {
        string url = "https://api.cc98.org/message";
        var post = new Dictionary<string, object>()
            {
                {"receiverId",receiver_id},
                {"content",content}
            };
        string post_text = JsonSerializer.Serialize(post);
        var request_body = new StringContent(post_text, Encoding.UTF8, "application/json");
        var r = await LoginService.vpn.PostAsync(url, request_body);
        if (r.IsSuccessStatusCode)
        {
            return "1";
        }
        else
        {
            return "0";
        }
    }
    public static async Task<string> SendVoteResult(string id, List<int> list)
    {
        string url = $"https://api.cc98.org/topic/{id}/vote";
        var post = new Dictionary<string, object>()
            {
                {"items",list}
            };
        string post_text = JsonSerializer.Serialize(post);
        var request_body = new StringContent(post_text, Encoding.UTF8, "application/json");
        var r = await LoginService.vpn.PostAsync(url, request_body);
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
        List<string> tags = new();
        string url = $"https://api.cc98.org/board/{bid}/tag";
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
        using (var outputStream = output.GetOutputStreamAt(0))
        using (var writer = new DataWriter(outputStream))
        {
            while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                writer.WriteBytes(buffer.AsSpan(0, bytesRead).ToArray());
                await writer.StoreAsync();
                await outputStream.FlushAsync();
            }
            await writer.FlushAsync();
        }
    }
    public static bool IsValidResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return false; // 返回false表示响应内容为空
        }
        else
        {
            if (response.StartsWith("404:"))
            {
                return false; // 返回false表示响应内容包含错误代码
            }
            else
            {
                return true; // 返回true表示响应内容有效
            }
        }



    }
    public static async Task<string> AutoResponse(HttpResponseMessage res)
    {
        try
        {
            if (res.IsSuccessStatusCode)
            {
                string Text = await res.Content.ReadAsStringAsync();
                if (IsValidResponse(Text))
                {
                    return Text;
                }
                else
                {
                    return "404:空返回";
                }
            }
            else
            {
                string error = await res.Content.ReadAsStringAsync();
                return $"404:请求失败，错误内容为{error}";
            }
        }
        catch
        {
            return "404:连接出错";
        }
    }
    public static string GetValue(ApplicationDataContainer container, string key)
    {
        if (container.Values.TryGetValue(key, out var token))
        {
            if (token != null)
            {
                var _token = token.ToString();
                if (!string.IsNullOrEmpty(_token))
                {
                    return _token;
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
                var _value = value.ToString();
                if (_value != null)
                {
                    return _value;
                }
            }
        }
        return "0";
    }
    public static int GetKeyAsInt(Dictionary<string, object> dic, string key)//值不可为"0".
    {
        if (dic == null) return 0;
        if (dic.TryGetValue(key, out var _value) && _value != null)
        {
            if (_value is int value)
            {
                return value;
            }
            if (_value is long l)
            {
                return (int)l;
            }
            if (_value is double d)
            {
                return (int)d;
            }
            if (_value is string s && int.TryParse(s, out int result))
            {
                return result;
            }
        }
        return 0;
    }
    public static string GetValue(NameValueCollection collection, string key)
    {
        if (collection.AllKeys.Contains(key))
        {
            var value = collection[key];
            if (value is string _value)
            {
                return _value;
            }
        }
        return "0";
    }
    public static string GetPropertyAsString(JsonElement root, string key)
    {
        string defaultValue = "0";
        if (root.TryGetProperty(key, out JsonElement element) &&
        element.ValueKind != JsonValueKind.Null)
        {
            return element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? defaultValue
                : defaultValue;
        }
        return defaultValue;
    }
    public static int GetPropertyAsInt(JsonElement root, string key)
    {
        int defaultValue = 0;
        if (root.TryGetProperty(key, out JsonElement element) &&
        element.ValueKind != JsonValueKind.Null)
        {
            return element.ValueKind == JsonValueKind.Number
                ? element.GetInt32()
                : defaultValue;
        }
        return defaultValue;
    }
    public static bool StringToBool(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        if (input != "0") return true;
        return false;
    }
}


public static class UBBConverter
{

    public static string Convert(string ubbText, bool IsImageVisible, bool escapeMarkdown = false)
    {
        var text = Preprocess(ubbText);
        // 处理块级元素（优先级从高到低）
        text = ConvertCodeBlocks(text);
        text = ConvertUBBTable(text);
        text = ConvertQuotes(text);
        text = ConvertLists(text);

        // 处理行内元素
        text = ConvertImages(text, IsImageVisible);
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
    public static string ConvertUBBTable(string input)
    {
        // 匹配UBB表格标签
        var tableRegex = new Regex(@"\[table\](.*?)\[/table\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return tableRegex.Replace(input, ConvertTable);
    }

    private static string ConvertTable(Match tableMatch)
    {
        string tableContent = tableMatch.Groups[1].Value;
        var rowRegex = new Regex(@"\[tr\](.*?)\[/tr\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var rows = new List<List<string>>();

        // 提取所有行
        foreach (Match rowMatch in rowRegex.Matches(tableContent))
        {
            string rowContent = rowMatch.Groups[1].Value;
            var cellRegex = new Regex(@"\[(th|td)\](.*?)\[/\1\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var rowCells = new List<string>();

            // 提取行内所有单元格
            foreach (Match cellMatch in cellRegex.Matches(rowContent))
            {
                string cellValue = cellMatch.Groups[2].Value;
                // 转义Markdown特殊字符 | 和换行符
                cellValue = cellValue.Replace("|", "\\|").Replace("\r\n", " ").Replace("\n", " ");
                rowCells.Add(cellValue);
            }
            rows.Add(rowCells);
        }

        if (rows.Count == 0) return string.Empty;

        // 确定最大列数
        int maxCols = rows.Max(row => row.Count);
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
        for (int i = 0; i < rows.Count; i++)
        {
            markdown.Append("| ");
            markdown.Append(string.Join(" | ", rows[i]));
            markdown.AppendLine(" |");

            // 添加表头分隔行
            if (i == 0)
            {
                markdown.Append("| ");
                for (int j = 0; j < maxCols; j++)
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

        string pattern = @"(\[quote\])|(\[/quote\])";

        // 使用栈来处理嵌套层级。实际上栈不起作用，只是为了借用栈的思想。

        StringBuilder output = new StringBuilder();
        int currentLevel = 0;

        // 当前处理的文本
        int lastIndex = 0;

        // 正则匹配标签并进行替换
        foreach (Match match in Regex.Matches(input, pattern))
        {
            // 获取标签的开始位置
            int matchStart = match.Index;
            // 获取标签的结束位置
            int matchEnd = match.Index + match.Length;

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
        string pattern = @"\[color=[^\]]*\](.*?)\[/color\]";

        // 循环处理，逐层去除嵌套的 color 标签
        string result = input;
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
        var TrimList = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>(@"\[b\](.*?)\[/b\]","**"),
                new KeyValuePair<string, string>(@"\[i\](.*?)\[/i\]","*"),
                new KeyValuePair<string, string>(@"\[u\](.*?)\[/u\]",""),
                new KeyValuePair<string, string>(@"\[del\](.*?)\[/del\]","~~")
            };
        foreach (var kvp in TrimList)
        {
            input = Regex.Replace(input, kvp.Key, match =>
            {
                string content = match.Groups[1].Value;
                // 分割内容为多个段落    
                string[] paragraphs = content.Split(new[] { "  \n" }, StringSplitOptions.None);

                // 为每个段落单独添加加粗标记
                for (int i = 0; i < paragraphs.Length; i++)
                {
                    // 移除每段前后的空白，但保留内部格式
                    string trimmed = paragraphs[i].Trim();
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

                string pattern = @"https:\/\/api\.cc98\.org\/user\/name\/([^\/\s]+)";
                MatchCollection matches = Regex.Matches(link, pattern);
                if (matches.Count == 1)
                {
                    string username = matches[0].Groups[1].Value;
                    return new KeyValuePair<string, string>("user", username);
                }
                else
                {
                    return new KeyValuePair<string, string>("null", link);
                }

            }
            else if (link.Contains("/topic/") && (!link.Contains("#")))
            {
                string pattern = @"\/topic\/([^\/\s]+)";
                MatchCollection matches = Regex.Matches(link, pattern);
                if (matches.Count == 1)
                {
                    string pid = matches[0].Groups[1].Value;
                    return new KeyValuePair<string, string>("topic", pid);
                }
                else
                {
                    return new KeyValuePair<string, string>("null", link);
                }
            }
            else if (link.Contains("#"))
            {
                Match match = Regex.Match(link, @"/topic/(\d{7})/(\d+)#(\d+)");
                if (match.Success)
                {
                    string numberAfterHash = match.Groups[1].Value;
                    return new KeyValuePair<string, string>("anchor", link);//返回索引楼层
                }
                return new KeyValuePair<string, string>("null", link);
            }
            else if (link.Contains("file"))
            {
                string ext = Path.GetExtension(link)?.TrimStart('.').ToLowerInvariant();

                HashSet<string> picformats = new() { "jpg", "jpeg", "png", "gif", "webp" };
                HashSet<string> audioformats = new() { "mp3", "wav", "m4a", "ogg", "flac" };
                HashSet<string> videofromats = new() { "mp4", "avi", "mkv", "mov", "wmv" };

                if (!string.IsNullOrEmpty(ext))
                {
                    if (picformats.Contains(ext))
                    {
                        return new KeyValuePair<string, string>("file", "image");
                    }
                    else if (audioformats.Contains(ext))
                    {
                        return new KeyValuePair<string, string>("file", "audio");
                    }
                    else if (videofromats.Contains(ext))
                    {
                        return new KeyValuePair<string, string>("file", "video");
                    }
                    else
                    {
                        return new KeyValuePair<string, string>("file", "doc");
                    }
                }
                else
                {
                    return new KeyValuePair<string, string>("null", link);
                }

            }
            else if (link.Contains("https://www.bilibili.com/video"))
            {
                return new KeyValuePair<string, string>("backlink", "bili");
            }
            else if (link.Contains("board"))
            {
                Match match = Regex.Match(link, @"\/board\/(\d{2,3})");
                if (match.Success)
                {
                    string board_id = match.Groups[1].Value;
                    return new KeyValuePair<string, string>("board", board_id);
                }
                return new KeyValuePair<string, string>("null", link);
            }
            else
            {
                return new KeyValuePair<string, string>("null", link);
            }
        }
        else
        {
            return new KeyValuePair<string, string>("null", link);
        }

    }
}







