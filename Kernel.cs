using App3;
using FluentIcons.Common;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Security.AccessControl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using static App3.Topic;
namespace CCkernel
{
    //管理登录状态
    public static class CCloginservice
    {
        public static  HttpClient client;
        private readonly static CookieContainer Jar = new CookieContainer();
        public static HttpClientHandler handler;
        static CCloginservice()
        {
            handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = Jar,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            client = new HttpClient(handler);

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.58");
            client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");

        }
        public static async Task<string> LoginAsync(string username, string password)
        {
            string LoginUrl = "https://openid.cc98.org/connect/token";
            var data = new Dictionary<string, string>()
            {
                {"username",username},
                {"password",password },
                {"client_id","9a1fd200-8687-44b1-4c20-08d50a96e5cd" },
                {"client_secret","8b53f727-08e2-4509-8857-e34bf92b27f2"},
                {"grant_type" ,"password"},
                {"scope","cc98-api openid offline_access" }
            };
            var PostData = new FormUrlEncodedContent(data);
            var response = await client.PostAsync(LoginUrl, PostData);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return "0";
            }
        }
        public static async Task<string> RefreshToken(string rft)
        {
            if (!string.IsNullOrEmpty(rft))
            {
                string LoginUrl = "https://openid.cc98.org/connect/token";
                var data = new Dictionary<string, string>()
            {
                {"client_id","9a1fd200-8687-44b1-4c20-08d50a96e5cd" },
                {"client_secret","8b53f727-08e2-4509-8857-e34bf92b27f2"},
                {"grant_type" ,"refresh_token"},
                {"refresh_token",rft },
                {"scope","cc98-api openid offline_access" }
            };
                var PostData = new FormUrlEncodedContent(data);
                var response = await client.PostAsync(LoginUrl, PostData);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string NewAccessText = await response.Content.ReadAsStringAsync();
                    var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(NewAccessText);
                    if (js.TryGetValue("access_token", out var token))
                    {
                        if (token.ToString() != null)
                        {
                            return token.ToString();
                        }
                        else
                        {
                            return "0";
                        }
                    }
                    else
                    {
                        return "0";
                    }


                }
                else
                {
                    return "0";
                }
            }
            else
            {
                return "0";
            }
        }
        public static async Task<string> DeepAuthService(string id, string pass)
        {
            string url = "https://openid.cc98.org/Account/LogOn?returnUrl=%2F";
            var res = await client.GetAsync(url);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                var content = await res.Content.ReadAsStringAsync();
                doc.LoadHtml(content);
                var input = doc.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']");
                if (input != null)
                {
                    string token = input.GetAttributeValue("value", "");
                    var data = new Dictionary<string, string>()
                    {
                        {"__RequestVerificationToken",token},
                        {"UserName",id },
                        {"Password",pass},
                        {"ValidTime",""}
                    };
                    var PostData = new FormUrlEncodedContent(data);
                    var response = await client.PostAsync(url, PostData);
                    if (response.StatusCode == HttpStatusCode.Redirect)
                    {
                        List<string> keys = new();
                        foreach (Cookie c in Jar.GetAllCookies())
                        {
                            keys.Add(c.Name);
                        }
                        if (keys.Contains("idsrv"))
                        {
                            return "1";
                        }
                        else
                        {
                            return "0";
                        }
                    }
                    else
                    {
                        return "0";
                    }
                }
                else
                {
                    return "0";
                }
            }
            else
            {
                return "0";
            }
        }
        

    }
    //约定：请求总是返回json字符串,或者错误代码。
    //发送到解析器的文本总是不为空。
    //帖子、版面、个人信息核心操作
    public static class RequestSender
    {
        //通用的简单请求方法
        public static async Task<string> SimpleRequest(string api)
        {
            
            var response = await CCloginservice.client.GetAsync(api);
            return await ValidationHelper.AutoResponse(response); 
        }
        //获取新帖
        
        public static async Task<string> TopicReply(string uid,  string start)
        {
            try
            {
                
                var Response = await CCloginservice.client.GetAsync("https://api.cc98.org/Topic/" + uid + "/post?from=" + start + "&size=10");
                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    string ResponseBody = await Response.Content.ReadAsStringAsync();
                    if(!string.IsNullOrEmpty(ResponseBody))
                    {
                        return ResponseBody;
                    }
                    else
                    {
                        return "404:空返回";
                    }
                }
                else
                {
                    return "404:请求失败";
                }

            }
            catch (Exception ex)
            {
                return "404:" + ex.Message;
            }

        }
        //以"id=username"的格式作为列表的元素user
        public static async Task<string> SimpleUserInfo(List<string> users)
        {
            string param = string.Join("&", users);
            string url = "https://api.cc98.org/user/basic?" + param;
            
            var PortRes = await CCloginservice.client.GetAsync(url);
            if (PortRes.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await PortRes.Content.ReadAsStringAsync();
                if (ValidationHelper.IsValidResponse(content))
                {
                    return content;
                }
                else
                {
                    return "404:空返回";
                }
            }
            else
            {
                return "404:请求失败";
            }
        }

        public static async Task<JArray> FavoritesList()
        {
            string url = "https://api.cc98.org/me/favorite-topic-group";
            try
            {
                var LikeRes = await CCloginservice.client.GetAsync(url);
                if (LikeRes.StatusCode == HttpStatusCode.OK)
                {
                    string LikeText = await LikeRes.Content.ReadAsStringAsync();
                    var likes = JsonConvert.DeserializeObject<Dictionary<string, object>>(LikeText);
                    var LikeList = JsonConvert.DeserializeObject<JArray>(likes["data"].ToString());
                    return LikeList;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public static async Task<bool> AddFavorites(string Pid,string GroupId)//话题Id,收藏夹的组Id
        {
            if (!string.IsNullOrEmpty(GroupId)&&(!string.IsNullOrEmpty(Pid)))
            {
                try
                {
                    string favoriteurl = "https://api.cc98.org/me/favorite/" + Pid + "?groupid=" + GroupId;
                    var request = new HttpRequestMessage(HttpMethod.Put, favoriteurl);
                    var content = new StringContent("", Encoding.UTF8, "application/json");
                    request.Content = content;//按照此格式发送空的put请求并设置请求头
                    var res = await CCloginservice.client.SendAsync(request);

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
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cc98.org/me/signin");
            var content = new StringContent("", Encoding.UTF8, "application/json");
            request.Content = content;//按照此格式发送空的post请求并设置请求头
            var res = await CCloginservice.client.SendAsync(request);
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
            else
            {
                return "0";
            }

        }
        public static async Task<string> SystemNotice(string start)
        {
            string url = "https://api.cc98.org/notification/system?from=" + start+"&size=10";
            var res=await CCloginservice.client.GetAsync(url);
            return await ValidationHelper.AutoResponse(res);
        }
        public static async Task<bool> Like(string mode,string postid)
        {
            string url = "https://api.cc98.org/post/"+postid+"/like";
            var content = new StringContent(mode, Encoding.UTF8, "application/json");
            var response = await CCloginservice.client.PutAsync(url, content);
            try
            {
                if(response.StatusCode == System.Net.HttpStatusCode.OK)
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
        public static async Task<Dictionary<string,string>> LikeState(string postid)
        {
            string url = "https://api.cc98.org/post/" + postid + "/like";
            var res = await CCloginservice.client.GetAsync(url);
            try
            {
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    string restext = await res.Content.ReadAsStringAsync();
                    if (ValidationHelper.IsValidResponse(restext))
                    {
                        var state = JsonConvert.DeserializeObject<Dictionary<string, object>>(restext);
                        return new Dictionary<string, string>()
                        {
                            {"like",state["likeCount"].ToString() },
                            {"dislike",state["dislikeCount"].ToString() },
                            {"likestate",state["likeState"].ToString() }
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }

    //将json字符串解析为目标对象,总是返回对象或者null。
    //要获取错误信息，请接收并检验RequestSender的返回值。解析器不会处理错误，所以输入解析器的字符串必须有效。
    public static class Deserializer
    {
        //将文本转为JArray
        public static JArray ToArray(string re)
        {
            try
            {
                var Posts= JsonConvert.DeserializeObject<JArray>(re);
                if (Posts != null && Posts.Count > 0)
                {
                    return Posts; // 返回解析后的JArray
                }
                else
                {
                    return null; // 返回null表示解析失败或无数据
                }
            }
            catch (Exception ex)
            {
                return null; // 返回null表示解析失败
            }
        }

        public static Dictionary<string, string> UserInfoList(string userinfo)
        {
            var PortList = JsonConvert.DeserializeObject<JArray>(userinfo);
            Dictionary<string, string> PortDict = new Dictionary<string, string>();
            if (PortList == null || PortList.Count == 0)
            {
                return PortDict; // 返回空字典
            }
            else
            {
                try
                {
                    foreach (var p in PortList)
                    {
                        var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(p.ToString());
                        if (info != null)
                        {
                            string purl = info["portraitUrl"].ToString();
                            string id = info["id"].ToString();
                            PortDict[id] = purl;
                        }
                    }
                    return PortDict; // 返回包含用户信息的字典
                }
                catch
                {
                    return null;
                }
            }
        }
        public static Dictionary<string, object> ToDictionary(string index)//将本地缓存或者在线数据转化为字典。
        {
            if (!index.StartsWith("10")&&(!index.StartsWith("404")))//10为文件系统错误类型。
            {
                try
                {
                    var IndexContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(index);
                    if (IndexContent != null)
                    {
                        return IndexContent;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static StandardPost ToItem(string TopicText)
        {
            //此函数专用于解析帖子，应对有无MediaType的情况，用于版面主页和新帖页。
            Dictionary<string, object> TopicProperty = JsonConvert.DeserializeObject<Dictionary<string, object>>(TopicText);
            if (TopicProperty != null)
            {
                string pid = "0";
                if (TopicProperty["id"] != null)
                {
                    pid= TopicProperty["id"].ToString();
                }
                string hit = TopicProperty["hitCount"].ToString();
                string title = TopicProperty["title"].ToString();
                string time = TopicProperty["time"].ToString();
                string reply = TopicProperty["replyCount"].ToString();
                string author = "@ 匿名";
                List<MediaContent> images = new();
                List<MediaContent> videos = new();
                if (TopicProperty["userName"] != null)
                {
                    author ="@ "+ TopicProperty["userName"].ToString();
                }
               
                if (TopicProperty.TryGetValue("mediaContent", out var value))
                {
                    if (value != null)
                    {
                        if (value.ToString() != null)
                        {
                            var js=JsonConvert.DeserializeObject<Dictionary<string,object>>(value.ToString());
                            if (js != null)
                            {
                                if (js.ContainsKey("thumbnail"))
                                {
                                    if (js["thumbnail"] != null)
                                    {
                                        var MediaList = JsonConvert.DeserializeObject<JArray>(js["thumbnail"].ToString());
                                        foreach (var media in MediaList)
                                        {
                                            string link = media.ToString();
                                            var result = LinkAnalyzer.LinkDefinite(link);
                                            if (result.Value == "image")
                                            {
                                                images.Add(new MediaContent { MediaType = "image", MediaSource = link });
                                            }
                                        }
                                    }

                                }
                            }
                            
                            
                        }
                    }
                }
                return new StandardPost { author = author, reply = reply, hit = hit, time = time, pid = pid, images=images,videos=videos, title = title };

            }
            else
            {
                return null;
            }
        }
    }
    //约定：检验HttpResponse的字符串内容是否为空。检验输入合法性。
    public static class ValidationHelper
    {
        public static bool IsValidResponse(string response)
        {
            if(string.IsNullOrEmpty(response))
            {
                 return false; // 返回false表示响应内容为空
            }
            else
            {
                if(response.StartsWith("404:"))
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
                    return "404：请求失败";
                }
            }
            catch
            {
                return "404:连接出错";
            }
        }
        public static bool IsTokenExist(ApplicationDataContainer container,string key)
        {
            if (container.Values.TryGetValue(key,out object token))
            {
                if (token != null)
                {
                    if (!string.IsNullOrEmpty(token.ToString()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static string JsonReader(string path)
        {
            if (File.Exists(path))
            {
                string content=File.ReadAllText(path);
                if(!string.IsNullOrEmpty(content))
                {
                    return content;
                }
                else
                {
                    return "100:空内容";
                }
            }
            else
            {
                return "101:不存在的文件";
            }

        }
        public static async void JsonWritter(string json,string filename)
        { 
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile file = await cacheFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, json);  
        }
        
    }

    public static class UBBConverter
    {
        public static string Convert(string ubbText, bool IsImageVisible, bool escapeMarkdown = false)
        {
            var text = Preprocess(ubbText);

            // 处理块级元素（优先级从高到低）
            text = ConvertCodeBlocks(text);
            text = ConvertQuotes(text);
            text = ConvertLists(text);

            // 处理行内元素
            text = ConvertImages(text, IsImageVisible);
            text = ConvertLinks(text);
            text = ConvertEmoji(text);
            text = ConvertColor(text);
            //text=ConvertBold(text);
            text = ConvertTextStyles(text);


            // 清理格式
            //text = Cleanup(text);

            return escapeMarkdown ? EscapeMarkdown(text) : text;
        }

        private static string Preprocess(string input)
        {
            return input.Replace("\r\n", "  \n")
                        .Replace("\r", "  \n")
                        .Replace("\n", "  \n")
                        .Replace("<br>", "  \n")
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
                    (@"\[ac(\d{2})\]","![#ac$1#](https://www.cc98.org/static/images/ac/$1.png)"),//ac娘
                    (@"\[em(\d{2})\]","![#em$1#](https://www.cc98.org/static/images/em/em$1.gif)"),//经典
                    (@"\[([a-zA-Z]{2})(\d{2})\]","![#$1$2#](https://www.cc98.org/static/images/$1/$1$2.png)"),//贴吧，雀魂
                    (@"\[cc98(\d{2})\]","![#cc98$1#](https://www.cc98.org/static/images/CC98/CC98$1.png)")//cc98

                };
            foreach (var (pattern, replacement) in replacements)
            {
                input = Regex.Replace(input, pattern, replacement,
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            return input;
        }
        private static string ConvertTextStyles(string input)
        {
            var replacements = new[]
        {

            (@"\[b\](.*?)\[/b\]", "**$1**"),
            (@"\[del\](.*?)\[/del\]", "~~$1~~"),
            (@"\[i\](.*?)\[/i\]", "*$1*"),
            (@"\[u\](.*?)\[/u\]", "$1"),
            (@"\[color=[^\]]*\](.*?)\[/color\]", "$1"),
            (@"\[font=.*?\](.*?)\[/font\]", "$1"),
            (@"\[size=\d{1,2}\]",""),
            (@"\[/size\]",""),
            (@"\[center\](.*?)\[/center\]","$1"),
            (@"\<center\>(.*?)\</center\>","$1"),
            (@"<p[^>]*>(.*?)</p>","$1"),
            (@"\[align=[^\]]+\](.*?)\[/align\]","$1"),
            (@"<img\s+[^>]*src=""([^""]+)""[^>]*>","[#**图片**#]($1)"),
            (@"@(\S+)\s","[@ $1 ](https://api.cc98.org/user/name/$1)"),
            (@"\[audio\](.*?)\[/audio\]","[#**音频**#]($1)"),
            (@"\[video\](.*?)\[/video\]","[#**视频**#]($1)"),
            (@"\[upload\](.*?)\[/upload\]","[#**文件**#]($1)")
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
        public static KeyValuePair<string, string> LinkDefinite(string link)
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
}


