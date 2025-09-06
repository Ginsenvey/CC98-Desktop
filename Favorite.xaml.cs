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
using CCkernel;
using FluentIcons.Common;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Favorite : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<StandardPost> tiles=new();
        public FavoriteGroup SelectedFavo { get; set; }
        public ObservableCollection<FavoriteGroup> Favos = new()
        {
            new FavoriteGroup{GroupName="默认分组",Id="0"}
        };
        public int SortId = 0;
        public Favorite()
        {
            this.InitializeComponent();
        }
        public string mode = "0";
        public string groupid = "0";
        
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as Dictionary<string,string>;

            if (parameter != null)
            {
                LoadFavorites();
                mode= parameter["mode"];
                groupid=parameter["gid"];
                if (mode =="favorite")
                {
                    Request(mode, "0", ((int)Current_Order).ToString(), groupid);
                }
                
            }
        }
        private void LoadFavorites()
        {
            var f = ValidationHelper.IsTokenExist(Set, "Favorites");
            if (f != "0")
            {
                //likecollection.MenuItems.Clear();
                var LikeList = JsonConvert.DeserializeObject<JArray>(f);
                if(LikeList != null)
                {
                    Favos.Clear();
                    foreach (var like in LikeList)
                    {
                        var likeinfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(like.ToString());
                        string collection = likeinfo["name"].ToString();
                        string sortid = like["id"].ToString();
                        Favos.Add(new FavoriteGroup { Id = sortid, GroupName = collection });
                    }
                }
                
            }
        }
        private async void Request(string mode,string start,string order,string groupid)
        {
            if (mode == "favorite")
            {
                string url = "https://api.cc98.org/topic/me/favorite?from=" + start + "&size=11&order=" + order + "&groupid=" + groupid;
                var r = await CCloginservice.vpn.GetAsync(url);
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string restext = await r.Content.ReadAsStringAsync();
                    var AllTopics = Deserializer.ToArray(restext);
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
                            tiles.Add(new StandardPost { author = "@ " + Author, section = Section, title = Title, pid = Pid, hit = Hit, reply = Reply, rid = AuthorId ,time=Time,sort=(SortId+1).ToString()});
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
                        Request(mode, (current + 1).ToString(), ((int)Current_Order).ToString(), groupid);
                    }
                }
            };
        }

        private void Content_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h != null)
            {
                var t = h?.DataContext as StandardPost;
                if (t != null)
                {
                    Frame.Navigate(typeof(Topic), t.pid);
                }
            }
        }
        public enum PostOrder
        {
            Time = 0,      // 按发帖时间排序
            LastReply = 1, // 按最后回复时间排序
            Mark = 2       // 按收藏顺序排序
        }
        public PostOrder Current_Order = PostOrder.Mark;
        private void ChangeSort_Click(object sender, RoutedEventArgs e)
        {
            Current_Order= (PostOrder)(((int)Current_Order + 1) % 3);
            tiles.Clear();
            history = 0;
            SortId = 0;
            Request(mode, "0", ((int)Current_Order).ToString(), groupid);
            string Sort_Method = string.Empty;
            if (Current_Order == PostOrder.Time)
            {
                Sort_Method = "发帖时间";
                SortIcon.Symbol = FluentIcons.Common.Symbol.History;
            }
            else if (Current_Order == PostOrder.LastReply)
            {
                Sort_Method = "最后回复";
                SortIcon.Symbol = FluentIcons.Common.Symbol.ArrowReply;
            }
            else
            {
                Sort_Method = "收藏顺序";
                SortIcon.Symbol = FluentIcons.Common.Symbol.StarAdd;
            }
            Flower.PlayAnimation("\uE8CB", "切换为" + Sort_Method + "排序");
        }

        private void FavoriteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedFavo != null)
            {
                tiles.Clear();
                history = 0;
                SortId = 0;
                groupid = SelectedFavo.Id;
                de.Text = SelectedFavo.GroupName;
                Request(mode, "0", ((int)Current_Order).ToString(), groupid);
            }
        }
    }
    public class FavoriteGroup
    {
        public string GroupName {  get; set; }
        public string Id { get; set; }
    }
}
