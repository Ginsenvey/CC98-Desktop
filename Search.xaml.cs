using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic.FileIO;
using CCkernel;
using System.Net;
using Windows.Storage;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static App3.Profile;
using static App3.Index;
using System.Collections.ObjectModel;
using Windows.Security.Authentication.OnlineId;
using System.Web;
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
            
        }
        
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var p = e.Parameter as Dictionary<string,string>;

            if (p!=null)
            {
               string type = p["type"];
               string key = p["key"];
                if(type== "user")
                {
                    SearchUser(key);
                }
                else if (type == "topic")
                {
                    SearchTopic(HttpUtility.UrlEncode(key));
                }

            }
            


        }

        private void SearchUser(string key)
        {
            throw new NotImplementedException();
        }

        private async void SearchTopic(string key)
        {
            string searchurl = "https://api.cc98.org/topic/search?keyword="+key+"&size=20&from=0";
            var r = await CCloginservice.client.GetAsync(searchurl);
            if (r.StatusCode == HttpStatusCode.OK)
            {
                string SText = await r.Content.ReadAsStringAsync();

                var Posts = JsonConvert.DeserializeObject<JArray>(SText);
                if (Posts != null)
                {
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
                        string title = js["title"].ToString();
                        string hit = js["hitCount"].ToString();
                        string reply = js["replyCount"].ToString();
                        Tiles.Add(new StandardPost { author ="@ "+ author, pid = uid, time = time, title = title, hit = hit, reply = reply,  rid= js["id"].ToString()});
                    }
                    SearchList.ItemsSource = Tiles;
                    
                }
            }
            else
            {
                Tiles.Add(new StandardPost { author = "搜索失败",pid = "0", time = "0", title = "0", hit = "0", reply = "0" });
            }
        }

        private void SearchContent_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as StandardPost;
            if (t != null)
            {
                if (t.pid!= null)
                {
                    Frame.Navigate(typeof(Topic), t.pid);
                    //存在一个问题，我们需要缓存页面的数据源，否则在用户从其中一个帖子返回后，搜索结果是空的。
                }

            }
        }
    }
}
