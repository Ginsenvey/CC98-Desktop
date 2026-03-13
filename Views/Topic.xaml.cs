using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.Network;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CC98.Share.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UbbRender.Common;
using UbbRender.Parser;
using UbbRender.Render;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using static CC98.Kernel.ApiScope.ApiEndpoints;
namespace CC98
{
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
        public GlobalService globalService= GlobalService.Instance;
        public Topic()
        {
            this.InitializeComponent();
            LoadSet();
            LoadFavorites();
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //释放资源
            base.OnNavigatedFrom(e);   
            replies.Clear();
        }
        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var args = e.TryGetParameter<TopicNavigationInfo>();
            if (args != null)
            {
                topicId =args.TopicId;
                await LoadTopicInfo();
                if (args.IsJumpingMode)
                {
                    isJumping = true;
                    await TP(args.TargetFloor);
                    return;
                }
                
                await LoadReply();
                
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
                Flower.Play("\uEA39", "收藏夹未缓存");
            }
            var favoritesList =JsonSerialize.Deserialize<List<Favorites>>(favoritesJson);
            if (favoritesList == null)
            {
                Flower.Play("\uEA39", "解析收藏夹缓存出错");
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
                    Flower.Play("\uEA39", ex.Message);
                }
            }
        }
        //当jumping mode=true时响应。响应包括两种，来自外部页面导航的跳转和用户点击帖子内链接的跳转。
        //floor如17824L，则page为1782，sort为4，此时目标楼层的index是3.sort=1时目标index为0。如果sort=0,则目标页码在上一页。
        private async Task TP(int floor)
        {
            // 解析楼层
            int page = floor / 10;
            int sort = floor % 10;
            int currentPage = Pager.SelectedPageIndex;

            // 情况1：目标就在当前页
            if (currentPage == page)
            {
                await HandleSamePageJump(sort);
            }
            // 情况2：目标是上一页的最后一个（特殊边界情况）
            else if (currentPage == page - 1 && sort == 0)
            {
                await HandlePrevPageLastItem();
            }
            // 情况3：需要翻页
            else
            {
                HandlePageNavigation(page, sort);
            }
        }

        /// <summary>
        /// 处理同一页内的跳转
        /// </summary>
        private async Task HandleSamePageJump(int sort)
        {
            if (sort == 0)
            {
                // 整十楼：跳转到上一页的最后一个
                Pager.SelectedPageIndex--;
                JumpToFloor = 9;
            }
            else
            {
                // 非整十楼：直接跳转到对应楼层
                await EnsureReplyLoadedAndScroll(sort - 1);
            }
        }

        /// <summary>
        /// 处理跳转到上一页最后一个的情况
        /// </summary>
        private async Task HandlePrevPageLastItem()
        {
            Pager.SelectedPageIndex++;

            // 判断第10楼是否已加载
            if (replies.Count == 10)
            {
                ScrollTo(9);
            }
            else
            {
                await LoadReply();
                ScrollTo(9);
            }
        }

        /// <summary>
        /// 处理需要翻页的跳转
        /// </summary>
        private void HandlePageNavigation(int targetPage, int targetSort)
        {
            if (targetSort == 0)
            {
                // 整十楼：目标在上一页
                Pager.SelectedPageIndex = targetPage - 1;
                JumpToFloor = 9;
            }
            else
            {
                // 非整十楼：目标在当前页
                Pager.SelectedPageIndex = targetPage;
                JumpToFloor = targetSort - 1;
            }
        }

        /// <summary>
        /// 确保指定索引的回复已加载并滚动到该位置
        /// </summary>
        private async Task EnsureReplyLoadedAndScroll(int targetIndex)
        {
            // 如果目标索引尚未加载，先加载数据
            if (replies.Count <= targetIndex)
            {
                await LoadReply();
            }

            ScrollTo(targetIndex);
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
            Pager.NumberOfPages = (topicInfo.ReplyCount/ 10) + 1;
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
                //
                await App.Logger.WriteAsync("Topic","加载回帖失败", replyResult.Message);
                return;
            }
            var data= replyResult.Data;
            
            var param = string.Join("&", data.Where(x=>!x.IsAnonymous&&x.UserId.HasValue).Select(x => $"id={x.UserId}").ToHashSet());
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
                    reply.PortraitUrl = "ms-appx:///Assets/deleter.png";
                    //跳过
                    continue;
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
            ProfileViewer.Target = h;
            var t = h?.Tag as Reply;
            if (t == null || t.IsAnonymous) return;
            var info = new ProfileNavigationInfo { IsMe = t.IsMe, UserId = t.UserId ?? 0 };
            Frame.Navigate(typeof(Profile), info);
        }
        private async void Pager_SelectedIndexChanged(DevWinUI.PagerControl sender, DevWinUI.PagerControlSelectedIndexChangedEventArgs args)
        {
            //此方法在页面加载完成后会被调用一次，Pager的SelectedIndex会被设置为0。
            //所以页面构造函数处不需要单独调用LoadReply方法。
            //限定了只有页面主动加载和用户点击翻页，index从-1到0不触发数据加载。
            
            PagerFix();
            if (args.PreviousPageIndex != -1)
            {
                int index = Pager.SelectedPageIndex;
                currentPage = index;
                if (index >= 0)
                {
                    await LoadReply();
                    if (isJumping && JumpToFloor != -1)
                    {
                        ScrollTo(JumpToFloor);
                        isJumping = false;
                        JumpToFloor = -1;
                    }
                }
            }
           
        }

        
        
        private async void MarkdownTextBlock_LinkClicked(object sender)
        {
            var url = "";
            var result = LinkAnalyzer.Parse(url);
            switch (result.Key)
            {
                case "topic":
                    Set.Values["CurrentTopicId"]=result.Value;
                    topicId =int.Parse(result.Value);
                    await LoadTopicInfo();
                    await LoadReply();
                    break;
                case "user":
                    {
                        string _url = "https://api.cc98.org/user/name/" + result.Value;
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
                                ScrollTo(floor - 1);
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
                        
                    }
                    else if (result.Value == "doc")//无法预览的媒体文件类
                    {
                        //var Operation = await DownLoadDialog.ShowAsync();
                        if (true)
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
                                        Flower.Play("\uE930", "下载文件成功");
                                    }
                                }
                                else
                                {
                                    Flower.Play("\uEA39", $"下载失败，状态码为{fileres.StatusCode.ToString()}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Flower.Play("\uEA39", ex.Message);
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
                        Flower.Play("\uE930", "已复制Bili外链");
                    }
                    break ;
                default://自动复制到用户剪切板
                    var datapackage = new DataPackage();
                    datapackage.SetText(url);
                    Clipboard.SetContent(datapackage);
                    Flower.Play("\uE930", "已复制外部链接");
                    break;
            }

        }
        
        
        private void writereply_Click(object sender, RoutedEventArgs e)
        {
            var param = new EditorNavigationInfo
            {
                EditorMode=EditorMode.ReplyToTopic,
                TopicId=topicId,
                HintText= topicInfo.Title,
            };
            Frame.Navigate(typeof(UBBEditor), param);
        }

        private async void TileFlyout_Click(object sender, RoutedEventArgs e)
        {
            var m = sender as MenuFlyoutItem;
            if (m != null)
            {
                var tag = m.Tag as string;
                if (tag == "0")
                {
                    await LoadTopicInfo();
                    await LoadReply();
                    Flower.Play("\uE930", "刷新成功");
                }
                else if (tag == "1")
                {
                    string shareurl = "https://www.cc98.org/topic/" + Set.Values["CurrentTopicId"] as string;
                    var datapackage = new DataPackage();
                    datapackage.SetText(shareurl);
                    Clipboard.SetContent(datapackage);
                    Flower.Play("\uE930", "已复制帖子链接");
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
                    await LoadReply();
                }

            }
            else
            {
            }

        }
        

        private async void CollectionItem_Click(object sender, RoutedEventArgs e)
        {
            var m = sender as MenuFlyoutItem;
            var t = m?.Tag as string;
            if (t == null) return;
            var url = ApiEndpoints.Topic.AddIntoFavorites(topicId, int.Parse(t));
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var result=await RequestSender.Put(url, content);
            if (!result.IsSuccess)
            {
                //
                return;
            }
            topicInfo.IsFavorite = true;
            await LoadTopicInfo();
            Flower.Play("\uE930", "已收藏");
        }

        

        

        

        

        private void ScrollTo(int index)
        {
            try
            {
                var element = ReplyRepeater.GetOrCreateElement(index);
                var options = new BringIntoViewOptions
                {
                    VerticalAlignmentRatio = 0, // 0=顶部对齐，0.5=居中，1=底部
                    AnimationDesired = true       // 启用平滑滚动动画
                };
                element.StartBringIntoView(options);
                //对于没有页码的跳转链接暂时没有处理
            }
            catch { }
        }
        



        private void PostOperation_Click(object sender, RoutedEventArgs e)
        {
            var operation = sender as MenuFlyoutItem;
            var tag = operation?.Tag as string;
            var reply = operation?.DataContext as Reply;
            if (reply == null || tag == null) return;
            switch (tag)
            {
                case "UBB":
                    var pack = new DataPackage();
                    pack.SetText(reply.Content);
                    Clipboard.SetContent(pack);
                    Flower.Play("\uE930", "已复制为UBB代码");
                    break;
                case "MD":
                    var _pack = new DataPackage();
                    _pack.SetText(UBBConverter.Convert(reply.Content, true));
                    Clipboard.SetContent(_pack);
                    Flower.Play("\uE930", "已复制为Markdown文本");
                    break;
                case "QUOTE":
                    if (reply.Content != null)
                    {
                        int floor = reply.Floor;
                        int page = 1 + floor / 10;
                        int location = floor % 10;
                        string header = $"[b]以下是引用{floor}楼：用户{reply.UserName}在{reply.Time}的发言：[url=/topic/{ValidationHelper.GetValue(Set, "CurrentTopicId")}/{page}#{location}]>>查看原帖<<[/url][/b]\r\n";
                        var param = new EditorNavigationInfo
                        {
                            EditorMode = EditorMode.ReplyToPost,
                            TopicId = topicId,
                            QuoteHeader = $"[quote]{header}{reply.Content}[/quote]",
                            ParentId = reply.Id,
                            HintText=$"引用{reply.UserName}的回复",
                            Floor=floor
                        };
                        Frame.Navigate(typeof(UBBEditor), param);
                    }
                    break;
                case "EDIT":
                    var _param = new EditorNavigationInfo
                    {
                        EditorMode = reply.Floor==0?EditorMode.EditMyTopic:EditorMode.EditMyPost,
                        TopicId = topicId,
                        BaseText = reply.Content,
                        PostId = reply.Id,
                        HintText = topicInfo.Title,
                        Floor=reply.Floor
                    };
                    Frame.Navigate(typeof(UBBEditor), _param);
                    break;
            }
        }
        private void PagerFix()
        {
            Pager.NextButtonVisibility = Pager.NumberOfPages == 1 ? 
                PagerControlButtonVisibility.Hidden : 
                PagerControlButtonVisibility.Visible;
        }



        private async void Like_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b == null) return;
            var reply = b.DataContext as Reply;
            var mode = b.Tag as string;
            if (reply == null || mode == null) return;
            var postId = reply.Id;
            var url = ApiEndpoints.Post.React(postId);
            var content = new StringContent(mode, Encoding.UTF8, "application/json");
            var result = await RequestSender.Put(url, content);
            if (!result.IsSuccess)
            {
                //
                Flower.Play("\uEA39", "操作失败");
                return;
            }
            var newStateUrl = ApiEndpoints.Post.ReactionState(postId);
            var newStateResult = await RequestSender.Fetch<ReactionState>(newStateUrl);
            if (!newStateResult.IsSuccess || newStateResult.Data == null)
            {
                //
                Flower.Play("\uEA39", "获取赞踩数据失败");
                return;
            }
            var newState = newStateResult.Data;
            reply.LikeState = newState.LikeState;
            reply.LikeCount = newState.LikeCount;
            reply.DislikeCount = newState.DislikeCount;
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

        
        private async Task InitializeVote()
        {
            if (isVote)
            {

                string voteUrl = ApiEndpoints.Topic.Vote(topicId);
                var voteResult = await RequestSender.Fetch<VoteInfo>(voteUrl);
                if (!voteResult.IsSuccess || voteResult.Data == null)
                {
                    //
                    return;
                }
                var data= voteResult.Data;
                VoteList.ItemsSource= data.VoteItems;
                var record = data.MyRecord;
                if (record.Count > 0)
                {
                    foreach (int i in record)
                    {
                        VoteList.SelectedItems.Add(VoteList.Items[i - 1]);
                    }
                }

                if (data.CanVote && data.IsAvailable)
                {
                    SendVote.IsEnabled = true;
                    VoteTitle.Text = "投票(开放中)";
                }
                else
                {
                    SendVote.IsEnabled = false;
                    VoteList.IsEnabled = false;
                    if (data.IsAvailable)
                    {
                        VoteTitle.Text = "投票(已投票)";
                    }
                    else
                    {
                        VoteTitle.Text = "投票(已过期)";
                    }
                }
                VoteList.SelectionChanged += (s, e) =>
                {
                    if (VoteList.SelectedItems.Count > data.MaxVoteCount)
                    {
                        SendVote.IsEnabled = false;
                    }
                    else
                    {
                        SendVote.IsEnabled = true;
                    }
                };
                votetime.Text = $"过期时间:{data.expiredTime}";
                voteinfo.Text = $"参与人数:{data.VoteUserCount},票数限制:{data.MaxVoteCount}";


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
                    Flower.Play("\uE930", "投票完成");
                    await InitializeVote();
                }
                else
                {
                    Flower.Play("\uEA39", "投票失败");
                }
            }
            else
            {
                Flower.Play("\uEA39", "选择至少一项");
            }
        }

        private async void Person_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            try
            {
                var h = sender as HyperlinkButton;
                ProfileViewer.Target = h;
                var t = h?.Tag as Reply;
                if (t == null || t.IsAnonymous) return;
                string profileUrl = ApiEndpoints.User.UserProfile(false, t.UserId ?? 0);
                var profileResult = await RequestSender.Fetch<UserInfo>(profileUrl);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    return;
                }
                var data = profileResult.Data;
                profile.Name = data.Name;
                profile.Id = data.Id;
                profile.Popularity = data.Popularity;
                profile.FanCount = data.FanCount;
                profile.PortraitUrl = data.PortraitUrl;
                profile.SignatureCode = data.SignatureCode;
                profile.PostCount = data.PostCount;
                ProfileViewer.IsOpen = true;
            }
            catch (Exception ex)
            {
                await App.Logger.WriteAsync("Topic", "加载用户信息预览失败", ex.Message);
            }
        }

        

        private void UbbTextBlock_MediaClicked(object sender, MediaClickEventArgs e)
        {
            de.Text = $"链接：{e.Source}，类型：{e.MediaType}";
            switch (e.MediaType)
            {
                case MediaType.Image:
                    var u=sender as UbbTextBlock;
                    if (u == null) return;
                    string ubb = u.UbbText;
                    var doc= Parser.Parse(ubb);
                    if (doc == null) return;
                    var list=new List<string>();
                    var nodes = doc.Root.GetDescendantsByType(UbbNodeType.Image);
                    foreach(var node in nodes)
                    {
                        list.Add(ExtractImageUrl(node));
                    }
                    int anchor = list.IndexOf(e.Source);
                    var info = new ViewerNavigationInfo
                    {
                        Type = "image",
                        Urls = list,
                        CurrentIndex=anchor
                    };
                    var viewer=new MediaViewer(info);
                    viewer.Activate();
                    break;
                case MediaType.Video:
                    var vinfo = new ViewerNavigationInfo
                    {
                        Type = "video",
                        Urls = new List<string> { e.Source }
                    };
                    Frame.Navigate(typeof(MediaViewer), vinfo);
                    break;
            }
        }
        private string ExtractImageUrl(UbbNode node)
        {
            string src = "";
            if (node is TagNode tagNode)
            {
                var value = tagNode.GetAttribute("value");
                if (string.IsNullOrEmpty(value) || value == "1")
                {
                    // 尝试从子节点获取URL（对于 [img]url[/img] 格式）
                    var first = node.FirstChild;
                    if (first is TextNode textNode)
                    {
                        src = textNode.Content;
                    }
                }
                else
                {
                    src = value;
                }
            }
            return src;
        }
    }
}
