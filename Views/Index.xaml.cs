
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using DevWinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using static CC98.Kernel.ApiScope.ApiEndpoints;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Index : Page
    {
        public ObservableCollection<SectionCard> sections=[];
        public ObservableCollection<FlipTopic> flipTopics=[];
        public IndexDataService.ForumStatistics? Statistics { get; private set; }
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        private readonly IndexDataService indexService;
        public ImageSource? ThemePic;
        public Index()
        {
            this.InitializeComponent();
            indexService=IndexDataService.Instance;
            LoadSet();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadFromCacheAsync();
        }

        private void LoadSet()
        {
            var _Theme = ValidationHelper.GetValue(Set, "ThemePic");
            if (_Theme != "0")
            {
                ThemePresenter.ImageSource = new BitmapImage(new Uri(_Theme));
            }
        }
        private async Task LoadFromCacheAsync()
        {
            //只从缓存中读取。
            List<string> SectionNames = ["hotTopic", "schoolEvent", "academics", "study", "emotion", "fleaMarket", "fullTimeJob", "partTimeJob"];
            List<string> _SectionNames = ["十大话题", "校园活动", "学术通知", "学习天地", "感性·情感", "跳蚤市场", "求职广场", "实习兼职"];
            sections.Clear();
            flipTopics.Clear();
            for(int i=0; i<SectionNames.Count; i++)
            {
                string propertyName = SectionNames[i];
                string name= _SectionNames[i];
                var hotTopics = await indexService.GetTopicPartitionAsync(propertyName);
                var section=new SectionCard { SectionName=name,IndexTopics=hotTopics,HexColor=ColorEx.GenerateMorandiColorHex()};
                sections.Add(section);
            }
            var recommendations = await indexService.GetRecommendationReadingAsync();
            if (recommendations == null)
            {
                await App.Logger.WriteAsync("Index", "获取推荐阅读列表失败");
                return;
            }
            foreach (var item in recommendations)
            {
                item.Url = $"cc98:/{item.Url}";
            }
            flipTopics.AddRange(recommendations);
            Pips.NumberOfPages = recommendations.Count;         
        }

        
        private void ContentCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var translate = h?.RenderTransform as TranslateTransform;
            UIEx.AnimateCard(translate, 0, -5); // 向上方移动
        }

        private void ContentCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var translate = h?.RenderTransform as TranslateTransform;
            UIEx.AnimateCard(translate, 0, 0); // 恢复原位
        }
        

        public bool IsOnlineMode = false;
        public string NaviCode = "";
        private void TopicItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as HyperlinkButton;
            var tag = button?.Tag;
            if (tag == null) return;
            var param = new TopicNavigationInfo { TopicId = tag.ToInt()};
            Frame.Navigate(typeof(Topic), param);
        }

        

        private void RecomHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            string url = (sender as Hyperlink).NavigateUri.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                int topicId=int.Parse(url.Replace("cc98://topic/", ""));
                var param=new TopicNavigationInfo { TopicId = topicId };
                Frame.Navigate(typeof(Topic),param);
            } 
        }

        private async void IndexAction_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as AppBarButton;
            if (button == null) return;
            if (button.Tag is not string tag) return;
            switch (tag)
            {
                case "refresh":
                    string url = ApiEndpoints.Forum.Index();
                    bool success= await IndexDataService.Instance.RefreshFromApiAsync(url);
                    if (success)
                    {
                        await LoadFromCacheAsync();
                    }
                    else
                    {
                        Flower.Play(FlowStatus.Fail, "刷新首页失败");
                    }
                        break;
                case "search":

                    break;
                case "appcenter":
                    await Launcher.LaunchUriAsync(new Uri(ApiEndpoints.Forum.AppCenter));
                    break;
                case "lottery":
                    //网页端OpenID未注册权限，不支持抽卡
                    if (ValidationHelper.GetValue(Set, "IsActive") != "1")
                    {
                        Flower.Play("\uEA39", "当前登录方式不支持抽卡");
                        return;
                    }
                    Frame.Navigate(typeof(Game));
                    break;
                case "stat":
                    ForumStat.XamlRoot = RootGrid.XamlRoot;
                    ForumStat.IsOpen = true;
                    await LoadForumStat();
                    break;
            }
        }
        private async Task LoadForumStat()
        {
            var stats = await IndexDataService.Instance.GetStatisticsAsync();
            if (stats == null) return;
            ForumStatList.ItemsSource = new List<CardStatInfoPair>
    {
        new() { StatItem = "今日帖数", Value = stats.TodayCount },
        new() { StatItem = "今日主题数", Value = stats.TodayTopicCount },
        new() { StatItem = "全站帖数", Value = stats.PostCount },
        new() { StatItem = "全站话题", Value = stats.TopicCount },
        new() { StatItem = "在线用户", Value = stats.OnlineUserCount },
        new() { StatItem = "全站用户", Value = stats.UserCount }
    };
            welcome.Text = $"欢迎新用户 {stats.LastUserName}";
        }

        private void ForumStat_Unloaded(object sender, RoutedEventArgs e)
        {
            welcome.Text = "";
            ForumStatList.ItemsSource = null;
            ForumStat.Target = null;
        }
    }
    
}
