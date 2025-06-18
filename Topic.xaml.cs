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

            // ��ȡ���ݵĲ���
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
                                        return;//�޷���Ȩ�������������������/�������⡣
                                        //��֪���⣺markdowntextblock����������ӣ����´����������
                                        //�������Ե���������������ƥ�䣬ȷ���ǺϷ������ӡ�ͬʱ���Ľ����ӷ������Ĺ��ܡ�

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
                    TileList.ItemsSource = null;//����󶨣���ֹ�ظ����ݡ��˴�������TileList.ItemsSource��ֵ֮ǰ���ã�����ᵼ�������ظ��󶨡�
                    List<string> users = new();
                    List<Reply> ReplyList = new List<Reply>();
                    string hideurl = "/Assets/hide.gif";
                    foreach (var ATile in TileArray)
                    {
                        string TileText = ATile.ToString();
                        var TilePair = JsonConvert.DeserializeObject<Dictionary<string, object>>(TileText);
                        string content = TilePair["content"].ToString();
                        string author = "����";
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
                                        //�˴�����һ����idΪkey���ֵ䣬�洢ͷ����Ϣ��
                                        PortDict[id] = purl;
                                    }
                                    //�˴���һ����ѧ���⣬�Ϸ��Ѿ����й�һ��Clear������Ϊʲô�������ظ����ݣ�ԭ����TileList.ItemsSource�İ�û�б������������TileList.ItemsSource����һ�μ���ʱ��Ȼ��������һ�ε����á����������ÿ�μ���ǰ�����replies���ϣ�Ȼ�����°󶨡�
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
                                    replies.Add(new Reply { text="�������ӳ���:"+ex.Message});
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
                    
                    RootViewer.ScrollToVerticalOffset(0);//������һҳ�����Ϸ��������������������load����֮����ã�����ui�л��߼�����
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
                                //de.Text = "�շ���";
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
                        var Operation=await DownLoadDialog.ShowAsync();
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
                            if(filematch.Success)
                            {
                                DownloadLocation = DownloadsFolder+"\\"+filematch.Value;
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
                                catch(Exception ex)
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
                {"Mode","0"},//�ظ�����Ϊ0������Ϊ1�������⡢ͶƱΪ2
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
                    m.Items.Add(new MenuFlyoutItem { Text = "��ȡ�ղؼ�ʧ��:" + ex.Message, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.CloudError } });
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
                    request.Content = content;//���մ˸�ʽ���Ϳյ�post������������ͷ
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
                    m.Items.Add(new MenuFlyoutItem { Text = "��ȡ�ղؼ�ʧ��:" + ex.Message, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.CloudError } });
                }
                //��issue���ղؼ���Ӧ��ÿ�ν���topicҳ��ʱ����ȡ���ӽ�ʡ��Դ�ĽǶȣ�ֻ��Ӧ������ʱ��ȡһ�μ��ɡ�
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
                            return new KeyValuePair<string, string>("anchor",link );//��������¥��
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
            private string _floor;//¥����
            
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
        
        public static class UBBToMarkdownConverter
        {
            public static string Convert(string ubbText, bool IsImageVisible, bool escapeMarkdown =false)
            {
                var text = Preprocess(ubbText);

                // ����鼶Ԫ�أ����ȼ��Ӹߵ��ͣ�
                text = ConvertCodeBlocks(text);
                text = ConvertQuotes(text);
                text = ConvertLists(text);

                // ��������Ԫ��
                text = ConvertImages(text,IsImageVisible);
                text = ConvertLinks(text);
                text=ConvertEmoji(text);
                text=ConvertColor(text);
                //text=ConvertBold(text);
                text = ConvertTextStyles(text);
                
                
                // �����ʽ
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
            //�����ո��\n��markdown�ؼ��Ļ��и�ʽ��\n��\r�ǲ���ϵͳ�س����ĸ�ʽ���ַ���"\n"��cc98�����ı��Ļ��и�ʽ��
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

                // ʹ��ջ������Ƕ�ײ㼶��ʵ����ջ�������ã�ֻ��Ϊ�˽���ջ��˼�롣
                
                StringBuilder output = new StringBuilder();
                int currentLevel = 0;

                // ��ǰ������ı�
                int lastIndex = 0;

                // ����ƥ���ǩ�������滻
                foreach (Match match in Regex.Matches(input, pattern))
                {
                    // ��ȡ��ǩ�Ŀ�ʼλ��
                    int matchStart = match.Index;
                    // ��ȡ��ǩ�Ľ���λ��
                    int matchEnd = match.Index + match.Length;

                    // �ȴ����ǩ֮ǰ���ı�


                    if (match.Value == "[quote]")
                    {
                        output.Append(input.Substring(lastIndex, matchStart - lastIndex));
                        // ���� [quote] ��ǩ�����Ӳ㼶������ջ
                        
                        currentLevel++;
                        // ��� Markdown ��ʽ������
                        output.Append(new string('>', currentLevel) + " ");
                    }
                    else if (match.Value == "[/quote]")
                    {
                        output.Append(new string(input.Substring(lastIndex, matchStart - lastIndex).Replace("\n", "  \n" + new string('>', currentLevel)).Replace("\r", "  \n" + new string('>', currentLevel)) + "  \n" + new string('>', currentLevel - 1) + "  \n" + new string('>', currentLevel - 1)));
                        // ���� [/quote] ��ǩ�����ٲ㼶������ջ.
                        //��ϵͳ���з��滻Ϊ���÷��ŷǳ��ؼ���������ȷ�Ļ��нṹ�����õ������á�
                        currentLevel--;
                        
                        // ������ı���ֻ��Ҫ�رյ�ǰ�㼶������
                    }

                    // ���� lastIndex
                    lastIndex = matchEnd;
                }

                // �������һ����ǩ֮����ı�
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
            "![#**ͼƬ**#]($1)",
            RegexOptions.IgnoreCase);
                }
                else
                {
                    return Regex.Replace(input,
            @"\[img\](.*?)\[/img\]",
            "[#**ͼƬ**#]($1)",
            RegexOptions.IgnoreCase);
                }
                

            }
            private static string ConvertColor(string input)
            {
                string pattern = @"\[color=[^\]]*\](.*?)\[/color\]";

                // ѭ���������ȥ��Ƕ�׵� color ��ǩ
                string result = input;
                while (Regex.IsMatch(result, pattern))
                {
                    result = Regex.Replace(result, pattern, "$1");
                }
                return result;
            }
           
            private static string ConvertLinks(string input)
            {
                // ����������� [url=...]...[/url]
                input = Regex.Replace(input,
                @"\[url=(.*?)\](.*?)\[/url\]",
                "[$2]($1)",
                RegexOptions.IgnoreCase);

                // �ޱ������� [url]...[/url]
                return Regex.Replace(input,
                    @"\[url\](.*?)\[/url\]",
                    "[$1]($1)",
                    RegexOptions.IgnoreCase);
            }
            private static string ConvertEmoji(string input)
            {
                var replacements = new[]
                {
                    (@"\[ac(\d{2})\]","![#ac$1#](https://www.cc98.org/static/images/ac/$1.png)"),//ac��
                    (@"\[em(\d{2})\]","![#em$1#](https://www.cc98.org/static/images/em/em$1.gif)"),//����
                    (@"\[([a-zA-Z]{2})(\d{2})\]","![#$1$2#](https://www.cc98.org/static/images/$1/$1$2.png)"),//���ɣ�ȸ��
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
            (@"<img\s+[^>]*src=""([^""]+)""[^>]*>","[#**ͼƬ**#]($1)"),
            (@"@(\S+)\s","[@ $1 ](https://api.cc98.org/user/name/$1)"),
            (@"\[audio\](.*?)\[/audio\]","[#**��Ƶ**#]($1)"),
            (@"\[video\](.*?)\[/video\]","[#**��Ƶ**#]($1)"),
            (@"\[upload\](.*?)\[/upload\]","[#**�ļ�**#]($1)")
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
                // �ϲ��������
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
