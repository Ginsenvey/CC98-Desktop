using CC98.Kernel;
using CC98.Kernel.Network;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CommunityToolkit.Mvvm.ComponentModel;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.SmartCards;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Text.Json;
using CC98.Kernel.ApiScope;
using CC98.Services.Extensions;
namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Topic : Page
    {
        public ObservableCollection<Reply> replies=[];
        public TopicInfo topicInfo { get; set; } = new TopicInfo(){};
        public UserInfo profile = new() {Popularity=0,PostCount=0,FanCount=0}; 
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public bool isVote = false;//是否为投票贴
        public bool isJumping=false;//是否正在进行跳转
        public int JumpToFloor = -1;
        public int topicId = 0;
        public int currentPage = 0;
        public int pageSize = 10;
        private MediaPlayer _mediaPlayer;
        public Topic()
        {
            this.InitializeComponent();
            LoadSet();
            TileList.ItemsSource = replies;
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //释放资源
            base.OnNavigatedFrom(e);
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Dispose();
            }
            TileList.ItemsSource = null;
            replies.Clear();
            TileList = null;
        }
        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var args = e.TryGetParameter<TopicNavigationInfo>();
            if (args != null)
            {
                Set.Values["CurrentTopicId"]=args.TopicId;
                topicId=args.TopicId;
                await LoadTopicInfo();
                await LoadReply();
                LoadFavorites();
            }
            
        }
        private void LoadSet()
        {
            string _IsImageVisible = ValidationHelper.GetValue(Set, "IsImageVisible");
            if (_IsImageVisible == "0")
            {
                Set.Values["IsImageVisible"] = "2";   //此时默认为不可见
            } 
        }
        /// <summary>
        /// 加载收藏集
        /// </summary>
        private void LoadFavorites()
        {
            var favoritesJson = ValidationHelper.GetValue(Set, "Favorites");
            if (favoritesJson == "0")
            {
                Flower.PlayAnimation("\uEA39", "收藏夹未缓存");
            }
            var favoritesList = JsonSerializer.Deserialize<List<Favorites>>(favoritesJson);
            if (favoritesList == null)
            {
                Flower.PlayAnimation("\uEA39", "解析收藏夹缓存出错");
                return;
            }
            foreach (var favorites in favoritesList)
            {
                try
                {
                    var item = new MenuFlyoutItem { Text = favorites.Name, Tag = favorites.Id, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.Tag } };
                    item.Click += CollectionItem_Click;
                    CollectionMenu.Items.Add(item);
                }
                catch (Exception ex)
                {
                    Flower.PlayAnimation("\uEA39", ex.Message);
                }
            }
        }
        
        private async Task LoadTopicInfo()
        {
            string topicInfoUrl = ApiEndpoints.Topic.TopicInfo(topicId);
            var topicInfoResult = await RequestSender.Fetch<TopicInfo>(topicInfoUrl);
            if (!topicInfoResult.IsSuccess||topicInfoResult.Data==null)
            {
                return;
            }
            var data = topicInfoResult.Data;
            topicInfo.FavoriteCount = data.FavoriteCount;
            topicInfo.Title = data.Title;
            topicInfo.Time = data.Time;
            topicInfo.HitCount = data.HitCount;
            topicInfo.ReplyCount = data.ReplyCount;
            string isFavoriteUrl = ApiEndpoints.Topic.IsFavorite(topicId);
            var isFavoriteResult = await RequestSender.Fetch<bool>(isFavoriteUrl);
            if (isFavoriteResult.IsNotValid)
            {
                //
            }
            else
            {
                topicInfo.IsFavorite = isFavoriteResult.Data;
            }
            Pager.NumberOfPages = (Convert.ToInt32(topicInfo.ReplyCount) / 10) + 1;
            PagerFix();
            isVote = topicInfo.IsVote;
            if (isVote)
            {
                StartVote.Visibility = Visibility.Visible;
            }
        }

        
        private async Task LoadReply()
        {
            //清空
            replies.Clear();
            string replyUrl = ApiEndpoints.Topic.ReplyList(topicId, currentPage * pageSize);
            var replyResult=await RequestSender.Fetch<List<Reply>>(replyUrl);
            if (!replyResult.IsSuccess || replyResult.Data == null)
            {
                //报错
                return;
            }
            var data= replyResult.Data;
            
            var param = string.Join("&", data.Where(x=>!x.IsAnonymous).Select(x => $"id={x.UserId}").ToHashSet());
            string userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
            var userInfoResult = await RequestSender.Fetch<List<BasicUserInfo>>(userInfoUrl);
            if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
            {
                //报错
                return;
            }

            var userInfoList = userInfoResult.Data;
            //提取头像链接
            foreach (var reply in data)
            {
                //CC98 Deleter
                if (reply.IsDeleted)
                {
                    reply.UserName = "CC98 Deleter";
                    reply.Content = "<--该回复已被管理员或发布者删除-->";
                }
                if (reply.IsAnonymous)
                {
                    string code = reply.UserName;
                    reply.UserName = $"匿名{code.ToUpper()}";
                    reply.PortraitUrl = "ms-appx:///Assets/hide.gif";
                    //跳过
                    continue;
                }
                
                var user = userInfoList.First(x => x.Id == reply.UserId);
                if (user != null)
                {
                    reply.PortraitUrl = user.PortraitUrl;
                }
            }
            replies.AddRange(data);
        }


        private void Person_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as Reply;
            if (t != null)
            {
                if (t.UserId != 0)//非匿名才会跳转
                {
                    var info=new ProfileNavigationInfo { IsMe=t.IsMe,UserId=t.UserId};
                    Frame.Navigate(typeof(Profile), info);
                }

            }
        }
        public int CurrentPage = 0;
        private async void Pager_SelectedIndexChanged(DevWinUI.PagerControl sender, DevWinUI.PagerControlSelectedIndexChangedEventArgs args)
        {
            //此方法在页面加载完成后会被调用一次，Pager的SelectedIndex会被设置为0。
            //所以页面构造函数处不需要单独调用LoadReply方法。
            //限定了只有页面主动加载和用户点击翻页，index从-1到0不触发数据加载。
            
            PagerFix();
            if (args.PreviousPageIndex != -1)
            {
                de.Text += "trigged";
                int index = Pager.SelectedPageIndex;
                CurrentPage = index;
                if (index >= 0)
                {
                    int startindex = 10 * (index);
                    await LoadReply();
                    if (isJumping && JumpToFloor != -1)
                    {
                        GoTo(JumpToFloor);
                        isJumping = false;
                        JumpToFloor = -1;
                    }
                }
            }
           
        }

        
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
            var result = LinkAnalyzer.Parse(url);
            switch (result.Key)
            {
                case "topic":
                    Set.Values["CurrentTopicId"]=result.Value;
                    await LoadTopicInfo(result.Value);
                    await LoadReply(result.Value, "0");
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
                    //暂时不考虑跨页引用。如果考虑，我们需要改进跳转参数，让其包含一个跳转信息。
                    Regex regex = new Regex(pattern);

                    // 使用正则表达式进行匹配
                    Match match = regex.Match(url);

                    if (match.Success)
                    {
                        // 输出匹配的内容
                        string before = match.Groups[2].Value;  // #页码
                        string after = match.Groups[3].Value;   // #楼层
                        int page = Convert.ToInt32(before);
                        int floor = Convert.ToInt32(after);
                        try
                        {
                            if (Pager.SelectedPageIndex + 1 == page && floor > 0)
                            {
                                GoTo(floor - 1);
                            }
                            else
                            {
                                Pager.SelectedPageIndex = page - 1;
                                //应在页码变化函数中进行跳转，否则不等待。
                                isJumping = true;
                                JumpToFloor = floor-1;
                            }
                        }
                        catch
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
                        var source = await LoginService.vpn.GetSourceAsync(url);
                        if (source != null)
                        {
                            _mediaPlayer.Source = source;
                            _mediaPlayer.Play();
                            Play.Visibility = Visibility.Collapsed;
                            Pause.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            Flower.PlayAnimation("\uEA39", "音频下载出错");
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
                            try
                            {
                                string target_url = LoginService.vpn.IsVpnEnabled ? VpnService.ConvertUrl(url) : url;
                                var fileres = await LoginService.vpn.client.GetAsync(target_url, HttpCompletionOption.ResponseHeadersRead);
                                if (fileres.StatusCode == HttpStatusCode.OK)
                                {
                                    using (Stream contentStream = await fileres.Content.ReadAsStreamAsync(),
                                    fileStream = new FileStream(DownloadLocation, FileMode.Create, FileAccess.Write, FileShare.None))
                                    {
                                        await contentStream.CopyToAsync(fileStream);
                                        Flower.PlayAnimation("\uE930", "下载文件成功");
                                    }
                                }
                                else
                                {
                                    Flower.PlayAnimation("\uEA39", $"下载失败，状态码为{fileres.StatusCode.ToString()}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Flower.PlayAnimation("\uEA39", ex.Message);
                            }
                        }

                    }
                    break;
                case "backlink":
                    if (result.Value == "bili")
                    {
                        var _datapackage = new DataPackage();
                        _datapackage.SetText(url);
                        Clipboard.SetContent(_datapackage);
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
                {"Pid",ValidationHelper.GetValue(Set,"CurrentTopicId") },

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
                    await LoadMetaData(ValidationHelper.GetValue(Set, "CurrentTopicId"));
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
                    string _Visibility = ValidationHelper.GetValue(Set, "IsImageVisible");
                    if (_Visibility == "1")
                    {
                        Set.Values["IsImageVisible"] = "2"; ;
                    }
                    else
                    {
                        Set.Values["IsImageVisible"] = "1";
                    }
                    await LoadReply(ValidationHelper.GetValue(Set, "CurrentTopicId"), (Pager.SelectedPageIndex * 10).ToString());
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
                        topicInfo.variant = IconVariant.Color;
                        await LoadMetaData(Set.Values["CurrentTopicId"] as string);
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
                if (t.Uid != "0")//非匿名才会跳转
                {
                    string ProfileUrl = "https://api.cc98.org/user/" + t.Uid;
                    string ProfileText = await RequestSender.SimpleRequest(ProfileUrl);
                    if (!ProfileText.StartsWith("404:"))
                    {
                        var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(ProfileText);
                        profile.Name = js["name"].ToString();
                        profile.Id = js["id"].ToString();
                        profile.Popularity = js["popularity"].ToString();
                        profile.FanCount = js["fanCount"].ToString();
                        profile.PortraitUrl = js["portraitUrl"].ToString();
                        profile.SignatureCode = UBBConverter.Convert(js["signatureCode"].ToString(), true);
                        profile.PostCount = js["postCount"].ToString();
                        ProfileViewer.IsOpen = true;
                    }
                    else
                    {
                        de.Text = ProfileText;
                    }
                    
                }
            }

        }
        


        private async void PostOperation_Click(object sender, RoutedEventArgs e)
        {
            var operation = sender as MenuFlyoutItem;
            if (operation != null)
            {
                var tag = operation.Tag;
                var reply = operation.DataContext as Reply;
                if (reply != null&&tag is string _tag )
                {
                    switch (_tag)
                    {
                        case "UBB":
                            var pack = new DataPackage();
                            pack.SetText(reply.Content);
                            Clipboard.SetContent(pack);
                            Flower.PlayAnimation("\uE930", "已复制为UBB代码");
                            break;
                        case "MD":
                            var _pack = new DataPackage();
                            _pack.SetText(UBBConverter.Convert(reply.Content, true));
                            Clipboard.SetContent(_pack);
                            Flower.PlayAnimation("\uE930", "已复制为Markdown文本");
                            break;
                        case "QUOTE":
                            if (reply.Content != null)
                            {
                                int floor = reply.Floor;
                                int page = 1 + floor / 10;
                                int loc = floor % 10;
                                string header = $"[b]以下是引用{floor}楼：用户{reply.UserName}在{reply.Time}的发言：[url=/topic/{ValidationHelper.GetValue(Set, "CurrentTopicId")}/{page}#{loc}]>>查看原帖<<[/url][/b]\r\n";
                                var param = new Dictionary<string, string>()
                        {
                            {"Mode","1"},//回复主题为0，回帖为1，发主题、投票为2
                            {"Pid",ValidationHelper.GetValue(Set, "CurrentTopicId")},
                            {"BaseText", $"[quote]{header}{reply.Content}[/quote]"},
                            {"ParentId", reply.Id},

                        };
                                Frame.Navigate(typeof(Post), param);
                            }
                            break;
                        case "EDIT":
                            var _param = new Dictionary<string, string>()
                            {
                                {"Mode","3"},
                                {"Pid",ValidationHelper.GetValue(Set, "CurrentTopicId")},
                                {"BaseText", reply.Content},
                                {"Rid", reply.Id},//回帖id
                                {"Title",topicInfo.Title},//主题标题
                            };
                            Frame.Navigate(typeof(Post), _param);
                            break;
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
        


        private async void Like_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b != null)
            {
                var reply = b.DataContext as Reply;
                var tag = b.Tag as string;
                if (reply != null && tag != null)
                {
                    bool res = await RequestSender.Like(tag, reply.Id);
                    if (res)
                    {
                        
                        var NewState = await RequestSender.LikeState(reply.Id);
                        if (NewState != null)
                        {

                            reply.Like = NewState["like"];
                            reply.Dislike = NewState["dislike"];
                            if (NewState["likestate"] == "1")
                            {
                                reply.Likestate = IconVariant.Filled;
                                reply.Dislikestate = IconVariant.Regular;
                            }
                            else if (NewState["likestate"]=="2")
                            {
                                reply.Likestate= IconVariant.Regular;
                                reply.Dislikestate = IconVariant.Filled;
                            }
                            else
                            {
                                reply.Likestate = IconVariant.Regular;
                                reply.Dislikestate = IconVariant.Regular;
                            }
                        }

                    }
                    else
                    {
                        Flower.PlayAnimation("\uEA39","操作失败");
                    }
                }
            }


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

        private void Drawer_Unloaded(object sender, RoutedEventArgs e)
        {
            var mt = sender as MarkdownTextBlock;
            if (mt != null)
            {
                mt.Text = string.Empty;
            }
        }
        private async Task InitializeVote()
        {
            if (isVote)
            {

                string vote_url = $"https://api.cc98.org/topic/{ValidationHelper.GetValue(Set, "CurrentTopicId")}/vote";
                var restext = await RequestSender.SimpleRequest(vote_url);
                if (!restext.StartsWith("404:"))
                {
                    var info = Deserializer.ToDictionary(restext);
                    if (info != null)
                    {
                        var vote_items = Deserializer.ToArray(ValidationHelper.GetKey(info, "voteItems"));
                        var record_text = ValidationHelper.GetKey(info, "myRecord");
                        var record = new List<int>();
                        if (record_text != "0")
                        {
                            var js = Deserializer.ToDictionary(record_text);
                            var array = ValidationHelper.GetKey(js, "items");
                            record = Deserializer.ToArray(array).Select(g => Convert.ToInt32(g)).ToList();
                        }

                        if (vote_items != null)
                        {
                            var list = vote_items.Select(g => JsonConvert.DeserializeObject<VoteItem>(g.ToString())).ToList();
                            VoteList.ItemsSource = list;
                            if (record != null)
                            {
                                if (record.Count > 0)
                                {
                                    foreach (int g in record)
                                    {
                                        VoteList.SelectedItems.Add(VoteList.Items[g - 1]);
                                    }
                                }
                            }
                            bool can_vote = ValidationHelper.GetKey(info, "canVote") == "True";
                            bool is_active = ValidationHelper.GetKey(info, "isAvailable") == "True";
                            if (can_vote && is_active)
                            {
                                SendVote.IsEnabled = true;
                                VoteTitle.Text = "投票(开放中)";
                            }
                            else
                            {
                                SendVote.IsEnabled = false;
                                VoteList.IsEnabled = false;
                                if (is_active)
                                {
                                    VoteTitle.Text = "投票(已投票)";
                                }
                                else
                                {
                                    VoteTitle.Text = "投票(已过期)";
                                }
                            }
                            int max_count = Convert.ToInt32(ValidationHelper.GetKey(info, "maxVoteCount"));
                            VoteList.SelectionChanged += (s, e) =>
                            {
                                if (VoteList.SelectedItems.Count > max_count)
                                {
                                    SendVote.IsEnabled = false;
                                }
                                else
                                {
                                    SendVote.IsEnabled = true;
                                }
                            };
                            votetime.Text = "过期时间:" + ValidationHelper.GetKey(info, "expiredTime");
                            voteinfo.Text = "参与人数:" + ValidationHelper.GetKey(info, "voteUserCount") + ";票数限制:" + ValidationHelper.GetKey(info, "maxVoteCount");
                        }
                    }

                }
            }
        }
        private async void StartVote_Click(object sender, RoutedEventArgs e)
        {
            await InitializeVote();
            VotePanel.IsOpen = true;
        }

        

        private void VotePanel_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            VoteList.ItemsSource = null;
        }

        private async void SendVote_Click(object sender, RoutedEventArgs e)
        {
            if (VoteList.SelectedItems.Count > 0)
            {
                var list = VoteList.SelectedItems.Select(g=>VoteList.Items.IndexOf(g)+1).ToList();
                var r = await RequestSender.SendVoteResult(ValidationHelper.GetValue(Set, "CurrentTopicId"), list);
                if (r == "1")
                {
                    Flower.PlayAnimation("\uE930", "投票完成");
                    await InitializeVote();
                }
                else
                {
                    Flower.PlayAnimation("\uEA39", "投票失败");
                }
            }
            else
            {
                Flower.PlayAnimation("\uEA39", "选择至少一项");
            }
        }

        
    }
    public class VoteItem
    {
        public required string id { get; set; }
        public int count { get; set; }
        public required string description { get; set; }
    }
    
    
    
    
}
