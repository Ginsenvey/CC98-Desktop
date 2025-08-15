using ABI.System;
using CCkernel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.OnlineId;
using Windows.Storage;
using static App3.Index;
using static App3.Profile;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Search : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<StandardPost> Tiles = new();
        public Search()
        {
            this.InitializeComponent();
            SearchList.ItemsSource = Tiles;
        }
        public string key = "";
        public string type = "";
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var p = e.Parameter as Dictionary<string,string>;

            if (p!=null)
            {
               type = p["type"];
               key = p["key"];
                if(type== "user")
                {
                    SearchUser(key);//弃用
                }
                else if (type == "topic")
                {
                    SearchTopic(HttpUtility.UrlEncode(key),"0");
                }

            }
            


        }

        private async void SearchUser(string key)
        {
            string url = $"https://api.cc98.org/user/name/{key}" ;
            string infotext = await RequestSender.SimpleRequest(url);
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
        }

        private async void SearchTopic(string key,string start)
        {
            string searchurl = $"https://api.cc98.org/topic/search?keyword={key}&size=20&from={start}";
            var r = await CCloginservice.vpn.GetAsync(searchurl);
            if (r.StatusCode == HttpStatusCode.OK)
            {
                string SText = await r.Content.ReadAsStringAsync();

                var Posts = JsonConvert.DeserializeObject<JArray>(SText);
                if (Posts != null)
                {
                    Tiles.Clear();
                    foreach (var post in Posts)
                    {
                        var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(post.ToString());
                        string author = "匿名";
                        string uid = "0";
                        if (js["userId"] != null)
                        {
                            author = js["userName"].ToString();
                            uid = js["userId"].ToString();
                        }
                        string time = js["time"].ToString();
                        string pid = js["id"].ToString();
                        string title = js["title"].ToString();
                        string hit = js["hitCount"].ToString();
                        string reply = js["replyCount"].ToString();
                        Tiles.Add(new StandardPost { author ="@ "+ author, pid = pid, time = time, title = title, hit = hit, reply = reply,  rid= uid});
                    }
                    
                    
                }
            }
            else
            {
                Tiles.Add(new StandardPost { author = "搜索失败",pid = "0", time = "0", title = "0", hit = "0", reply = "0" });
            }
        }
        public int current = 0;
        private void NaviBar_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b != null)
            {
                string tag = b.Tag.ToString();
                if (tag == "Back")
                {
                    if (current > 0)
                    {
                        current -= 20;
                    }
                    else
                    {
                        current = 0;
                        Flower.PlayAnimation("\uE946", "已到达最新页面");
                    }
                }
                else if (tag == "Forward")
                {
                    current += 20;
                }
                SearchTopic(key,current.ToString());
                PageIndex.Text = "第 " + (current / 20 + 1).ToString() + " 页";
                RootViewer.ScrollToVerticalOffset(0);
            }
        }
        private void SearchContent_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var t = h?.Tag;

            if (t != null)
            {
                if (t is string _t)
                {
                    Frame.Navigate(typeof(Topic), _t);
                    
                }

            }
        }

        private void Person_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h != null)
            {
                var tag = h.Tag as string;
                if (tag != null && tag != "0")
                {
                    var param = new Dictionary<string, string>()
                        {
                            {"Mode","Others" },
                            {"UserId",tag }
                        };
                    Frame.Navigate(typeof(Profile), param);
                }
            }
        }
    }
}
