using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Symbol = FluentIcons.Common.Symbol;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Favorite : Page
{
    public PostOrder CurrenOrder = PostOrder.Mark;
    public ObservableCollection<Favorites> FavoritesList = [];
    public int GroupId;
    public Increment Increment = new();
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    public int SortId;
    public ObservableCollection<SimpleTopicInfo> Topics = [];

    public Favorite()
    {
        InitializeComponent();
    }

    public Favorites? SelectedFavorites { get; set; }


    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        LoadFavorites();
        await GetFavoriteTopic();
    }

    private void LoadFavorites()
    {
        var f = ValidationHelper.GetValue(Set, "Favorites");
        if (f != "0")
        {
            //likecollection.MenuItems.Clear();
            var data = JsonSerialize.Deserialize<List<Favorites>>(f);
            if (data != null)
            {
                FavoritesList.Clear();
                FavoritesList.AddRange(data);
            }
        }
    }

    private async Task<bool> GetFavoriteTopic()
    {
        var favoriteTopicUrl = ApiEndpoints.Topic.FavoriteTopicList(Increment.StartIndex, (int)CurrenOrder, GroupId);
        var favoriteTopicResult = await RequestSender.Fetch<List<SimpleTopicInfo>>(favoriteTopicUrl);
        if (!favoriteTopicResult.IsSuccess || favoriteTopicResult.Data == null)
            //
            return false;
        var data = favoriteTopicResult.Data;
        if (data.Count == 11) data.RemoveAt(10);
        Increment.HasMore = data.Count == Increment.PageSize;
        Topics.AddRange(data);
        return true;
    }


    private void Content_Click(object sender, RoutedEventArgs e)
    {
        var h = sender as HyperlinkButton;
        if (h?.DataContext is not SimpleTopicInfo t) return;
        var param = new TopicNavigationInfo { TopicId = t.Id };
        Frame.Navigate(typeof(Topic), param);
    }


    private async void ChangeSort_Click(object sender, RoutedEventArgs e)
    {
        CurrenOrder = (PostOrder)(((int)CurrenOrder + 1) % 3);
        Topics.Clear();
        Increment.Clear();

        await GetFavoriteTopic();
        string sortMethod;
        if (CurrenOrder == PostOrder.Time)
        {
            sortMethod = "发帖时间";
            SortIcon.Symbol = Symbol.History;
        }
        else if (CurrenOrder == PostOrder.LastReply)
        {
            sortMethod = "最后回复";
            SortIcon.Symbol = Symbol.ArrowReply;
        }
        else
        {
            sortMethod = "收藏顺序";
            SortIcon.Symbol = Symbol.StarAdd;
        }

        Flower.Play("\uE8CB", "切换为" + sortMethod + "排序");
    }

    private async void FavoriteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedFavorites != null)
        {
            Topics.Clear();
            Increment.Clear();
            SortId = 0;
            GroupId = SelectedFavorites.Id;
            de.Text = SelectedFavorites.Name;
            await GetFavoriteTopic();
        }
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        var m = sender as MenuFlyoutItem;
        if (m != null)
            if (m?.DataContext is SimpleTopicInfo)
            {
                //bool res=await RequestSender.RemoveFavorite(t.Id);
                var res = true; //待实现
                if (res)
                {
                    Topics.Clear();
                    Increment.Clear();
                    SortId = 0;
                    await GetFavoriteTopic();
                    Flower.Play("\uE930", "已取消收藏");
                }
                else
                {
                    Flower.Play("\uEA39", "取消收藏失败");
                }
            }
    }

    private async void FavoriteTopicRepeater_ElementPrepared(ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args)
    {
        await Increment.LoadMore(args.Index, GetFavoriteTopic);
    }
}