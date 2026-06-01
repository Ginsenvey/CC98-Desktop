using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using CC98.Services.Extensions;
using CC98.Services.Helpers;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class IndexPage
{
    private readonly IndexDataService _indexService;
    public ObservableCollection<FlipTopic> FlipTopics = [];


    public bool IsOnlineMode = false;
    public string NaviCode = "";
    public ObservableCollection<SectionCard> Sections = [];
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    public ImageSource? ThemePic;

    public IndexPage()
    {
        InitializeComponent();
        _indexService = IndexDataService.Instance;
        LoadSet();
    }


    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadFromCacheAsync();
    }

    private void LoadSet()
    {
        var theme = ValidationHelper.GetValue(Set, "ThemePic");
        if (theme != "0") ThemePresenter.ImageSource = new BitmapImage(new(theme));
    }

    private async Task LoadFromCacheAsync()
    {
        //只从缓存中读取。
        (string Key, string DisplayName)[] sections =
[
    ("hotTopic", "十大话题"),
    ("schoolEvent", "校园活动"),
    ("academics", "学术通知"),
    ("study", "学习天地"),
    ("emotion", "感性·情感"),
    ("fleaMarket", "跳蚤市场"),
    ("fullTimeJob", "求职广场"),
    ("partTimeJob", "实习兼职")
];
        Sections.Clear();
        FlipTopics.Clear();
        for (var i = 0; i < sections.Length; i++)
        {
            var propertyName = sections[i].Key;
            var name = sections[i].DisplayName;
            var topics = await IndexDataService.GetTopicPartitionAsync(propertyName);
            var section = new SectionCard
                { SectionName = name, IndexTopics = topics, HexColor = ColorEx.GenerateMorandiColorHex() };
            Sections.Add(section);
        }

        var recommendations = await IndexDataService.GetRecommendationReadingAsync();
        if (recommendations == null)
        {
            await App.Logger.WriteAsync("Index", "获取推荐阅读列表失败");
            return;
        }

        foreach (var item in recommendations) item.Url = $"cc98:/{item.Url}";
        FlipTopics.AddRange(recommendations);
        Pips.NumberOfPages = recommendations.Count;
    }


    private void ContentCard_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var h = sender as HyperlinkButton;
        var translate = h?.RenderTransform as TranslateTransform;
        UiEx.AnimateCard(translate, 0, -5); // 向上方移动
    }

    private void ContentCard_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var h = sender as HyperlinkButton;
        var translate = h?.RenderTransform as TranslateTransform;
        UiEx.AnimateCard(translate, 0, 0); // 恢复原位
    }

    private void TopicItem_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as HyperlinkButton;
        var tag = button?.Tag;
        if (tag == null) return;
        var param = new TopicNavigationInfo { TopicId = tag.ToInt() };
        Frame.Navigate(typeof(TopicPage), param);
    }


    private void RecomHyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        var url = sender.NavigateUri.ToString();
        if (!string.IsNullOrEmpty(url))
        {
            var topicId = int.Parse(url.Replace("cc98://topic/", ""));
            var param = new TopicNavigationInfo { TopicId = topicId };
            Frame.Navigate(typeof(TopicPage), param);
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
                var url = ApiEndpoints.Forum.Index;
                var success = await IndexDataService.Instance.RefreshFromApiAsync(url);
                if (success)
                    await LoadFromCacheAsync();
                else
                    Flower.Play(FlowStatus.Fail, "刷新首页失败");
                break;
            case "search":

                break;
            case "appcenter":
                await Launcher.LaunchUriAsync(new(ApiEndpoints.Forum.AppCenter));
                break;
            case "lottery":
                //网页端OpenID未注册权限，不支持抽卡
                if (AppSettings.Current.ActiveMode== (int)ActiveMode.ByPassword)
                {
                    Flower.Play(FlowStatus.Info, "当前登录方式不支持抽卡");
                    return;
                }

                Frame.Navigate(typeof(GamePage));
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
        var data = await IndexDataService.LoadFromCacheAsync();
        if (data == null) return;
        ForumStatList.ItemsSource = new List<CardStatInfoPair>
        {
            new() { StatItem = "今日帖数", Value = data.TodayCount },
            new() { StatItem = "今日主题数", Value = data.TodayTopicCount },
            new() { StatItem = "全站帖数", Value = data.PostCount },
            new() { StatItem = "全站话题", Value = data.TopicCount },
            new() { StatItem = "在线用户", Value = data.OnlineUserCount },
            new() { StatItem = "全站用户", Value = data.UserCount }
        };
        welcome.Text = $"欢迎新用户 {data.LastUserName}";
    }

    private void ForumStat_Unloaded(object sender, RoutedEventArgs e)
    {
        welcome.Text = "";
        ForumStatList.ItemsSource = null;
        ForumStat.Target = null;
    }
}