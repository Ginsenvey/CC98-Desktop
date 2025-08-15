using CCkernel;
using CCUserModel;
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
using static App3.Profile;
using static App3.Topic;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Board : Page
    {
        public ObservableCollection<STile> stiles;
        public ApplicationDataContainer Set;
        public BoardData board_data = new();
        public Board()
        {
            this.InitializeComponent();
            Set = ApplicationData.Current.LocalSettings;
            stiles= new ObservableCollection<STile>()
            {

            };
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
                            board_data = new BoardData()
                            {
                                Name = name,
                                Description = description,
                                Todaycount = "今日帖数:" + todaycount,
                                Totalcount = "总话题数:" + totaltopic,
                                Masters = "版主:" + masters,
                                BanText = UBBConverter.Convert(bantext,true)
                            };
                            BoardBanner.DataContext = board_data;
                            
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void LoadTopics(string bid,string start)
        {       
            string url = "https://api.cc98.org/board/" + bid + "/topic?from=" + start + "&size=20";
            var list = new JArray();
            string res=await RequestSender.SimpleRequest(url);
            if (!res.StartsWith("404"))
            {
                list = Deserializer.ToArray(res);
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
                    string hit = info["hitCount"].ToString();
                    string pid = info["id"].ToString();
                    string author = "匿名";
                    string text = info["title"].ToString();
                    if (info["userName"] != null)
                    {
                        author = info["userName"].ToString();
                    }
                    string reply = info["replyCount"].ToString();
                    stiles.Add(new STile { author = author, hit = hit, reply = reply, pid = pid, text = text, symbol = FluentIcons.Common.Symbol.Note });
                }

            }
        }
        public class BoardData
        {
            public string Name { get; set; }
            public string Totalcount { get; set; }
            public string Todaycount { get; set; }
            public  string Masters { get; set; }
            public string Description { get; set; }
            public string BanText { get; set; }
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
            STileList.ElementPrepared += (s, e) =>
            {
                if (STileList.ItemsSource != null)
                {
                    int current = e.Index;

                    if (current > 0 && (current + 1) % 20 == 0 && current > history)
                    {
                        history = current;
                        LoadTopics(bid, (current + 1).ToString());
                    }
                }
            };
        }

        private async void Gooey_Click(object sender, RoutedEventArgs e)
        {
            GooeyGroup.Distance += 10;
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
                                    IconSymbol = BoardIcon.GetSymbol(bid, board_data.Name),
                                    Name = board_data.Name,
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
                        case "Vote":
                            break;
                    }
                        
                }
            }
            
        }
    }
}
