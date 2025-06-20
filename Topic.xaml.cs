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
using System.Text;
using CommunityToolkit.WinUI.UI.Controls;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.ApplicationModel.DataTransfer;
using FluentIcons.Common;
using System.Net;
using Windows.ApplicationModel.Contacts;
using static App3.Message;
using DevWinUI;
using ABI.Microsoft.UI.Xaml.Media.Animation;
using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;




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
            metadata = new MetaData()
            {
                reply = "0",
                favorite="0",
                hit="0",
                
            };
            MetaDataArea.DataContext = metadata;
            loginservice = new CCloginservice();
            Unloaded += Topic_Unloaded;

        }

        private void Topic_Unloaded(object sender, RoutedEventArgs e)
        {
            if(_mediaPlayer!= null)
            {
                _mediaPlayer.Dispose();
                TileList.ItemsSource = null;
            }
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
                LoadReply(Set.Values["CurrentTopicId"] as string, "0", IsImageVisible);
            }
            else
            {
                
            }
        }
        private void Stats(string board)
        //统计板块访问次数，作为用户体验模型的一部分。
        {
            if (!Set.Values.ContainsKey("Stats"))
            {
                Dictionary<string, int> stats = new Dictionary<string, int>();
                stats[board] = 1;
                string statsjson = JsonConvert.SerializeObject(stats);
                Set.Values["Stats"] = statsjson;
            }
            else
            {
                string _statsjson = Set.Values["Stats"].ToString();
                if (_statsjson != null)
                {
                    var stats = JsonConvert.DeserializeObject<Dictionary<string, int>>(_statsjson);
                    if(stats != null)
                    {
                        if (stats.ContainsKey(board))
                        {
                            stats[board]++;
                        }
                        else
                        {
                            stats[board] = 1;
                        }
                        string statsjson = JsonConvert.SerializeObject(stats);
                        Set.Values["Stats"] = statsjson;
                    }
                    
                }
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
                    string favourl = "https://api.cc98.org/topic/"+pid+"/isfavorite";
                    var favourres = await loginservice.client.GetAsync(favourl);
                    
                    if (favourres.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string content = await favourres.Content.ReadAsStringAsync();
                        if (content == "true")
                        {
                            metadata.variant = IconVariant.Color;
                        }
                        else
                        {
                            metadata.variant = IconVariant.Regular;
                        }
                    }
                    else
                    {
                        metadata.variant = IconVariant.Regular;
                    }
                    
                    string boardid = js["boardId"].ToString();
                    if (boardid != null)
                    {
                        Stats(boardid);
                    }
                    
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
        public bool IsImageVisible = false;
        private async void LoadReply(string pid, string start,bool mode)
        {

            if (Set.Values["IsAcTive"] as string == "1")
            {
                string access = Set.Values["Access"] as string;
                if (!string.IsNullOrEmpty(access))
                {
                    string TileSequence = await loginservice.GetTopic(pid, access, start);
                    JArray TileArray = new JArray();
                    try
                    {
                        TileArray = JsonConvert.DeserializeObject<JArray>(TileSequence);
                    }
                    catch
                    {
                        if (Set.Values.TryGetValue("Refresh", out var token))
                        {
                            if (token != null)
                            {
                                string newaccess = await loginservice.RefreshToken(token.ToString());
                                if (newaccess != "0")
                                {
                                    loginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newaccess);
                                    Set.Values["Access"] = newaccess;
                                    access = newaccess;
                                    try
                                    {
                                        TileSequence = await loginservice.GetTopic(pid, access, start);
                                        TileArray = JsonConvert.DeserializeObject<JArray>(TileSequence);
                                    }
                                    catch (Exception ex)
                                    {
                                        return;//无法鉴权的情况，或者链接有误/网络问题。
                                        //已知问题：markdowntextblock错误解析链接，导致传入参数错误。
                                        //方案：对导航参数进行正则匹配，确保是合法的链接。同时，改进链接分析器的功能。

                                    }

                                }
                            }
                            else
                            {
                                return;
                            }
                        }

                    }

                    replies.Clear();
                    TileList.ItemsSource = null;//清除绑定，防止重复数据。此处必须在TileList.ItemsSource赋值之前调用，否则会导致数据重复绑定。
                    List<string> users = new();
                    List<Reply> ReplyList = new List<Reply>();
                    string hideurl = "/Assets/hide.gif";
                    foreach (var ATile in TileArray)
                    {
                        string TileText = ATile.ToString();
                        var TilePair = JsonConvert.DeserializeObject<Dictionary<string, object>>(TileText);
                        string content = TilePair["content"].ToString();
                        string author = "匿名";
                        string time = TilePair["time"].ToString();
                        string rid = TilePair["id"].ToString();
                        string id = "0";
                        if (TilePair["userId"] != null)
                        {
                            id = TilePair["userId"].ToString();
                            author = TilePair["userName"].ToString();
                        }
                        string floor = TilePair["floor"].ToString();
                        string like = TilePair["likeCount"].ToString();
                        string dislike = TilePair["dislikeCount"].ToString();
                        
                        if (author != "null" && author != null&&id!="0")
                        {
                            users.Add("id="+id);
                        }
                        
                        ReplyList.Add(new Reply { author = author, text = UBBToMarkdownConverter.Convert(content, mode), like = like, dislike = dislike, rid = rid, time = time, uid = id, floor=floor+"L" ,url=hideurl});
                    }
                    
                    if (users.Count > 0)
                    {
                        
                        string param = string.Join("&", users);
                        string url = "https://api.cc98.org/user/basic?" + param;
                        using var client = new HttpClient();
                        var PortRes = await client.GetAsync(url);
                        if (PortRes.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string content = await PortRes.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(content))
                            {
                                try
                                {
                                    var portlist = JsonConvert.DeserializeObject<JArray>(content);
                                    Dictionary<string,string> PortDict = new Dictionary<string, string>();
                                    foreach (var p in portlist)
                                    {
                                        var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(p.ToString());
                                        string purl = info["portraitUrl"].ToString();
                                        string id = info["id"].ToString();
                                        //此处创建一个以id为key的字典，存储头像信息。
                                        PortDict[id] = purl;
                                    }
                                    //此处有一个玄学问题，上方已经运行过一次Clear方法，为什么还会有重复数据？原因是TileList.ItemsSource的绑定没有被清除掉，导致TileList.ItemsSource在下一次加载时仍然保留了上一次的引用。解决方法是每次加载前先清空replies集合，然后重新绑定。
                                    foreach (Reply r in ReplyList)
                                    {
                                        if (PortDict.ContainsKey(r.uid))
                                        {
                                            r.url = PortDict[r.uid];
                                        }
                                       
                                        replies.Add(r);
                                    }
                                }
                                catch(Exception ex)
                                {
                                    replies.Add(new Reply { text="处理帖子出错:"+ex.Message});
                                }
                            }
                        }
                    }
                    else
                    {
                        
                        foreach (Reply r in ReplyList)
                        {
                            replies.Add(r);
                        }
                    }
                    TileList.ItemsSource = replies;
                    
                    RootViewer.ScrollToVerticalOffset(0);//滚动到一页的最上方。这个方法必须在帖子load结束之后调用，否则ui切换逻辑错误。
                }
                else
                {
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
                    Frame.Navigate(typeof(Profile), t.uid);
                }

            }
        }
        public int CurrentPage = 0;
        
        private void Pager_SelectedIndexChanged(DevWinUI.PagerControl sender, DevWinUI.PagerControlSelectedIndexChangedEventArgs args)
        {
            //此方法在页面加载完成后会被调用一次，Pager的SelectedIndex会被设置为0。
            //所以页面构造函数处不需要单独调用LoadReply方法。
            //限定了只有页面主动加载和用户点击翻页，index从-1到0不触发数据加载。这似乎是pager的bug.
            if (args.PreviousPageIndex != -1)
            {
                int index = Pager.SelectedPageIndex;
                CurrentPage = index;

                if (index >= 0)
                {
                    int startindex = 10 * (index);
                    LoadReply(Set.Values["CurrentTopicId"] as string, startindex.ToString(), IsImageVisible);

                }
            }
            

        }

        private MediaPlayer _mediaPlayer;
        private void InitializeMediaPlayer()
        {
            _mediaPlayer = new MediaPlayer
            {
                AutoPlay = true,
                Volume = 0.8 // 默认音量 (0.0 ~ 1.0)
            };

            // 监听关键事件
            
        }
        private async void MarkdownTextBlock_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            var url = e.Link.ToString();
            var result = LinkAnalyzer.LinkDefinite(url);
            switch (result.Key)
            {
                case "topic":
                    LoadMetaData(result.Value);
                    LoadReply(result.Value, "0", IsImageVisible);
                    break;
                case "user":
                    {
                        string _url = "https://api.cc98.org/user/name/" + url;
                        using var client = new HttpClient();
                        var PortRes = await client.GetAsync(_url);
                        if (PortRes.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string content = await PortRes.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(content))
                            {
                                try
                                {
                                    var Info = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                                    string uid = Info["id"].ToString();
                                    //de.Text = uid;
                                    Set.Values["ProfileNaviMode"] = "Others";
                                    Frame.Navigate(typeof(Profile), uid);
                                }
                                catch (Exception ex)
                                {
                                    //de.Text = ex.Message;
                                }
                            }
                            else
                            {
                                //de.Text = "空返回";
                            }
                        }
                        break;
                    }
                    //using语句不能在switch语句中直接出现。因此，使用大括号包围这个case.
                case "anchor":
                    string pattern = @"/topic/(\d{7})/(\d+)#(\d+)";
                    Regex regex = new Regex(pattern);

                    // 使用正则表达式进行匹配
                    Match match = regex.Match(url);

                    if (match.Success)
                    {
                        // 输出匹配的内容
                        string beforeHash = match.Groups[2].Value;  // #前面的数字
                        string afterHash = match.Groups[3].Value;   // #后面的数字
                        int page = Convert.ToInt32(beforeHash);
                        int floor = Convert.ToInt32(afterHash);
                        try
                        {
                            if (Pager.SelectedPageIndex + 1 == page && floor > 0)
                            {
                                GoTo(floor - 1);
                            }
                            else
                            {

                                Pager.SelectedPageIndex = page - 1;
                                GoTo(floor - 1);
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    break;
                case "file":
                    if (result.Value == "image")
                    {
                        try
                        {
                            var param = new Dictionary<string, string>()
                            {
                                {"url",url },
                                {"type","image" }
                             };
                            var picviewer = new MediaViewer(param);
                            picviewer.Activate();
                        }
                        catch
                        {

                        }
                    }
                    else if (result.Value == "audio")
                    {
                        AudioPlayer.Visibility = Visibility.Visible;
                        InitializeMediaPlayer();
                        AudioName.Text = url;
                        try
                        {
                            var mediaSource = MediaSource.CreateFromUri(new Uri(url));
                            // 设置媒体源
                            _mediaPlayer.Source = mediaSource;
                            // 开始播放
                            _mediaPlayer.Play();
                            Play.Visibility = Visibility.Collapsed;
                            Pause.Visibility = Visibility.Visible;
                        }
                        catch (Exception ex)
                        {
                            AudioName.Text = ex.Message;
                        }
                    }
                    else if (result.Value == "video")
                    {
                        var param = new Dictionary<string, string>()
                            {
                                {"url",url },
                                {"type","video" }
                             };
                        var picviewer = new MediaViewer(param);
                        picviewer.Activate();
                    }
                    else if (result.Value == "doc")//无法预览的媒体文件类
                    {
                        var Operation=await DownLoadDialog.ShowAsync();
                        if (Operation == ContentDialogResult.Primary)
                        {
                            
                            string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            string DownloadsFolder = System.IO.Path.Combine(UserProfile, "Downloads");
                            string DownloadLocation = "";
                            string filepattern = @"(?<=https://file\.cc98\.org/v4-upload/d/\d{4}/\d{4}/)[^/]+";

                            // 创建正则表达式对象
                            Regex fileregex = new Regex(filepattern);

                            // 执行匹配
                            Match filematch = fileregex.Match(url);
                            if(filematch.Success)
                            {
                                DownloadLocation = DownloadsFolder+"\\"+filematch.Value;
                            }
                            else
                            {
                                DownloadLocation = System.IO.Path.Combine(DownloadsFolder, "CC98_Download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
                            }
                            ShowTips("开始下载附件到:", DownloadLocation);
                            if (!System.IO.File.Exists(DownloadsFolder))
                            {

                                try
                                {
                                    var client = new HttpClient();
                                    var fileres = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                                    if (fileres.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream contentStream = await fileres.Content.ReadAsStreamAsync(),
                                        fileStream = new FileStream(DownloadLocation, FileMode.Create, FileAccess.Write, FileShare.None))
                                        {
                                            await contentStream.CopyToAsync(fileStream);
                                            ShowTips("下载完成:", DownloadLocation);
                                        }
                                    }
                                }
                                catch(Exception ex)
                                {
                                    ShowTips("下载出错:", ex.Message);
                                }


                            }
                        }
                        
                    }
                    break;
                default://自动复制到用户剪切板
                    var datapackage = new DataPackage();
                    datapackage.SetText(url);
                    Clipboard.SetContent(datapackage);
                    break;
            }
  
        }

        private void ShowTips(string title,string content)
        {
            if (msg.IsOpen == true)
            {
                msg.IsOpen = false;
            }
            msg.Title = title;
            msg.Content = content;
            msg.IsOpen = true;
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

        private async void TileFlyout_Click(object sender, RoutedEventArgs e)
        {
            var m= sender as MenuFlyoutItem;
            if (m != null)
            {
                var tag = m.Tag as string;
                if (tag == "0")
                {
                    LoadMetaData(Set.Values["CurrentTopicId"] as string);
                }
                else if (tag == "1")
                {
                    string shareurl = "https://www.cc98.org/topic/" + Set.Values["CurrentTopicId"] as string;
                    var datapackage=new DataPackage();
                    datapackage.SetText(shareurl);
                    Clipboard.SetContent(datapackage); 
                }
                
            }
            else
            {
            }
            
        }
        private async void CollectionMenu_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var m = sender as MenuFlyoutSubItem;
            if (m != null)
            {
                m.Items.Clear();
                string LikeUrl = "https://api.cc98.org/me/favorite-topic-group";
                try
                {
                    var LikeRes = await loginservice.client.GetAsync(LikeUrl);
                    if (LikeRes.StatusCode == HttpStatusCode.OK)
                    {

                        string LikeText = await LikeRes.Content.ReadAsStringAsync();
                        var likes = JsonConvert.DeserializeObject<Dictionary<string, object>>(LikeText);
                        var LikeList = JsonConvert.DeserializeObject<JArray>(likes["data"].ToString());

                        foreach (var like in LikeList)
                        {
                            var likeinfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(like.ToString());
                            string collection = likeinfo["name"].ToString();
                            string sortid = like["id"].ToString();
                            var s = new MenuFlyoutItem { Text = collection, Tag = sortid, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.List } };
                            s.Click += CollectionItem_Click;
                            m.Items.Add(s);
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    m.Items.Add(new MenuFlyoutItem { Text = "获取收藏夹失败:" + ex.Message, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.CloudError } });
                }

            }
        }

        private async void CollectionItem_Click(object sender, RoutedEventArgs e)
        {
            var m = sender as MenuFlyoutItem;
            if (m != null)
            {
                var t = m.Tag as string;
                if (!string.IsNullOrEmpty(t))
                {
                    string favoriteurl = "https://api.cc98.org/me/favorite/" + (Set.Values["CurrentTopicId"] as string) + "?groupid=" + t;
                    var request = new HttpRequestMessage(HttpMethod.Put, favoriteurl);
                    var content = new StringContent("", Encoding.UTF8, "application/json");
                    request.Content = content;//按照此格式发送空的post请求并设置请求头
                    var res = await loginservice.client.SendAsync(request);
                    de.Text = res.StatusCode.ToString();
                    if (res.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        metadata.variant = IconVariant.Color;
                        LoadMetaData(Set.Values["CurrentTopicId"] as string);
                    }
                }
            }
        }
        private async void CollectionMenu_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var m = sender as MenuFlyoutSubItem;
            if (m != null)
            {
                m.Items.Clear();
                string LikeUrl = "https://api.cc98.org/me/favorite-topic-group";
                try
                {
                    var LikeRes = await loginservice.client.GetAsync(LikeUrl);
                    if (LikeRes.StatusCode == HttpStatusCode.OK)
                    {

                        string LikeText = await LikeRes.Content.ReadAsStringAsync();
                        var likes = JsonConvert.DeserializeObject<Dictionary<string, object>>(LikeText);
                        var LikeList = JsonConvert.DeserializeObject<JArray>(likes["data"].ToString());

                        foreach (var like in LikeList)
                        {
                            var likeinfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(like.ToString());
                            string collection = likeinfo["name"].ToString();
                            string sortid = (Convert.ToInt16(like["id"]) + 100).ToString();
                            var s = new MenuFlyoutItem { Text = collection, Tag = sortid, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.List } };
                            s.Click += CollectionItem_Click;
                            m.Items.Add(s);

                        }
                    }
                }
                catch (Exception ex)
                {
                    m.Items.Add(new MenuFlyoutItem { Text = "获取收藏夹失败:" + ex.Message, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.CloudError } });
                }
                //【issue】收藏集不应在每次进入topic页面时都获取。从节省资源的角度，只在应用启动时获取一次即可。
            }
        }
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    _mediaPlayer.Pause();
                    Play.Visibility = Visibility.Visible;
                    Pause.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
                {
                    _mediaPlayer.Play();
                    Play.Visibility = Visibility.Collapsed;
                    Pause.Visibility = Visibility.Visible;
                }

            }
        }

        private void RePlay_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Pause();
                _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            AudioPlayer.Visibility = Visibility.Collapsed;
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Pause();
                _mediaPlayer.Dispose();
            }
        }

        private void GoTo(int index)
        {
            
            var element = TileList.GetOrCreateElement(index);
            var options = new BringIntoViewOptions
            {
                VerticalAlignmentRatio = 0.5, // 0=顶部对齐，0.5=居中，1=底部
                AnimationDesired = true       // 启用平滑滚动动画
            };
            element.StartBringIntoView(options);
            //对于没有页码的跳转链接暂时没有处理
        }
        private void Drawer_ImageClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            string ImageUrl = e.Link.ToString();
            var param = new Dictionary<string, string>()
{
    {"url",ImageUrl },
    {"type","image" }
};
            var picviewer = new MediaViewer(param);
            picviewer.Activate();

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
                            return new KeyValuePair<string, string>("anchor",link );//返回索引楼层
                        }
                        return new KeyValuePair<string, string>("null", link);
                    }
                    else if(link.Contains("file"))
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
                                return new KeyValuePair<string, string> ( "file", "audio" );
                            }
                            else if (videofromats.Contains(ext))
                            {
                                return new KeyValuePair<string, string> ("file","video" );
                            }
                            else
                            {
                                return new KeyValuePair<string, string> ("file", "doc");
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
        public class Reply : INotifyPropertyChanged
        {
            private string _text;
            private string _like;
            private string _author;
            private string _rid;
            private string _dislike;
            private string _time;
            private string _uid;
            private string _url;
            private string _floor;//楼层数
            
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
            public string url
            {
                get => _url;
                set
                {
                    if (_url != value)
                    {
                        _url = value;
                        OnPropertyChanged(nameof(url));
                    }
                }
            }
            public string floor
            {
                get => _floor;
                set
                {
                    if (_floor != value)
                    {
                        _floor = value;
                        OnPropertyChanged(nameof(floor));
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
            private IconVariant _variant;
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
            public IconVariant variant
            {
                get => _variant;
                set
                {
                    if (_variant != value)
                    {
                        _variant = value;
                        OnPropertyChanged(nameof(variant));
                    }
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        
        public static class UBBToMarkdownConverter
        {
            public static string Convert(string ubbText, bool IsImageVisible, bool escapeMarkdown =false)
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
                text=ConvertColor(text);
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
                            .Replace("<br>","  \n")
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
                output.Append("  \n"+input.Substring(lastIndex));


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
                var charsToEscape = new[] { '\\', '_',  '+', '-', '.' };
                return charsToEscape.Aggregate(input, (current, c) =>
                    current.Replace(c.ToString(), $"\\{c}"));
            }
        }

        
    }
}
