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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static App3.Index;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Repeater : Page
    {
        public ObservableCollection<Tile> tiles;
        public int SortId = 0;
        public Repeater()
        {
            this.InitializeComponent();
            tiles = new ObservableCollection<Tile>();
        }
        public string mode = "0";
        public string groupid = "0";
        public string sortmrthod = "0";
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as Dictionary<string,string>;

            if (parameter != null)
            {
                mode= parameter["mode"];
                groupid=parameter["gid"];
                de.Text = parameter["name"];
                if (mode =="favorite")
                {
                    Request(mode, "0", "0", groupid);
                }
                
            }
            else
            {

            }
        }
        private async void Request(string mode,string start,string order,string groupid)
        {
            if (mode == "favorite")
            {
                string url = "https://api.cc98.org/topic/me/favorite?from=" + start + "&size=11&order=" + order + "&groupid=" + groupid;
                var r = await MainWindow.loginservice.client.GetAsync(url);
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string restext = await r.Content.ReadAsStringAsync();
                    var AllTopics = JsonConvert.DeserializeObject<JArray>(restext);
                    if (AllTopics != null)
                    {
                        foreach (var Topic in AllTopics)
                        {
                            var TopicInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(Topic.ToString());
                            string Author = "匿名";
                            if (TopicInfo["userName"] != null)
                            {
                                Author = TopicInfo["userName"].ToString();
                            }
                            string AuthorId = "-1";
                            if (TopicInfo["userId"] !=null)
                            {
                                AuthorId = TopicInfo["userId"].ToString();
                            }
                            string Section = TopicInfo["boardName"].ToString();
                            string Time = TopicInfo["time"].ToString();
                            string Title = TopicInfo["title"].ToString();
                            string Pid = TopicInfo["id"].ToString();
                            string Hit = TopicInfo["hitCount"].ToString();
                            string Reply = TopicInfo["replyCount"].ToString();
                            tiles.Add(new Tile { author = "@ " + Author, section = Section, title = Title, uid = Pid, hit = Hit, reply = Reply, rid = AuthorId ,time=Time,sort=(SortId+1).ToString()});
                            SortId++;
                        }
                    }
                }
            }
                
        }
        public int history = 0;
        private void Collection_Loaded(object sender, RoutedEventArgs e)
        {
            Collection.ElementPrepared += (s, e) =>
            {
                if (Collection.ItemsSource != null)
                {
                    int current = e.Index;
                    
                    if (current > 0 && (current + 1) % 11 == 0&&current>history)
                    {
                        history = current;
                        Request(mode, (current + 1).ToString(), sortmrthod, groupid);
                    }
                }
            };
        }

        private void TileContent_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h != null)
            {
                var t = h?.DataContext as Tile;
                if (t != null)
                {
                    Frame.Navigate(typeof(Topic), t.uid);
                }
            }
        }
    }
}
