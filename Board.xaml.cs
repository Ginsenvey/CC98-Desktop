
using CommunityToolkit.Mvvm.ComponentModel;
using DevWinUI;
using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.AppBroadcasting;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core.Preview;
using CC98.Services;
using CC98.Kernel;
using CC98.UserExperience;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Board : Page
    {
        public ObservableCollection<STile> stiles=new();
        public ApplicationDataContainer Set;
        public bool isBest = false;
        public BoardData boardData { get; set; } = new() { name = "版面", todayCount = "今日发帖:9898", totalCount = "9898" };
        public Board()
        {
            this.InitializeComponent();
            Set = ApplicationData.Current.LocalSettings;
            STileList.ItemsSource = stiles;
            

        }
        
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                bid = parameter;
                GetData(parameter);
                LoadTopics(parameter,"0");

            }
            else
            {

            }
        }
        public string bid= "0";
        private async void GetData(string bid)
        {
            string BoardUrl = "https://api.cc98.org/board/"+bid;
            
            try
            {
                var BoardRes = await CCloginservice.vpn.GetAsync(BoardUrl);
                if (BoardRes.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string BoardText = await BoardRes.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(BoardText))
                    {
                        var js = JsonConvert.DeserializeObject<Dictionary<string,object>>(BoardText);
                        if (js != null)
                        {
                            var master = JsonConvert.DeserializeObject<JArray>(js["boardMasters"].ToString());
                            string masters = string.Empty;
                            List<string> masterlist = new List<string>();
                            foreach(var user in master)
                            {
                                if (user != null)
                                {
                                    masterlist.Add(user.ToString());
                                }
                            }
                            masters=string.Join(";", masterlist);
                            string description = js["description"].ToString();
                            string name = js["name"].ToString();
                            string todaycount = js["todayCount"].ToString();
                            string totaltopic = js["topicCount"].ToString();
                            string bantext = js["bigPaper"].ToString() ;
                            boardData.name = name;
                            boardData.description = description;
                            boardData.masters = masters;
                            boardData.banText = UBBConverter.Convert(bantext, true);
                            boardData.totalCount = "总话题数:" + totaltopic;
                            boardData.todayCount = "今日帖数:" + todaycount;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async Task LoadTopics(string bid,string start)
        {
            string _url =$"https://api.cc98.org/topic/best/board/{bid}?from={start}&size=20";
            string url = $"https://api.cc98.org/board/{bid}/topic?from={start}&size=20";
            var list = new JArray();
            string res=await RequestSender.SimpleRequest(isBest?_url:url);
            if (!res.StartsWith("404"))
            {
                if (isBest)
                {
                    var js=Deserializer.ToDictionary(res);
                    if (js != null)
                    {
                        string topics = ValidationHelper.GetKey(js,"topics");
                        if (topics != "0")
                        {
                            list= Deserializer.ToArray(topics);
                        }
                    }
                    else
                    {
                        Flower.PlayAnimation("\uEA39", res);
                    }
                }
                else
                {
                    list = Deserializer.ToArray(res);
                }        
            }
            else
            {
                Flower.PlayAnimation("\uEA39", res);
            }
            if (list.Count > 0)
            {
                foreach (var topic in list)
                {
                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(topic.ToString());
                    string hit = ValidationHelper.GetKey(info,"hitCount");
                    string pid = ValidationHelper.GetKey(info,"id");
                    string author = "匿名";
                    string text = ValidationHelper.GetKey(info,"title");
                    if (info["userName"] != null)
                    {
                        author = ValidationHelper.GetKey(info,"userName");
                    }
                    string reply = ValidationHelper.GetKey(info, "replyCount");
                    stiles.Add(new STile { author = author, hit = hit, reply = reply, pid = pid, text = text, symbol =isBest?FluentIcons.Common.Symbol.Star:FluentIcons.Common.Symbol.Note });
                }

            }
        }
        

        private void TileContent_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as STile;
            if (t != null)
            {
                if (t.pid != null)
                {
                    Frame.Navigate(typeof(Topic), t.pid);
                }
            }
            else
            {
                Flower.PlayAnimation("\uE930", "null");
            }
        }

        
        private async void MarkdownTextBlock_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            var url = e.Link.ToString();
            var result = LinkAnalyzer.LinkDefinite(url);
            switch (result.Key)
            {
                case "topic":
                    Frame.Navigate(typeof (Topic), result.Value);
                    break;
                case "user":
                    {
                        string _url = "https://api.cc98.org/user/name/" + result.Value;
                        
                        var PortRes = await CCloginservice.vpn.GetAsync(_url);
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
                //using语句不能在switch语句中直接出现。因此，使用大括号包围这个case.
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
                    break;

                case "backlink":
                    if (result.Value == "bili")
                    {
                        Flower.PlayAnimation("\uE930", "已复制Bili外链");
                    }
                    break;
                default://自动复制到用户剪切板
                    var datapackage = new DataPackage();
                    datapackage.SetText(url);
                    Clipboard.SetContent(datapackage);
                    Flower.PlayAnimation("\uE930", "已复制外部链接");
                    break;
            }

        }
        
        public int history;
        private void STileList_Loaded(object sender, RoutedEventArgs e)
        {
            STileList.ElementPrepared += async (s, e) =>
            {
                if (STileList.ItemsSource != null)
                {
                    int current = e.Index;

                    if (current > 0 && (current + 1) % 20 == 0 && current > history)
                    {
                        history = current;
                        await LoadTopics(bid, (current + 1).ToString());
                    }
                }
            };
        }

        private async void Gooey_Click(object sender, RoutedEventArgs e)
        {
            var s = sender as GooeyButtonItem;
            if (s != null)
            {
                var _tag = s.Tag;
                if (_tag is string tag)
                {
                    switch (tag)
                    {
                        case "Send":
                            var param = new Dictionary<string, string>()
                            {
                                {"Mode","2" },
                                {"BoardId",bid }
                            };
                            Frame.Navigate(typeof(Post), param);
                            break;
                        case "Pin":
                            var r = await RequestSender.EditFocusList("add", bid);
                            if (r == "1")
                            {
                                
                                var i = new NavigationItem
                                {
                                    IconSymbol = BoardIcon.GetSymbol(bid, boardData.name),
                                    Name = boardData.name,
                                    IsEditable = true,
                                    Tag = bid
                                };
                                Messenger.Instance.AddNavigationItem(i);
                            }
                            else
                            {
                                Flower.PlayAnimation("\uEA39", r);
                            }

                            break;
                        case "Best":
                            GooeyGroup.Visibility = Visibility.Collapsed;
                            BackFromBest.Visibility = Visibility.Visible;
                            history = 0;
                            stiles.Clear();
                            isBest = true;
                            try
                            {
                                await LoadTopics(bid, "0");
                                
                            }
                            catch (Exception ex)
                            {
                                Flower.PlayAnimation("\uEA39", ex.Message);
                            }
                            break;
                    }
                        
                }
            }
            
        }

        private async void BackFromBest_Click(object sender, RoutedEventArgs e)
        {
            BackFromBest.Visibility = Visibility.Collapsed;
            GooeyGroup.Visibility = Visibility.Visible;
            isBest= false;
            history = 0;
            stiles.Clear();
            await LoadTopics(bid, "0");
        }
    }
    public partial class BoardData :ObservableObject
    {
        
        private string _name;
        private string _totalCount;
        private string _todayCount;
        private string _masters;
        private string _description;
        private string _banText;
        public string name
        {
            get => _name;
            set=> SetProperty(ref _name, value);
        }
        public string totalCount
        {
            get => _totalCount;
            set=>SetProperty(ref _totalCount, value);
        }
        public string masters
        {
            get => _masters;
            set=>SetProperty(ref _masters, value);
        }
        public string description
        {
            get => _description;
            set=>SetProperty(ref _description, value);
        }
        public string todayCount
        {
            get => _todayCount;
            set=>SetProperty(ref _todayCount, value);
        }
        public string banText
        {
            get => _banText;
            set => SetProperty(ref _banText, value);
        }
    }
}
