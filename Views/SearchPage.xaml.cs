using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CC98.Services.Helpers;
using DevWinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Windows.Storage;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace CC98.Views;


/// <summary>
/// 搜索页面:AutoSuggestBox 实时搜索(300ms 防抖)、四状态切换、历史搜索缓存、
/// 下拉推荐(话题/用户/版面/主题ID)、Increment 翻页、结果排序。
/// </summary>
public sealed partial class SearchPage : Page
{
    /// <summary>搜索结果集合(结果卡片绑定)。</summary>
    public ObservableCollection<TopicInfo> SearchResults { get; } = [];
    public ObservableCollection<SearchSuggestion> Suggestions { get; } = [];

    /// <summary>历史搜索关键词。</summary>
    public ObservableCollection<string> SearchHistory { get; } = [];
    /// <summary>当前搜索关键词(供结果卡片高亮绑定)。</summary>
    public string SearchKeyword { get; set; } = "";

    private ApiService ApiService { get; } = App.Current.GetService<ApiService>();

    /// <summary>历史搜索缓存(JsonFileCache 持久化)。</summary>
    private JsonFileCache<List<string>> HistoryCache { get; } = new(
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "SearchHistory.json"));

    /// <summary>话题搜索分页器(PageSize 与 API size=20 一致)。</summary>
    private Increment Pager { get; } = new(20);

    private CancellationTokenSource? _debounceCts;
    private const int HistoryLimit = 12;
    private string _currentKeyword = "";

    /// <summary>当前排序模式(页面内全局,切换关键词/翻页后保持)。</summary>
    private SearchSortMode _sortMode = SearchSortMode.Default;
    /// <summary>版面缓存(懒加载,用于下拉建议的版面匹配)。</summary>
    private SectionInfo[]? _sections;


    public SearchPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadHistoryAsync();

        // 兼容从别处携带关键词跳转(如 IndexPage 推荐)
        var args = e.TryGetParameter<SearchNavigationInfo>();
        if (args != null && !string.IsNullOrWhiteSpace(args.Key))
        {
            SearchBox.Text = args.Key;
            await PerformSearchAsync(args.Key);
        }
        else
        {
            ShowEmptyState();
        }

        SearchBox.Focus(FocusState.Programmatic);
    }

    #region 历史搜索

    private async Task LoadHistoryAsync()
    {
        var history = await HistoryCache.GetDataAsync();
        if (history == null || history.Count == 0) return;
        foreach (var k in history) SearchHistory.Add(k);
        HistoryLabel.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 保存历史搜索:去重、最新在前、限制条数,写 JsonFileCache。
    /// </summary>
    private async Task SaveHistoryAsync(string keyword)
    {
        var list = new List<string> { keyword };
        foreach (var k in SearchHistory.Where(k => !string.Equals(k, keyword, StringComparison.OrdinalIgnoreCase)))
            list.Add(k);
        if (list.Count > HistoryLimit) list = list.Take(HistoryLimit).ToList();

        SearchHistory.Clear();
        foreach (var k in list) SearchHistory.Add(k);
        HistoryLabel.Visibility = Visibility.Visible;
        await HistoryCache.UpdateDataAsync(list);
    }

    #endregion

    #region 搜索

    /// <summary>
    /// 输入变化:仅生成下拉建议并清除旧结果,不自动搜索。
    /// 搜索只在用户按 Enter(QuerySubmitted)或点击下拉"搜索话题"建议(SuggestionChosen)时触发。
    /// </summary>
    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        var keyword = sender.Text.Trim();

        if (string.IsNullOrEmpty(keyword))
        {
            Suggestions.Clear();
            SearchResults.Clear();
            ShowEmptyState();
            return;
        }
        await BuildQuickSuggestions(keyword);

        // 输入变化:旧结果不再匹配,清除并回到空状态
        SearchResults.Clear();
        ShowEmptyState();
    }
    /// <summary>
    /// 回车提交:立即搜索话题(取消防抖)。
    /// </summary>
    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var keyword = (args.QueryText ?? sender.Text).Trim();
        if (string.IsNullOrEmpty(keyword)) return;
        _debounceCts?.Cancel();
        await PerformSearchAsync(keyword);
    }

    /// <summary>
    /// 选择下拉建议:按建议类型执行跳转/搜索。
    /// </summary>
    private async void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is not SearchSuggestion s) return;
        switch (s.Type)
        {
            case SearchSuggestionType.Topic:
                // 搜索话题:与回车一致
                _debounceCts?.Cancel();
                await PerformSearchAsync(SearchBox.Text.Trim());
                break;
            case SearchSuggestionType.User:
                await OpenUserByNameAsync(s.Parameter ?? SearchBox.Text.Trim());
                break;
            case SearchSuggestionType.UserId:
                if (int.TryParse(s.Parameter, out var uid))
                    Frame.Navigate(typeof(ProfilePage), new ProfileNavigationInfo { UserId = uid });
                break;
            case SearchSuggestionType.Board:
                if (int.TryParse(s.Parameter, out var bid))
                    Frame.Navigate(typeof(BoardPage), bid);
                break;
            case SearchSuggestionType.TopicId:
                if (int.TryParse(s.Parameter, out var tid))
                    Frame.Navigate(typeof(TopicPage), new TopicNavigationInfo { TopicId = tid });
                break;
        }
        Suggestions.Clear();
    }

    /// <summary>
    /// 执行话题搜索(第一页):重置分页器、填充结果、加载头像、更新状态。
    /// </summary>
    private async Task PerformSearchAsync(string keyword, CancellationToken ct = default)
    {
        SearchKeyword = keyword;
        _currentKeyword = keyword;
        Pager.Clear();
        SearchResults.Clear();
        ShowLoadingState();

        var url = ApiEndpoints.Topic.SearchTopic(HttpUtility.UrlEncode(keyword), Pager.StartIndex);
        var result = await ApiService.Fetch<List<TopicInfo>>(url, ct);
        if (ct.IsCancellationRequested) return; // 已被更新的输入取消

        if (!result.IsSuccess || result.Data == null)
        {
            Flower.Play(FlowStatus.Fail, $"搜索失败:{result.Message}");
            ShowEmptyState();
            return;
        }

        var data = SortData(result.Data);
        Pager.HasMore = data.Count == Pager.PageSize;
        var param = string.Join("&", data.Where(x => !x.IsAnonymous && x.UserId.HasValue).Select(x => $"id={x.UserId}").Distinct());
        var userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
        var userInfoResult = await ApiService.Fetch<List<BasicUserInfo>>(userInfoUrl);
        if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
        {
            //报错
            Flower.Play(FlowStatus.Fail, $"加载用户头像失败：{userInfoResult.Message}");
        }

        var userInfoList = userInfoResult.Data;

        foreach (var topic in data) 
        { 
            topic.SearchKeyword = keyword; 
            if (topic.IsAnonymous)
            {
                topic.PortraitUrl = "ms-appx:///Assets/hide.gif";
            }
            else
            {
                var user = userInfoList?.FirstOrDefault(x => x.Id == topic.UserId);
                if (user != null)
                {
                    topic.PortraitUrl = user.PortraitUrl;
                }
            }
            SearchResults.Add(topic);
        }

        if (data.Count > 0)
        {
            ResultCountText.Text = $"找到 {data.Count}+个结果";
            ShowResultState();
            FadeInContent();
        }
        else
        {
            ShowNoResultState();
        }
        UpdatePagerButtons();

        await SaveHistoryAsync(keyword);
    }

    /// <summary>
    /// 翻页加载回调(供 Increment.LoadNextPage / LoadLastPage 使用)。
    /// </summary>
    private async Task<bool> LoadSearchPageAsync()
    {
        SearchResults.Clear();

        var url = ApiEndpoints.Topic.SearchTopic(HttpUtility.UrlEncode(_currentKeyword), Pager.StartIndex);
        var result = await ApiService.Fetch<List<TopicInfo>>(url);
        if (!result.IsSuccess || result.Data == null) return false;
        var data = SortData(result.Data);
        Pager.HasMore = data.Count == Pager.PageSize;

        var param = string.Join("&", data.Where(x => !x.IsAnonymous && x.UserId.HasValue).Select(x => $"id={x.UserId}").Distinct());
        var userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
        var userInfoResult = await ApiService.Fetch<List<BasicUserInfo>>(userInfoUrl);
        if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
        {
            //报错
            Flower.Play(FlowStatus.Fail, $"加载用户头像失败：{userInfoResult.Message}");
        }

        var userInfoList = userInfoResult.Data;

        foreach (var topic in data)
        {
            topic.SearchKeyword = _currentKeyword;
            if (topic.IsAnonymous)
            {
                topic.PortraitUrl = "ms-appx:///Assets/hide.gif";
            }
            else
            {
                var user = userInfoList?.FirstOrDefault(x => x.Id == topic.UserId);
                if (user != null)
                {
                    topic.PortraitUrl = user.PortraitUrl;
                }
            }
            SearchResults.Add(topic);
        }
        ResultCountText.Text = $"找到 {data.Count} 个结果";
        UpdatePagerButtons();
        return true;
    }

    /// <summary>
    /// 按当前排序模式对搜索结果排序。默认模式保持 API 顺序(发帖时间)。
    /// </summary>
    private List<TopicInfo> SortData(List<TopicInfo> data) => _sortMode switch
    {
        SearchSortMode.MostReplies => data.OrderByDescending(t => t.ReplyCount).ToList(),
        SearchSortMode.MostHits => data.OrderByDescending(t => t.HitCount).ToList(),
        _ => data
    };

    /// <summary>
    /// 点击排序按钮:循环切换排序模式,并按新排序重新加载当前关键词第一页。
    /// 排序为页面级全局状态,切换关键词/翻页后保持。
    /// </summary>
    private async void SortButton_Click(object sender, RoutedEventArgs e)
    {
        _sortMode = _sortMode switch
        {
            SearchSortMode.Default => SearchSortMode.MostReplies,
            SearchSortMode.MostReplies => SearchSortMode.MostHits,
            _ => SearchSortMode.Default
        };
        UpdateSortButton();

        // 已有搜索时,按新排序重新加载第一页
        if (!string.IsNullOrEmpty(_currentKeyword))
            await PerformSearchAsync(_currentKeyword);
    }

    /// <summary>更新排序按钮文本。</summary>
    private void UpdateSortButton()
    {
        SortButtonText.Text = _sortMode switch
        {
            SearchSortMode.MostReplies => "排序:回复",
            SearchSortMode.MostHits => "排序:点击",
            _ => "排序:时间"
        };
    }

    /// <summary>
    /// 按用户名搜索用户并打开主页。
    /// </summary>
    private async Task OpenUserByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var url = ApiEndpoints.User.SearchUserByName(name);
        var result = await ApiService.Fetch<UserInfo>(url);
        if (!result.IsSuccess || result.Data == null || result.Data.Id == 0)
        {
            Flower.Play(FlowStatus.Fail, $"未找到用户:{name}");
            return;
        }
        var user = result.Data;
        var info = new ProfileNavigationInfo
        {
            IsMe = user.Id == AppSettings.Current.UserId,
            UserId = user.Id
        };
        Frame.Navigate(typeof(ProfilePage), info);
    }

  

    #endregion

    #region 下拉建议

    /// <summary>
    /// 同步构建快速建议:搜索话题 / 搜索用户 / 数字ID浏览主题或用户。
    /// </summary>
    private async Task BuildQuickSuggestions(string keyword)
    {
        Suggestions.Clear();
        var list = new List<SearchSuggestion>
        {
            new()
            {
                Type = SearchSuggestionType.Topic,
                Title = $"搜索话题:{keyword}"
            },
            new()
            {
                Type = SearchSuggestionType.User,
                Title = $"搜索用户:{keyword}",
                Parameter = keyword
            }
        };

        // 纯数字:直接浏览主题 / 查看用户
        if (int.TryParse(keyword, out var id))
        {
            list.Add(new SearchSuggestion
            {
                Type = SearchSuggestionType.TopicId,
                Title = $"浏览主题 #{id}",
                Parameter = id.ToString()
            });
            list.Add(new SearchSuggestion
            {
                Type = SearchSuggestionType.UserId,
                Title = $"查看用户 #{id}",
                Parameter = id.ToString()
            });
        }
        Suggestions.AddRange(list);
        await AppendBoardSuggestionsAsync(keyword);
    }

    private async Task AppendBoardSuggestionsAsync(string keyword)
    {
        var sections = await GetSectionsAsync();
        if (sections == null) return;

        foreach (var section in sections)
        {
            foreach (var board in section.Boards)
            {
                if (board.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    Suggestions.Add(new SearchSuggestion
                    {
                        Type = SearchSuggestionType.Board,
                        Title = $"版面:{board.Name}",
                        Subtitle = section.Name,
                        Parameter = board.Id.ToString()
                    });
                    if (Suggestions.Count >= 10) break;
                }
            }
            if (Suggestions.Count >= 10) break;
        }
    }


    /// <summary>
    /// 懒加载版面缓存(仅首次,失败返回 null)。
    /// </summary>
    private async Task<SectionInfo[]?> GetSectionsAsync()
    {
        if (_sections != null) return _sections;
        try
        {
            _sections = await BoardSectionManager.Instance.GetSectionDataAsync();
        }
        catch
        {
            _sections = null;
        }
        return _sections;
    }

    #endregion

    #region 翻页

    private async void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (Pager.CurrentPage <= 0) return;
        _debounceCts?.Cancel();
        ShowLoadingState();
        await Pager.LoadLastPage(LoadSearchPageAsync);
        ShowResultState();
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        _debounceCts?.Cancel();
        ShowLoadingState();
        await Pager.LoadNextPage(LoadSearchPageAsync);
        ShowResultState();
    }

    private void UpdatePagerButtons()
    {
        PrevPageButton.IsEnabled = Pager.CurrentPage > 0;
        NextPageButton.IsEnabled = Pager.HasMore;
        PageText.Text = $"第 {Pager.CurrentPage + 1} 页";
    }

    #endregion

    #region 状态切换

    private void ShowEmptyState()
    {
        EmptyState.Visibility = Visibility.Visible;
        LoadingState.Visibility = Visibility.Collapsed;
        NoResultState.Visibility = Visibility.Collapsed;
        ResultState.Visibility = Visibility.Collapsed;
    }

    private void ShowLoadingState()
    {
        EmptyState.Visibility = Visibility.Collapsed;
        LoadingState.Visibility = Visibility.Visible;
        NoResultState.Visibility = Visibility.Collapsed;
        ResultState.Visibility = Visibility.Collapsed;
    }

    private void ShowNoResultState()
    {
        EmptyState.Visibility = Visibility.Collapsed;
        LoadingState.Visibility = Visibility.Collapsed;
        NoResultState.Visibility = Visibility.Visible;
        ResultState.Visibility = Visibility.Collapsed;
    }

    private void ShowResultState()
    {
        EmptyState.Visibility = Visibility.Collapsed;
        LoadingState.Visibility = Visibility.Collapsed;
        NoResultState.Visibility = Visibility.Collapsed;
        ResultState.Visibility = Visibility.Visible;
    }

    /// <summary>结果出现时的淡入动画。</summary>
    private void FadeInContent()
    {
        if (Resources["ContentFadeIn"] is Storyboard storyboard)
            storyboard.Begin();
    }

    #endregion

    #region 交互

    private void HistoryKeyword_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Content is string kw && !string.IsNullOrWhiteSpace(kw))
        {
            SearchBox.Text = kw;
            _debounceCts?.Cancel();
            _ = PerformSearchAsync(kw);
        }
    }

    
    private void ResultCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        //超绝模式匹配语法
        if (sender is Grid { Tag: int topicId })
        {
            Frame.Navigate(typeof(TopicPage), new TopicNavigationInfo { TopicId = topicId });
        }
    }

    /// <summary>Ctrl+K:聚焦搜索框。</summary>
    private void FocusSearch_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SearchBox.Focus(FocusState.Programmatic);
        args.Handled = true;
    }

    /// <summary>ESC:清空搜索框;已清空则返回上一页。</summary>
    private void Escape_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (!string.IsNullOrEmpty(SearchBox.Text))
        {
            SearchBox.Text = "";
            Suggestions.Clear();
            _debounceCts?.Cancel();
            ShowEmptyState();
        }
        else if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        args.Handled = true;
    }

    #endregion

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformSearchAsync(_currentKeyword);
    }
}
