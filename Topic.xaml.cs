using CCkernel;
using CCUserModel;
using CommunityToolkit.WinUI.UI.Controls;
using DevWinUI;
using FluentIcons.Common;
using FluentIcons.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.SmartCards;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using Windows.Storage.Streams;
using static App3.Index;
using static App3.Message;
using static App3.Profile;
using static App3.Topic;
using static System.Net.WebRequestMethods;




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



        public Topic()
        {
            this.InitializeComponent();

            replies = new ObservableCollection<Reply>() { };
            metadata = new MetaData()
            {
                reply = "0",
                favorite = "0",
                hit = "0",

            };
            MetaDataArea.DataContext = metadata;
            LoadSet();
            TileList.ItemsSource = replies;
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (_mediaPlayer != null)
            {
                _mediaPlayer.Dispose();

            }

            TileList.ItemsSource = null;
            replies.Clear();
            TileList = null;

            MetaDataArea.DataContext = null;
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                Set.Values["CurrentTopicId"] = parameter;
                LoadMetaData(parameter);
                LoadReply(parameter, "0");
                LoadFavorites();
            }
            else
            {

            }
        }
        private void LoadSet()
        {
            string _IsImageVisible = ValidationHelper.IsTokenExist(Set, "IsImageVisible");
            if (_IsImageVisible == "0")
            {
                Set.Values["IsImageVisible"] = "2";   //此时默认为不可见
            }
            
        }
        
        private void LoadFavorites()
        {
            var f = ValidationHelper.IsTokenExist(Set, "Favorites");
            if (f!="0")
            {
                var LikeList = JsonConvert.DeserializeObject<JArray>(f);
                foreach (var like in LikeList)
                {
                    try
                    {
                        var likeinfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(like.ToString());
                        string collection = likeinfo["name"].ToString();
                        string sortid = like["id"].ToString();
                        var m=new MenuFlyoutItem { Text = collection, Tag = sortid, Icon =new FluentIcons.WinUI.SymbolIcon{ Symbol = FluentIcons.Common.Symbol.Tag } };
                        m.Click += CollectionItem_Click;
                        CollectionMenu.Items.Add(m);
                    }
                    catch (Exception ex)
                    {
                        Flower.PlayAnimation("\uEA39", "出错。反馈此问题。");
                    }
                    
                }
            }
            else
            {
                Flower.PlayAnimation("\uEA39", "出错。反馈此问题。");
            }
        }
        private void Stats(string board)
        //统计板块访问次数，作为用户体验模型的一部分。
        {
            if (ValidationHelper.IsTokenExist(Set,"Stats")=="0")
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
                    if (stats != null)
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
            string MetaDataUrl = "https://api.cc98.org/topic/" + pid;
            string MetaData = await RequestSender.SimpleRequest(MetaDataUrl);
            var js = Deserializer.ToDictionary(MetaData);
            if(js != null)
            {
                metadata.favorite = js["favoriteCount"].ToString();
                metadata.text = js["title"].ToString();
                metadata.time = js["time"].ToString();
                metadata.hit = js["hitCount"].ToString();
                metadata.reply = js["replyCount"].ToString();
                string favourl = "https://api.cc98.org/topic/" + pid + "/isfavorite";
                string content = await RequestSender.SimpleRequest(favourl);
                if (content == "true")
                {
                    metadata.variant = IconVariant.Color;
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
                    Pager.NumberOfPages = (Convert.ToInt32(metadata.reply) / 10) + 1;
                    PagerFix();
                }
                else
                {
                    Pager.NumberOfPages = 1;
                    Pager.NextButtonVisibility = DevWinUI.PagerControlButtonVisibility.Hidden;
                }
            }
            else
            {
                Flower.PlayAnimation("\uEA39", MetaData);
            }
            
        }
        public bool IsImageVisible = false;
        public bool IsTracing=false;
        int Reply_Retry = 0;
        List<Reply> ReplyList = new List<Reply>();
        JArray Posts = new JArray();
        List<string> users = new();
        private async void LoadReply(string pid, string start)
        {
            Reply_Retry++;
            replies.Clear();
            ReplyList.Clear();
            Posts.Clear();
            users.Clear();
            string PostText = await RequestSender.TopicReply(pid, start);
            if (ValidationHelper.IsValidResponse(PostText))//回复有效
            {
                Posts = Deserializer.ToArray(PostText);
            }
            else//回复无效，重试一次。
            {
                if (PostText.StartsWith("404:"))
                {
                    if (Reply_Retry < 3)
                    {
                        LoadReply(pid, start);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;//边缘情况，理论上不存在此分支
                }
            }

            if (Posts != null)
            {
                if (Posts.Count > 0)
                {
                    
                   //清除绑定，防止重复数据。此处必须在TileList.ItemsSource赋值之前调用，否则会导致数据重复绑定。  
                    string hideurl = "ms-appx:///Assets/hide.gif";
                    foreach (var Tile in Posts)
                    {
                        string TileText = Tile.ToString();
                        var TilePair = JsonConvert.DeserializeObject<Dictionary<string, object>>(TileText);
                        string content = TilePair["content"].ToString();
                        string author = "匿名";
                        string time = TilePair["time"].ToString();
                        string rid = TilePair["id"].ToString();
                        string id = "0";//如果是匿名模式，id值为0
                        if (TilePair["userId"] != null)
                        {
                            id = TilePair["userId"].ToString();
                            author = TilePair["userName"].ToString();
                        }
                        else//在删帖的情况下，cc98deleter的id和name都是null
                        {
                            string user_name = ValidationHelper.GetKey(TilePair, "userName");
                            if ( user_name!= "0")
                            {
                                author = "匿名 " + user_name.ToUpper();
                            }
                            else
                            {
                                author = "CC98 Deleter";
                                content = "<--该回复已被管理员或发布者删除-->";
                            }
                            
                        }
                        string floor = TilePair["floor"].ToString();
                        string like = TilePair["likeCount"].ToString();
                        string dislike = TilePair["dislikeCount"].ToString();
                        string likestate = TilePair["likeState"].ToString();
                        IconVariant variant1 = IconVariant.Regular;
                        IconVariant variant2 = IconVariant.Regular;
                        if (likestate == "1")
                        {
                            variant1 = IconVariant.Filled;
                            variant2 = IconVariant.Regular;
                        }
                        else if (likestate == "2")
                        {
                            variant1 = IconVariant.Regular;
                            variant2 = IconVariant.Filled;
                        }
                        else
                        {
                            variant1 = IconVariant.Regular;
                            variant2 = IconVariant.Regular;
                        }
                        if (author != "null" && author != null && id != "0")
                        {
                            users.Add("id=" + id);
                        }
                        ReplyList.Add(new Reply { author = author, text = content, like = like, dislike = dislike, rid = rid, time = time, uid = id, floor = floor + "L", url = hideurl, likestate = variant1, dislikestate = variant2 });
                    }
                    if (users.Count > 0)
                    {
                        string _SimpleUseInfo = await RequestSender.SimpleUserInfo(users);
                        Dictionary<string, string> PortDict = Deserializer.UserInfoList(_SimpleUseInfo);
                        //此处有一个玄学问题，上方已经运行过一次Clear方法，为什么还会有重复数据？原因是TileList.ItemsSource的绑定没有被清除掉，导致TileList.ItemsSource在下一次加载时仍然保留了上一次的引用。解决方法是每次加载前先清空replies集合，然后重新绑定。
                        if (PortDict != null)
                        {
                            foreach (Reply r in ReplyList)
                            {
                                if (PortDict.ContainsKey(r.uid))
                                {
                                    r.url = PortDict[r.uid];
                                }

                                replies.Add(r);
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
                    RootViewer.ScrollToVerticalOffset(0);//滚动到一页的最上方。这个方法必须在帖子load结束之后调用，否则ui切换逻辑错误。
                }
            }
            else
            {
                return;//posts被赋值null，获取失败
            }

        }


        private void Person_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as Reply;
            if (t != null)
            {
                if (t.uid != "0")//非匿名才会跳转
                {
                    var param = new Dictionary<string, string>()
                        {
                            {"Mode","Others" },
                            {"UserId",t.uid }
                        };
                    Frame.Navigate(typeof(Profile), param);
                }

            }
        }
        public int CurrentPage = 0;
        public int CurrentTracingPage = 0;
        public string TracingPostId = "";
        private void Pager_SelectedIndexChanged(DevWinUI.PagerControl sender, DevWinUI.PagerControlSelectedIndexChangedEventArgs args)
        {
            //此方法在页面加载完成后会被调用一次，Pager的SelectedIndex会被设置为0。
            //所以页面构造函数处不需要单独调用LoadReply方法。
            //限定了只有页面主动加载和用户点击翻页，index从-1到0不触发数据加载。这似乎是pager的bug.
            PagerFix();
            if (args.PreviousPageIndex != -1)
            {
                if (!IsTracing)
                {
                    int index = Pager.SelectedPageIndex;
                    CurrentPage = index;

                    if (index >= 0)
                    {
                        
                        int startindex = 10 * (index);
                        LoadReply(Set.Values["CurrentTopicId"] as string, startindex.ToString());
                        
                    }
                }
                else
                {
                    int index = Pager.SelectedPageIndex;
                    CurrentTracingPage = index;
                    if (index >= 0)
                    {
                        
                        int startindex = 10 * (index);
                        LoadTrace(TracingPostId, startindex.ToString());

                    }
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
                    Set.Values["CurrentTopicId"]=result.Value;
                    LoadMetaData(result.Value);
                    LoadReply(result.Value, "0");
                    break;
                case "user":
                    {
                        string _url = "https://api.cc98.org/user/name/" + result.Value;
                        string infotext = await RequestSender.SimpleRequest(_url);
                        
                        if (!infotext.StartsWith("404:"))
                        {
                            var Info = Deserializer.ToDictionary(infotext);
                            if (Info != null)
                            {
                                string uid = Info["id"].ToString();
                                if (uid != null)
                                {
                                    if (uid.All(char.IsDigit))
                                    {
                                        var param = new Dictionary<string, string>()
                                        {
                                            {"Mode","Others" },
                                            {"UserId",uid }
                                        };
                                        Frame.Navigate(typeof(Profile), param);
                                    }
                                }
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
                case "board":
                    Frame.Navigate(typeof(Board), result.Value);
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
                        var Operation = await DownLoadDialog.ShowAsync();
                        if (Operation == ContentDialogResult.Primary)
                        {

                            string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            string DownloadsFolder = System.IO.Path.Combine(UserProfile, "Downloads");
                            string DownloadLocation = "";
                            string filepattern = @"(?<=https://file\.cc98\.org/v4-upload/d/\d{4}/\d{4}/)[^/]+";
                            Regex fileregex = new Regex(filepattern);
                            Match filematch = fileregex.Match(url);
                            if (filematch.Success)
                            {
                                DownloadLocation = DownloadsFolder + "\\" + filematch.Value;
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
                                    string target_url=CCloginservice.vpn.IsVpnEnabled?VpnService.ConvertUrl(url) : url;
                                    var fileres = await CCloginservice.vpn.client.GetAsync(target_url, HttpCompletionOption.ResponseHeadersRead);
                                    if (fileres.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream contentStream = await fileres.Content.ReadAsStreamAsync(),
                                        fileStream = new FileStream(DownloadLocation, FileMode.Create, FileAccess.Write, FileShare.None))
                                        {
                                            await contentStream.CopyToAsync(fileStream);
                                            Flower.PlayAnimation("\uE930", "下载文件成功");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Flower.PlayAnimation("\uEA39", "下载文件失败");
                                }


                            }
                        }

                    }
                    break;
                case "backlink":
                    if (result.Value == "bili")
                    {
                        Flower.PlayAnimation("\uE930", "已复制Bili外链");
                    }
                    break ;
                default://自动复制到用户剪切板
                    var datapackage = new DataPackage();
                    datapackage.SetText(url);
                    Clipboard.SetContent(datapackage);
                    Flower.PlayAnimation("\uE930", "已复制外部链接");
                    break;
            }

        }

        private void ShowTips(string title, string content)
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
            var m = sender as MenuFlyoutItem;
            if (m != null)
            {
                var tag = m.Tag as string;
                if (tag == "0")
                {
                    LoadMetaData(Set.Values["CurrentTopicId"] as string);
                    Flower.PlayAnimation("\uE930", "刷新标题栏成功");
                }
                else if (tag == "1")
                {
                    string shareurl = "https://www.cc98.org/topic/" + Set.Values["CurrentTopicId"] as string;
                    var datapackage = new DataPackage();
                    datapackage.SetText(shareurl);
                    Clipboard.SetContent(datapackage);
                    Flower.PlayAnimation("\uE930", "已复制帖子链接");
                }
                else if (tag == "2")
                {

                }
                else if (tag == "3")
                {
                    string _Visibility = ValidationHelper.IsTokenExist(Set, "IsImageVisible");
                    if (_Visibility == "1")
                    {
                        Set.Values["IsImageVisible"] = "2"; ;
                    }
                    else
                    {
                        Set.Values["IsImageVisible"] = "1";
                    }
                    LoadReply(Set.Values["CurrentTopicId"] as string, (Pager.SelectedPageIndex * 10).ToString());
                }

            }
            else
            {
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
                    bool status = await RequestSender.AddFavorites(Set.Values["CurrentTopicId"].ToString(), t);
                    if (status)
                    {
                        metadata.variant = IconVariant.Color;
                        LoadMetaData(Set.Values["CurrentTopicId"] as string);
                        Flower.PlayAnimation("\uE930", "已收藏");
                    }
                    else
                    {

                    }
                }
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
        private async void Person_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            ProfileViewer.Target = sender as HyperlinkButton;
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as Reply;
            if (t != null)
            {
                if (t.uid != "0")//非匿名才会跳转
                {
                    string ProfileUrl = "https://api.cc98.org/user/" + t.uid;
                    string ProfileText = await RequestSender.SimpleRequest(ProfileUrl);
                    if (!ProfileText.StartsWith("404:"))
                    {
                        var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(ProfileText);
                        var profile = new Info()
                        {
                            Name = js["name"].ToString(),
                            Id = js["id"].ToString(),
                            Popularity = js["popularity"].ToString(),
                            Fan = js["fanCount"].ToString(),
                            Port = js["portraitUrl"].ToString(),
                            Signature = UBBConverter.Convert(js["signatureCode"].ToString(), true),
                            Posts = js["postCount"].ToString(),
                        };
                        ProfileViewer.DataContext = profile;
                        ProfileViewer.IsOpen = true;
                    }
                    else
                    {
                        de.Text = ProfileText;
                    }
                    
                }
            }

        }

        public class Reply : INotifyPropertyChanged
        {
            private string _text;
            private string _like;
            private string _author;
            private string _rid;//回复Id,即PostId
            private string _dislike;
            private string _time;
            private string _uid;
            private string _url;
            private string _floor;//楼层数
            private IconVariant _likestate;//是否已点赞
            private IconVariant _dislikestate;//是否已点踩
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
            public IconVariant likestate
            {
                get => _likestate;
                set
                {
                    if (_likestate != value)
                    {
                        _likestate = value;
                        OnPropertyChanged(nameof(likestate));
                    }
                }
            }
            public IconVariant dislikestate
            {
                get => _dislikestate;
                set
                {
                    if (_dislikestate != value)
                    {
                        _dislikestate = value;
                        OnPropertyChanged(nameof(dislikestate));
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



        private void PostOperation_Click(object sender, RoutedEventArgs e)
        {
            var operation = sender as MenuFlyoutItem;
            if (operation != null)
            {
                var tag = operation.Tag;
                var reply = operation.DataContext as Reply;
                if (reply != null&&tag is string _tag )
                {

                    if (_tag == "UBB")//复制为UBB
                    {
                        var pack = new DataPackage();
                        pack.SetText(reply.text);
                        Clipboard.SetContent(pack);
                        Flower.PlayAnimation("\uE930", "已复制为UBB代码");
                    }
                    else if (_tag == "MD")
                    {
                        var pack = new DataPackage();
                        pack.SetText(UBBConverter.Convert(reply.text,true));
                        Clipboard.SetContent(pack);
                        Flower.PlayAnimation("\uE930", "已复制为Markdown文本");
                    }
                    else if (_tag.All(char.IsDigit))//追踪
                    {
                        TracingPostId = _tag;
                        Pager.SelectedPageIndex = -1;
                        IsTracing= true;
                        LoadTrace(TracingPostId, "0");
                        StopTrace.Visibility = Visibility.Visible;
                    }
                    else if (_tag == "QUOTE")
                    {
                        
                        if (reply.text != null)
                        {
                            var param = new Dictionary<string, string>()
                        {
                            {"Mode","1"},//回复主题为0，回帖为1，发主题、投票为2
                            {"Pid",Set.Values["CurrentTopicId"] as string },
                            {"QuoteText", reply.text},
                            {"ParentId", reply.rid}

                        };
                            Frame.Navigate(typeof(Post), param);
                        }
                        
                    }
                }
                
            }
        }
        private void PagerFix()
        {
            if (Pager.NumberOfPages == 1)
            {
                Pager.NextButtonVisibility = DevWinUI.PagerControlButtonVisibility.Hidden;
            }
            else
            {
                Pager.NextButtonVisibility = DevWinUI.PagerControlButtonVisibility.Visible;
            }
        }
        private async void LoadTrace(string post_id,string start)
        {
            string url = $"https://api.cc98.org/post/topic/specific-user?topicid={ValidationHelper.IsTokenExist(Set, "CurrentTopicId")}&postid={post_id}&from={start}&size=10";
            string trace = await RequestSender.SimpleRequest(url);
            if (!trace.StartsWith("404:"))
            {
                var Posts=Deserializer.ToArray(trace);
                if (Posts != null)
                {
                    replies.Clear();
                    List<string> users = new();
                    List<Reply> ReplyList = new List<Reply>();
                    string hideurl = "/Assets/hide.gif";
                    foreach (var Tile in Posts)
                    {
                        string TileText = Tile.ToString();
                        var TilePair = JsonConvert.DeserializeObject<Dictionary<string, object>>(TileText);
                        int count = Convert.ToInt32(TilePair["count"]);
                        Pager.NumberOfPages= count/10+1;
                        PagerFix();
                        string content = TilePair["content"].ToString();
                        string author = "匿名";
                        string time = TilePair["time"].ToString();
                        string rid = TilePair["id"].ToString();
                        string id = "0";//如果是匿名模式，id值为0
                        if (TilePair["userId"] != null)
                        {
                            id = TilePair["userId"].ToString();
                            author = TilePair["userName"].ToString();
                        }
                        else
                        {
                            author = "匿名 " + TilePair["userName"].ToString().ToUpper();
                        }
                        string floor = TilePair["floor"].ToString();
                        string like = TilePair["likeCount"].ToString();
                        string dislike = TilePair["dislikeCount"].ToString();
                        string likestate = TilePair["likeState"].ToString();
                        IconVariant variant1 = IconVariant.Regular;
                        IconVariant variant2 = IconVariant.Regular;
                        if (likestate == "1")
                        {
                            variant1 = IconVariant.Filled;
                            variant2 = IconVariant.Regular;
                        }
                        else if (likestate == "2")
                        {
                            variant1 = IconVariant.Regular;
                            variant2 = IconVariant.Filled;
                        }
                        else
                        {
                            variant1 = IconVariant.Regular;
                            variant2 = IconVariant.Regular;
                        }
                        if (author != "null" && author != null && id != "0")
                        {
                            users.Add("id=" + id);
                        }
                        ReplyList.Add(new Reply { author = author, text = content, like = like, dislike = dislike, rid = rid, time = time, uid = id, floor = floor + "L", url = hideurl, likestate = variant1, dislikestate = variant2 });
                    }
                    if (users.Count > 0)
                    {
                        string _SimpleUseInfo = await RequestSender.SimpleUserInfo(users);
                        Dictionary<string, string> PortDict = Deserializer.UserInfoList(_SimpleUseInfo);
                        if (PortDict != null)
                        {
                            foreach (Reply r in ReplyList)
                            {
                                if (PortDict.ContainsKey(r.uid))
                                {
                                    r.url = PortDict[r.uid];
                                }

                                replies.Add(r);
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
                    RootViewer.ScrollToVerticalOffset(0);//滚动到一页的最上方。这个方法必须在帖子load结束之后调用，否则ui切换逻辑错误。
                }
            
            }
        }


        private async void Like_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b != null)
            {
                var reply = b.DataContext as Reply;
                var tag = b.Tag as string;
                if (reply != null && tag != null)
                {
                    bool res = await RequestSender.Like(tag, reply.rid);
                    if (res)
                    {
                        
                        var NewState = await RequestSender.LikeState(reply.rid);
                        if (NewState != null)
                        {

                            reply.like = NewState["like"];
                            reply.dislike = NewState["dislike"];
                            if (NewState["likestate"] == "1")
                            {
                                reply.likestate = IconVariant.Filled;
                                reply.dislikestate = IconVariant.Regular;
                            }
                            else if (NewState["likestate"]=="2")
                            {
                                reply.likestate= IconVariant.Regular;
                                reply.dislikestate = IconVariant.Filled;
                            }
                            else
                            {
                                reply.likestate = IconVariant.Regular;
                                reply.dislikestate = IconVariant.Regular;
                            }
                        }

                    }
                }
            }


        }

        private void StopTrace_Click(object sender, RoutedEventArgs e)
        {
            IsTracing = false;
            LoadReply(ValidationHelper.IsTokenExist(Set, "CurrentTopicId"), (CurrentPage * 10).ToString());
           StopTrace.Visibility = Visibility.Collapsed;
        }

        private async void Drawer_ImageResolving(object sender, ImageResolvingEventArgs e)
        {
            var defr=e.GetDeferral();
            var Source=e.Url;
            if (Source == null) return;

            try
            {
                switch (Source)
                {
                    case string url when ImageResolver.IsWebUrl(url):
                        e.Image=await ImageResolver.LoadWebImage(url);
                        break;

                    case string path when ImageResolver.IsLocalPath(path):
                        e.Image= await ImageResolver.LoadLocalImage(path);
                        break;
                }
            }
            catch
            {
                e.Image = null;
            }
            e.Handled = true;
            defr.Complete();
            
        }

        private async void SmallProfile_Loaded(object sender, RoutedEventArgs e)
        {
            var p = sender as PersonPicture;
            if (p != null)
            {
                var _tag = p.Tag;
                if(_tag is string tag)
                {
                    var bitmap = await ImageResolver.LoadWebImage(tag);
                    p.ProfilePicture= bitmap;
                }
            }
        }

        private void SmallProfile_Unloaded(object sender, RoutedEventArgs e)
        {
            var p = sender as PersonPicture;
            if (p != null)
            {
                p.ProfilePicture = null;
            }
        }
    }
    public class UBBTextConverter : IValueConverter
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                string input = value as string;
                if (!string.IsNullOrEmpty(input))
                {
                    return UBBConverter.Convert(input, ValidationHelper.IsTokenExist(Set, "IsImageVisible")=="1");
                    
                }
                else
                {
                    return string.Empty;
                }

            }
            else
            {
                return string.Empty;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    
}
