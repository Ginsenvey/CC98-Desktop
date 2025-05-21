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
               
            }
            else
            {
                
            }
        }
        private void Stats(string board)
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
                    //_preparedCount = 0;
                    //RootViewer.IsEnabled = false;
                    replies.Clear();
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
                        string url = "/Assets/hide.gif";
                        if (author != "null" && author != null)
                        {
                            string urlres = await NameToImageUrl(author);
                            if (urlres != "0")
                            {
                                url = urlres;
                            }
                        }
                        replies.Add(new Reply { author = author, text = UBBToMarkdownConverter.Convert(content, mode), like = like, dislike = dislike, rid = rid, time = time, uid = id, url = url,floor=floor+"L" });
                    }
                    TileList.ItemsSource = replies;
                    TileList.UpdateLayout();
                    RootViewer.ScrollToVerticalOffset(0);//������һҳ�����Ϸ��������������������load����֮����ã�����ui�л��߼�����
                    await Task.Delay(100);

                }
                else
                {

                }
            }
        }
        private async Task<string> NameToImageUrl(string name)
        {
            //�˺������ڻ�ȡͷ��url
            string url = "https://api.cc98.org/user/name/" + name;
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
                        return ImageUrl;
                    }
                    catch
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
        
        private void Pager_SelectedIndexChanged(PagerControl sender, PagerControlSelectedIndexChangedEventArgs args)
        {
            //�˷�����ҳ�������ɺ�ᱻ����һ�Σ�Pager��SelectedIndex�ᱻ����Ϊ0��
            //����ҳ�湹�캯��������Ҫ��������LoadReply������
            int index = Pager.SelectedPageIndex;
            CurrentPage = index;
            
            if (index >= 0)
            {
                int startindex = 10 * (index);
                LoadReply(Set.Values["CurrentTopicId"] as string, startindex.ToString(),IsImageVisible);

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
            string flag= "0";
            string[] picformats=new string[]
            {
                "jpg",
                "jpeg",
                "png",
                "gif",
                "webp",
                
            };
            string[] audioformats= new string[]
            {
                "mp3",
                "wav",
                "m4a",
                "ogg",
                "flac",
            };
            string[] videofromats = new string[]
            {
                "mp4",
                "avi",
                "mkv",
                "mov",
                "wmv",
            };
            foreach (var format in picformats)
            {
                if (url.Contains(format))
                {
                    flag = "1";
                    break;
                }
            }
            foreach (var format in audioformats)
            {
                if (url.Contains(format))
                {
                    flag = "2";
                    break;
                }
            }
            foreach (var format in videofromats)
            {
                if (url.Contains(format))
                {
                    flag = "3";
                    break;
                }
            }
            if (flag=="1")
            {
                try
                {
                    var picviewer = new MediaViewer(url);
                    picviewer.Activate();
                }
                catch
                {

                }
                
                
            }
            else if (flag=="2")
            {
                //AudioPlayer.PlacementTarget = sender as MarkdownTextBlock;
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
                catch(Exception ex)
                {
                    AudioName.Text = ex.Message;
                }
                
            }
            else
            {
                var result = LinkAnalyzer.LinkDefinite(url);
                if (result.Key == "topic")
                {
                    LoadMetaData(result.Value);
                    LoadReply(result.Value, "0",IsImageVisible);
                }
                else if (result.Key == "user")
                {
                    string _url = "https://api.cc98.org/user/name/" + result.Value;
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
                }
            }

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
                else if (tag == "2")
                {
                    string favoriteurl = "https://api.cc98.org/me/favorite/"+ (Set.Values["CurrentTopicId"] as string) + "?groupid=0";
                    var request = new HttpRequestMessage(HttpMethod.Put, favoriteurl);
                    var content = new StringContent("", Encoding.UTF8, "application/json");
                    request.Content = content;//���մ˸�ʽ���Ϳյ�post������������ͷ
                    var res = await loginservice.client.SendAsync(request);
                    de.Text = res.StatusCode.ToString();
                    if (res.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        metadata.variant = IconVariant.Color;
                    }
                }
                else
                {
                    LoadReply(Set.Values["CurrentTopicId"] as string, (CurrentPage*10).ToString(), !IsImageVisible);
                    
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
                    else
                    {
                        return new KeyValuePair<string, string>("other", link);
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
                    "<$1>",
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
            (@"\[center\](.*?)\[/center\]","$1  "),
            (@"\<center\>(.*?)\</center\>","$1  "),
            (@"<p[^>]*>(.*?)</p>","$1  "),
            (@"\[align=[^\]]+\](.*?)\[/align\]","$1  "),
            (@"<img\s+[^>]*src=""([^""]+)""[^>]*>","[#**ͼƬ**#]($1)"),
            (@"@(\S+)\s","[@ $1 ](https://api.cc98.org/user/name/$1)"),
            (@"\[audio\](.*?)\[/audio\]","[#**��Ƶ**#]($1)"),
            (@"\[video\](.*?)\[/video\]","[#**��Ƶ**#]($1)")
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
