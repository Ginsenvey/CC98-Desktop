using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static App3.Profile;
using Windows.Storage;
using Windows.UI.Core.Preview;
using static App3.Topic;
using System.Threading.Tasks;
using System.Net.Http;
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
        
        public Board()
        {
            this.InitializeComponent();
            Set = ApplicationData.Current.LocalSettings;
            stiles= new ObservableCollection<STile>()
            {

            };
            STileList.ItemsSource = stiles;
            InitializeClient();
            
        }
        private void InitializeClient()
        {
            if (Set.Values.ContainsKey("Access"))
            {
                MainWindow.loginservice.client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Set.Values["Access"] as string);
            }

        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                GetData(parameter);
                LoadSTiles(parameter);
            }
            else
            {

            }
        }
        private async void GetData(string bid)
        {
            string BoardUrl = "https://api.cc98.org/board/"+bid;
            
            try
            {
                var BoardRes = await MainWindow.loginservice.client.GetAsync(BoardUrl);
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
                            var boarddata = new BoardData()
                            {
                                Name = name,
                                Description = description,
                                Todaycount = "今日帖数:" + todaycount,
                                Totalcount = "总话题数:" + totaltopic,
                                Masters = "版主:" + masters,
                                BanText = UBBToMarkdownConverter.Convert(bantext,false)
                            };
                            BoardBanner.DataContext = boarddata;
                            
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        private async void LoadSTiles(string bid)
        {
            string TileUrl = "https://api.cc98.org/board/"+bid+"/topic?from=0&size=20";
            try
            {
                var TileRes = await MainWindow.loginservice.client.GetAsync(TileUrl);
                if (TileRes.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string TileText = await TileRes.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(TileText))
                    {
                        var TileArray = JsonConvert.DeserializeObject<JArray>(TileText);
                        if (TileArray != null)
                        {
                            if (TileArray.Count > 0)
                            {
                                foreach (var tile in TileArray)
                                {
                                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(tile.ToString());
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
                    }
                }
                else
                {
                    //de.Text = "失败";
                }
            }
            catch(Exception ex)
            {
                //de.Text= ex.Message;
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

        private async void Banner_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            string link=e.Link as string;
            var result=LinkAnalyzer.LinkDefinite(link);
            if (result.Key == "user")
            {
                string url = "https://api.cc98.org/user/name/" + result.Value;
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
            }
            else if (result.Key == "topic")
            {
                Frame.Navigate (typeof(Topic), result.Value);
            }




        }

        private async void writepost_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (Post), null);
        }
    }
}
