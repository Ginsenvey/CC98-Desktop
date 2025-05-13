using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using CCkernel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using static App3.Index;
using static System.Net.WebRequestMethods;
using System.Net.Http;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.Devices.Power;
using System.Net.Http.Headers;
using Windows.Media.Protection.PlayReady;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Topic : Page
    {
        public ObservableCollection<Reply> replies;
        public MetaData metadata;
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public CCloginservice loginservice;
        
        public Topic()
        {
            this.InitializeComponent();
            replies = new ObservableCollection<Reply>() { };


            
            TileList.ItemsSource = replies;
            metadata = new MetaData()
            {
                reply = "0",
                favorite="0",
                hit="0",
                
            };
            MetaDataArea.DataContext = metadata;
            loginservice = new CCloginservice();
            

        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                Set.Values["CurrentTopicId"]=parameter;
                LoadMetaData(parameter);
                LoadReply(parameter,"0");
            }
            else
            {
                
            }
        }
        private async void LoadMetaData(string pid)
        {
            string access = Set.Values["Access"] as string;
            if (!string.IsNullOrEmpty(access))
            {
                loginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
                string MetaDataUrl = "https://api.cc98.org/topic/" + pid;
                var MetaDataRes = await loginservice.client.GetAsync(MetaDataUrl);
                if(MetaDataRes.StatusCode==System.Net.HttpStatusCode.OK)
                {
                    string MetaDataText=await MetaDataRes.Content.ReadAsStringAsync();
                    var js=JsonConvert.DeserializeObject<Dictionary<string,object>>(MetaDataText);
                    metadata.favorite = js["favoriteCount"].ToString();
                    metadata.text = js["title"].ToString();
                    metadata.time = js["time"].ToString();
                    metadata.hit = js["hitCount"].ToString();
                    metadata.reply = js["replyCount"].ToString();
                    if (metadata.reply != null)
                    {
                        Pager.NumberOfPages = (Convert.ToInt32(metadata.reply)/10)+1;
                    }
                    else
                    {
                        Pager.NumberOfPages=1;
                    }
                }
            }
        }
        private async void LoadReply(string pid,string start)
        {
            if (Set.Values["IsAcTive"] as string == "1")
            {
                
                string access = Set.Values["Access"] as string;
                if (!string.IsNullOrEmpty(access))
                {
                    string TileSequence = await loginservice.GetTopic(pid, access, start);
                    replies.Clear();
                    JArray TileArray=new JArray();
                    try 
                    {
                        TileArray = JsonConvert.DeserializeObject<JArray>(TileSequence);
                    }
                    catch
                    {
                        if(Set.Values.TryGetValue("Refresh",out var token))
                        {
                            if(token != null)
                            {
                                string newaccess=await loginservice.RefreshToken(token.ToString());
                                if (newaccess != "0")
                                {
                                    loginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newaccess);
                                    Set.Values["Access"] = newaccess;
                                    access = newaccess;
                                    TileSequence = await loginservice.GetTopic(pid, access, start);
                                    TileArray = JsonConvert.DeserializeObject<JArray>(TileSequence);
                                }
                            }
                            else
                            {
                                return;
                            }
                        }

                    }

                    
                    foreach (var ATile in TileArray)
                    {
                        string TileText = ATile.ToString();
                        var TilePair = JsonConvert.DeserializeObject<Dictionary<string, object>>(TileText);
                        string content = TilePair["content"].ToString();
                        string author = TilePair["userName"].ToString();
                        string time = TilePair["time"].ToString();
                        string rid = TilePair["id"].ToString();
                        string id = "0";
                        if (TilePair["userId"] != null)
                        {
                            id = TilePair["userId"].ToString();
                        }
                        string like = TilePair["likeCount"].ToString();
                        string dislike = TilePair["dislikeCount"].ToString();
                        replies.Add(new Reply { author = author, text = UBBToMarkdownConverter.Convert(content,false), like = like, dislike = dislike, rid = rid, time = time, uid = id });

                    }
                    RootViewer.ScrollToVerticalOffset(0);//滚动到一页的最上方。这个方法必须在帖子load结束之后调用，否则ui切换逻辑错误。

                }
            }
            else
            {

            }
        }

        public class Reply : INotifyPropertyChanged
        {
            private string _text;
            private string _like;
            private string _author;
            private string _rid;
            private string _dislike;
            private string _time;
            private string _uid;
            public string text
            {
                get => _text;
                set
                {
                    if (_text != value)
                    {
                        _text = value;
                        OnPropertyChanged(nameof(text));
                    }
                }
            }

            public string like
            {
                get => _like;
                set
                {
                    if (_like != value)
                    {
                        _like = value;
                        OnPropertyChanged(nameof(like));
                    }
                }
            }

            public string author

            {
                get => _author;
                set
                {
                    if (_author != value)
                    {
                        _author = value;
                        OnPropertyChanged(nameof(author));
                    }
                }
            }

            public string rid
            {
                get => _rid;
                set
                {
                    if (_rid != value)
                    {
                        _rid = value;
                        OnPropertyChanged(nameof(rid));
                    }
                }
            }

            public string dislike
            {
                get => _dislike;
                set
                {
                    if (_dislike != value)
                    {
                        _dislike = value;
                        OnPropertyChanged(nameof(dislike));
                    }
                }
            }

            public string time
            {
                get => _time;
                set
                {
                    if (_time != value)
                    {
                        _time = value;
                        OnPropertyChanged(nameof(time));
                    }
                }
            }

            public string uid
            {
                get => _uid;
                set
                {
                    if (_uid != value)
                    {
                        _uid = value;
                        OnPropertyChanged(nameof(uid));
                    }
                }
            }

            
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public class MetaData : INotifyPropertyChanged
        {
            private string _text;//标题
            private string _like;//赞
            private string _author;//楼主名
            private string _rid;//主题id
            private string _reply;//回帖数
            private string _favorite;//收藏数
            private string _time;//发帖时间
            private string _uid;//楼主id
            private string _dislike;//踩
            private string _isrealidvisible;//匿名与否
            private string _hit;//热度，点击量
            public string text
            {
                get => _text;
                set
                {
                    if (_text != value)
                    {
                        _text = value;
                        OnPropertyChanged(nameof(text));
                    }
                }
            }

            public string like
            {
                get => _like;
                set
                {
                    if (_like != value)
                    {
                        _like = value;
                        OnPropertyChanged(nameof(like));
                    }
                }
            }

            public string author

            {
                get => _author;
                set
                {
                    if (_author != value)
                    {
                        _author = value;
                        OnPropertyChanged(nameof(author));
                    }
                }
            }

            public string rid
            {
                get => _rid;
                set
                {
                    if (_rid != value)
                    {
                        _rid = value;
                        OnPropertyChanged(nameof(rid));
                    }
                }
            }

            public string reply
            {
                get => _reply;
                set
                {
                    if (_reply != value)
                    {
                        _reply = value;
                        OnPropertyChanged(nameof(reply));
                    }
                }
            }
            public string favorite
            {
                get => _favorite;
                set
                {
                    if (_favorite != value)
                    {
                        _favorite = value;
                        OnPropertyChanged(nameof(favorite));
                    }
                }
            }

            public string time
            {
                get => _time;
                set
                {
                    if (_time != value)
                    {
                        _time = value;
                        OnPropertyChanged(nameof(time));
                    }
                }
            }

            public string uid
            {
                get => _uid;
                set
                {
                    if (_uid != value)
                    {
                        _uid = value;
                        OnPropertyChanged(nameof(uid));
                    }
                }
            }

            public string dislike
            {
                get => _dislike;
                set
                {
                    if (_dislike != value)
                    {
                        _dislike = value;
                        OnPropertyChanged(nameof(dislike));
                    }
                }
            }

            public string isrealidvisible
            {
                get => _isrealidvisible;
                set
                {
                    if (_isrealidvisible != value)
                    {
                        _isrealidvisible = value;
                        OnPropertyChanged(nameof(isrealidvisible));
                    }
                }
            }

            public string hit
            {
                get => _hit;
                set
                {
                    if (_hit != value)
                    {
                        _hit = value;
                        OnPropertyChanged(nameof(hit));
                    }
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private async void portrait_Loaded(object sender, RoutedEventArgs e)
        {
            var port = sender as Image;
            if (port != null)
            {
                var tag = port.Tag as string;
                string url = "https://api.cc98.org/user/name/" + tag;
                using var client = new HttpClient();
                var PortRes = await client.GetAsync(url);
                if (PortRes.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string content = await PortRes.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        try
                        {
                            var Info = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                            string ImageUrl = Info["portraitUrl"].ToString();
                            var bitmap = new BitmapImage(new Uri(ImageUrl));
                            port.Source = bitmap;
                        }
                        catch
                        {
                            
                        }
                        
                    }
                }

            }


        }

        private void Person_Click(object sender, RoutedEventArgs e)
        {
            
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as Reply;
            if (t != null)
            {
                Set.Values["ProfileNaviMode"] = "Others";
                Set.Values["CurrentPerson"] = t.uid;
                if (t.uid != "0")
                {
                    Frame.Navigate(typeof(Profile),t.uid);
                }

            }
        }
        public static class LinkAnalyzer
        {
            public static KeyValuePair<string,string> LinkDefinite(string link)
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
                    else if (link.Contains("/topic/")&&(!link.Contains("#")))
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
                    else
                    {
                        return new KeyValuePair<string, string>("other",link);
                    }
                }
                else
                {
                    return new KeyValuePair<string,string>("null",link);
                }

            }
        }
        public static class UBBToMarkdownConverter
        {
            public static string Convert(string ubbText, bool IsImageVisible, bool escapeMarkdown = false)
            {
                var text = Preprocess(ubbText);

                // 处理块级元素（优先级从高到低）
                text = ConvertCodeBlocks(text);
                text = ConvertQuotes(text);
                text = ConvertLists(text);

                // 处理行内元素
                text = ConvertImages(text,IsImageVisible);
                text = ConvertLinks(text);
                text=ConvertEmoji(text);
                
                text = ConvertTextStyles(text);
                text=RemoveSizeTags(text);
                
                // 清理格式
                //text = Cleanup(text);

                return escapeMarkdown ? EscapeMarkdown(text) : text;
            }

            private static string Preprocess(string input)
            {
                return input.Replace("\r\n", "  \n")
                            .Replace("\r", " \n")
                            .Replace("\n", "  \n")
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
                string _output = Regex.Replace(input,
        @"\[quote\](.*?)\[/quote\]",
        m => {
            // 分割每行并在每行前添加 > 
            var lines = m.Groups[1].Value.Split('\n');
            List<string> _lines = new();
            foreach (var line in lines)
            {
                string _line = line + "  ";
                _lines.Add(_line);
            }
            string output = "> " + string.Join("\n> ", _lines) + "<引文>";
            return ConvertQuotes(output);
        },
        RegexOptions.Singleline | RegexOptions.IgnoreCase);

                return _output;
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

            private static string ConvertImages(string input,bool mode)
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
                    "<$1>",
                    RegexOptions.IgnoreCase);
            }
            private static string ConvertEmoji(string input)
            {

                string pattern = @"\[ac(\d{2})\]";
                string output1 = Regex.Replace(input, pattern, match =>
                {
                    string digits = match.Groups[1].Value;
                    return $"![#表情#](https://www.cc98.org/static/images/ac/{digits}.png)";
                });

                string pattern1 = @"\[([a-zA-Z]{2})(\d{2})\]";
                string output2 = Regex.Replace(output1, pattern1, match =>
                {
                    // 提取字母和数字部分
                    string letters = match.Groups[1].Value;
                    string digits = match.Groups[2].Value;
                    return $"![#表情#](https://www.cc98.org/static/images/{letters}/{letters}{digits}.png)";
                });
                return output2;
            }
            private static string ConvertTextStyles(string input)
            {
                var replacements = new[]
            {
            
            (@"\[b\](.*?)\[/b\]", "**$1**"),
            (@"\[del\](.*?)\[/del\]", "~~$1~~"),
            (@"\[i\](.*?)\[/i\]", "*$1*"),
            (@"\[u\](.*?)\[/u\]", "$1"),
            (@"\[color=.*?\](.*?)\[/color\]", "$1"),
            (@"\[font=.*?\](.*?)\[/font\]", "$1"),
            (@"\[size=.*?\](.*?)\[/size\]", "$1"),
            (@"\[center\](.*?)\[/center\]","$1  "),
            (@"\[align=[^\]]+\](.*?)\[/align\]","$1  "),
            (@"@(\S+)\s","[@ $1 ](https://api.cc98.org/user/name/$1)")
        };

                foreach (var (pattern, replacement) in replacements)
                {
                    input = Regex.Replace(input, pattern, replacement,
                        RegexOptions.Singleline | RegexOptions.IgnoreCase);
                }

                return input;
            }
            static string RemoveSizeTags(string input)
            {
                // 正则表达式：匹配 [size=xx] 标签及其内容
                string pattern = @"\[size=\d{1,2}\](.*?)\[/size\]";

                // 使用正则替换，递归地去除最内层的 [size] 标签
                string output = Regex.Replace(input, pattern, match =>
                {
                    // 仅保留标签内的内容
                    return match.Groups[1].Value;
                });

                // 如果替换后仍然存在 [size] 标签，递归调用此方法继续替换
                if (output != input)
                {
                    return RemoveSizeTags(output);
                    // 继续处理外层的标签
                }

                return output;  // 返回最终结果
            }
            private static string Cleanup(string input)
            {
                // 合并多余空行
                return Regex.Replace(input, @"\n{3,}", "\n\n");
            }

            private static string EscapeMarkdown(string input)
            {
                var charsToEscape = new[] { '\\', '_',  '+', '-', '.' };
                return charsToEscape.Aggregate(input, (current, c) =>
                    current.Replace(c.ToString(), $"\\{c}"));
            }
        }

        private  void Pager_SelectedIndexChanged(PagerControl sender, PagerControlSelectedIndexChangedEventArgs args)
        {
            int index = Pager.SelectedPageIndex;
            if (index >= 0)
            {
                int startindex = 10 * (index);
                LoadReply(Set.Values["CurrentTopicId"] as string, startindex.ToString());
                
            }
            
        }

        

        private void MarkdownTextBlock_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            var url = e.Link.ToString();
            if (url.Contains("webp"))
            {
                
                var bitmap = new BitmapImage(new Uri(url));
                BuiltInViewer.Source = bitmap;
                viewer.IsOpen = true;
            }
            else
            {
                var result = LinkAnalyzer.LinkDefinite(url);
                if (result.Key == "topic")
                {
                    LoadMetaData(result.Value);
                    LoadReply(result.Value, "0");
                }
            }
        }
        

        private void writereply_Click(object sender, RoutedEventArgs e)
        {
            var param = new Dictionary<string, string>()
            {
                {"Mode","0"},//回复主题为0，回帖为1，发主题、投票为2
                {"Pid",Set.Values["CurrentTopicId"] as string },
                
            };
            Frame.Navigate(typeof(Post), param);
        }
    }
}
