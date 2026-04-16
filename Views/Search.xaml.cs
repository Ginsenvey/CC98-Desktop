using ABI.System;
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using CC98.Services.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Web;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.OnlineId;
using Windows.Storage;
using DevWinUI;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Search : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<TopicInfo> topics = [];
        public Search()
        {
            this.InitializeComponent();
            SearchList.ItemsSource = topics;
        }
        public string key = "";
        public SearchType type = SearchType.Topic;
        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var args = e.TryGetParameter<SearchNavigationInfo>();

            if (args!=null)
            {
                type = args.SearchType;
                key = args.Key;
                switch (type)
                {
                    case SearchType.Topic:
                        await SearchTopic(HttpUtility.UrlEncode(key), currentIndex);
                        break;
                    case SearchType.User:
                        SearchUser(key);//弃用
                        break;
                }
            }
            


        }

        private void SearchUser(string key)
        {
            string url = ApiEndpoints.User.SearchUserByName(key) ;
            //替换实现
            
        }

        private async Task<bool> SearchTopic(string key,int start)
        {
            string searchUrl = ApiEndpoints.Topic.SearchTopic(key,start);
            var searchResult = await RequestSender.Fetch<List<TopicInfo>>(searchUrl);
            if (!searchResult.IsSuccess|| searchResult.Data== null)
            {
                //
                return false;
            }
            var data = searchResult.Data;

            topics.AddRange(data);
            if (data.Count>0)return true;
            
            return false;
            
        }
        public int currentIndex = 0;
        private async void NaviBar_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b != null)
            {
                string tag = b.Tag.ToString();
                if (tag == "Back")
                {
                    if (currentIndex > 0)
                    {
                        currentIndex -= 20;
                        if(!await SearchTopic(key, currentIndex))
                        {
                            currentIndex += 20;
                        }
                    }
                    else
                    {
                        currentIndex = 0;
                        Flower.Play("\uE946", "已到达最新页面");
                    }
                }
                else if (tag == "Forward")
                {
                    currentIndex += 20;
                    if(!await SearchTopic(key, currentIndex))
                    {
                        currentIndex -= 20;
                    }
                }
                
                PageIndex.Text = "第 " + (currentIndex / 20 + 1).ToString() + " 页";
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
