using ABI.Microsoft.UI.Xaml.Media.Animation;
using CCkernel;
using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Geolocation;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using Windows.UI;
using static App3.Index;
using static App3.Message;
using static App3.Topic;
using static System.Net.Mime.MediaTypeNames;
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

            // ��ȡ���ݵĲ���
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                Set.Values["CurrentTopicId"] = parameter;
                LoadMetaData(parameter);
                LoadReply(Set.Values["CurrentTopicId"] as string, "0", IsImageVisible);
                LoadFavorites();
            }
            else
            {

            }
        }
        private void LoadFavorites()
        {
            if (ValidationHelper.IsTokenExist(Set, "Favorites"))
            {
                var LikeList = JsonConvert.DeserializeObject<JArray>(Set.Values["Favorites"].ToString());
                foreach (var like in LikeList)
                {
                    try
                    {
                        var likeinfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(like.ToString());
                        string collection = likeinfo["name"].ToString();
                        string sortid = like["id"].ToString();
                        var s = new MenuFlyoutItem { Text = collection, Tag = sortid, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.List } };
                        s.Click += CollectionItem_Click;
                        CollectionMenu.Items.Add(s);
                    }
                    catch (Exception ex)
                    {
                        Flower.PlayAnimation("\uEA39", "�������������⡣");
                    }
                    
                }
            }
            else
            {
                Flower.PlayAnimation("\uEA39", "�������������⡣");
            }
        }
        private void Stats(string board)
        //ͳ�ư����ʴ�������Ϊ�û�����ģ�͵�һ���֡�
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
            string access = Set.Values["Access"] as string;
            if (!string.IsNullOrEmpty(access))
            {
                CCloginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
                string MetaDataUrl = "https://api.cc98.org/topic/" + pid;
                var MetaDataRes = await CCloginservice.client.GetAsync(MetaDataUrl);
                if (MetaDataRes.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string MetaDataText = await MetaDataRes.Content.ReadAsStringAsync();
                    var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(MetaDataText);
                    metadata.favorite = js["favoriteCount"].ToString();
                    metadata.text = js["title"].ToString();
                    metadata.time = js["time"].ToString();
                    metadata.hit = js["hitCount"].ToString();
                    metadata.reply = js["replyCount"].ToString();
                    string favourl = "https://api.cc98.org/topic/" + pid + "/isfavorite";
                    var favourres = await CCloginservice.client.GetAsync(favourl);

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
                        Pager.NumberOfPages = (Convert.ToInt32(metadata.reply) / 10) + 1;
                    }
                    else
                    {
                        Pager.NumberOfPages = 1;
                    }
                }
            }
        }
        public bool IsImageVisible = false;
        private async void LoadReply(string pid, string start, bool mode)
        {
            if (ValidationHelper.IsTokenExist(Set, "Access"))
            {
                JArray Posts = new JArray();
                string PostText = await RequestSender.TopicReply(pid, start);
                if (ValidationHelper.IsValidResponse(PostText))//�ظ���Ч
                {
                    Posts = Deserializer.ToArray(PostText);
                }
                else//�ظ���Ч������һ�Ρ�
                {
                    if (PostText.StartsWith("404:"))
                    {
                        if (ValidationHelper.IsTokenExist(Set, "Refresh"))
                        {
                            string NewAccess = await CCloginservice.RefreshToken(Set.Values["Refresh"].ToString());
                            if (NewAccess != "0")
                            {
                                Set.Values["Access"] = NewAccess;
                                CCloginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NewAccess);
                                string _PostText = await RequestSender.TopicReply(pid, start);
                                if (ValidationHelper.IsValidResponse(_PostText))
                                {
                                    Posts = Deserializer.ToArray(_PostText);

                                }
                                else
                                {
                                    return;//ʧ��
                                }
                            }
                            else
                            {
                                return;//ˢ������ʧ��
                            }
                        }
                        else
                        {
                            return;//ˢ�����Ʋ�����
                        }
                    }
                    else
                    {
                        return;//��Ե����������ϲ����ڴ˷�֧
                    }
                }

                if (Posts != null)
                {
                    if (Posts.Count > 0)
                    {
                        replies.Clear();
                        TileList.ItemsSource = null;//����󶨣���ֹ�ظ����ݡ��˴�������TileList.ItemsSource��ֵ֮ǰ���ã�����ᵼ�������ظ��󶨡�
                        List<string> users = new();
                        List<Reply> ReplyList = new List<Reply>();
                        string hideurl = "/Assets/hide.gif";
                        foreach (var Tile in Posts)
                        {
                            string TileText = Tile.ToString();
                            var TilePair = JsonConvert.DeserializeObject<Dictionary<string, object>>(TileText);
                            string content = TilePair["content"].ToString();
                            string author = "����";
                            string time = TilePair["time"].ToString();
                            string rid = TilePair["id"].ToString();
                            string id = "0";//���������ģʽ��idֵΪ0
                            if (TilePair["userId"] != null)
                            {
                                id = TilePair["userId"].ToString();
                                author = TilePair["userName"].ToString();
                            }
                            string floor = TilePair["floor"].ToString();
                            string like = TilePair["likeCount"].ToString();
                            string dislike = TilePair["dislikeCount"].ToString();
                            string likestate = TilePair["likeState"].ToString();
                            IconVariant variant1 = IconVariant.Regular ;
                            IconVariant variant2= IconVariant.Regular ;
                            if (likestate == "1")
                            {
                                variant1 = IconVariant.Filled;
                                variant2 = IconVariant.Regular;
                            }
                            else if(likestate == "2")
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
                            ReplyList.Add(new Reply { author = author, text=content, like = like, dislike = dislike, rid = rid, time = time, uid = id, floor = floor + "L", url = hideurl,likestate=variant1,dislikestate=variant2 });
                        }
                        if (users.Count > 0)
                        {
                            string _SimpleUseInfo = await RequestSender.SimpleUserInfo(users);
                            Dictionary<string, string> PortDict = Deserializer.UserInfoList(_SimpleUseInfo);
                            //�˴���һ����ѧ���⣬�Ϸ��Ѿ����й�һ��Clear������Ϊʲô�������ظ����ݣ�ԭ����TileList.ItemsSource�İ�û�б������������TileList.ItemsSource����һ�μ���ʱ��Ȼ��������һ�ε����á����������ÿ�μ���ǰ�����replies���ϣ�Ȼ�����°󶨡�
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
                        TileList.ItemsSource = replies;
                        RootViewer.ScrollToVerticalOffset(0);//������һҳ�����Ϸ��������������������load����֮����ã�����ui�л��߼�����
                    }
                }
                else
                {
                    return;//posts����ֵnull����ȡʧ��
                }
            }
            else
            {
                return;//Access���Ʋ�����
            }

        }


        private void Person_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as Reply;
            if (t != null)
            {
                if (t.uid != "0")//�������Ż���ת
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

        private void Pager_SelectedIndexChanged(DevWinUI.PagerControl sender, DevWinUI.PagerControlSelectedIndexChangedEventArgs args)
        {
            //�˷�����ҳ�������ɺ�ᱻ����һ�Σ�Pager��SelectedIndex�ᱻ����Ϊ0��
            //����ҳ�湹�캯��������Ҫ��������LoadReply������
            //�޶���ֻ��ҳ���������غ��û������ҳ��index��-1��0���������ݼ��ء����ƺ���pager��bug.
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
                Volume = 0.8 // Ĭ������ (0.0 ~ 1.0)
            };

            // �����ؼ��¼�

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
                                catch (Exception ex)
                                {

                                }
                            }

                        }
                        break;
                    }
                //using��䲻����switch�����ֱ�ӳ��֡���ˣ�ʹ�ô����Ű�Χ���case.
                case "anchor":
                    string pattern = @"/topic/(\d{7})/(\d+)#(\d+)";
                    Regex regex = new Regex(pattern);

                    // ʹ��������ʽ����ƥ��
                    Match match = regex.Match(url);

                    if (match.Success)
                    {
                        // ���ƥ�������
                        string beforeHash = match.Groups[2].Value;  // #ǰ�������
                        string afterHash = match.Groups[3].Value;   // #���������
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
                            // ����ý��Դ
                            _mediaPlayer.Source = mediaSource;
                            // ��ʼ����
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
                    else if (result.Value == "doc")//�޷�Ԥ����ý���ļ���
                    {
                        var Operation = await DownLoadDialog.ShowAsync();
                        if (Operation == ContentDialogResult.Primary)
                        {

                            string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            string DownloadsFolder = System.IO.Path.Combine(UserProfile, "Downloads");
                            string DownloadLocation = "";
                            string filepattern = @"(?<=https://file\.cc98\.org/v4-upload/d/\d{4}/\d{4}/)[^/]+";

                            // ����������ʽ����
                            Regex fileregex = new Regex(filepattern);

                            // ִ��ƥ��
                            Match filematch = fileregex.Match(url);
                            if (filematch.Success)
                            {
                                DownloadLocation = DownloadsFolder + "\\" + filematch.Value;
                            }
                            else
                            {
                                DownloadLocation = System.IO.Path.Combine(DownloadsFolder, "CC98_Download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
                            }
                            ShowTips("��ʼ���ظ�����:", DownloadLocation);
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
                                            ShowTips("�������:", DownloadLocation);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ShowTips("���س���:", ex.Message);
                                }


                            }
                        }

                    }
                    break;
                default://�Զ����Ƶ��û����а�
                    var datapackage = new DataPackage();
                    datapackage.SetText(url);
                    Clipboard.SetContent(datapackage);
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
                {"Mode","0"},//�ظ�����Ϊ0������Ϊ1�������⡢ͶƱΪ2
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
                    Flower.PlayAnimation("\uE930", "ˢ�³ɹ�");
                }
                else if (tag == "1")
                {
                    string shareurl = "https://www.cc98.org/topic/" + Set.Values["CurrentTopicId"] as string;
                    var datapackage = new DataPackage();
                    datapackage.SetText(shareurl);
                    Clipboard.SetContent(datapackage);
                    Flower.PlayAnimation("\uE930", "�Ѹ�����������");
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
                        Flower.PlayAnimation("\uE930", "���ղ�");
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
                VerticalAlignmentRatio = 0.5, // 0=�������룬0.5=���У�1=�ײ�
                AnimationDesired = true       // ����ƽ����������
            };
            element.StartBringIntoView(options);
            //����û��ҳ�����ת������ʱû�д���
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

        
        public class Reply : INotifyPropertyChanged
        {
            private string _text;
            private string _like;
            private string _author;
            private string _rid;//�ظ�Id,��PostId
            private string _dislike;
            private string _time;
            private string _uid;
            private string _url;
            private string _floor;//¥����
            private IconVariant _likestate;//�Ƿ��ѵ���
            private IconVariant _dislikestate;//�Ƿ��ѵ��
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
            private string _text;//����
            private string _like;//��
            private string _author;//¥����
            private string _rid;//����id
            private string _reply;//������
            private string _favorite;//�ղ���
            private string _time;//����ʱ��
            private string _uid;//¥��id
            private string _dislike;//��
            private string _isrealidvisible;//�������
            private string _hit;//�ȶȣ������
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
                string tag = operation.Tag as string;
                var reply = operation.DataContext as Reply;
                if (reply != null)
                {
                    if (tag == "0")//����ΪUBB
                    {
                        var pack = new DataPackage();
                        pack.SetText(reply.text);
                        Clipboard.SetContent(pack);
                        Flower.PlayAnimation("\uE930", "�Ѹ���ΪUBB����");
                    }
                    else if (tag == "1")
                    {
                        var pack = new DataPackage();
                        pack.SetText(UBBConverter.Convert(reply.text,true));
                        Clipboard.SetContent(pack);
                        Flower.PlayAnimation("\uE930", "�Ѹ���ΪMarkdown�ı�");
                    }
                    else if (tag == "3")
                    {


                    }
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

       
    }
    public class UBBTextConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                string input = value as string;
                if (!string.IsNullOrEmpty(input))
                {
                    return UBBConverter.Convert(input, false);
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
