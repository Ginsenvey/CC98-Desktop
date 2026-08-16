using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CC98.Services.Extensions;
using CC98.Services.Helpers;
using DevWinUI;
using Microsoft.UI.Xaml.Navigation;

namespace CC98.Views;

/// <summary>
/// 显示版面信息。
/// </summary>
public sealed partial class BoardPage
{
    public ObservableCollection<SimpleTopicInfo> Topics = [];
    
    public ApiService ApiService = App.Current.GetService<ApiService>();

    //是否精华帖
    public bool IsBest { get; } = false;

    public int BoardId { get; set; } = 0;

    public BoardData BoardData { get; } = new();

    public BoardTopicFilterType FilterType { get; set; } = BoardTopicFilterType.Latest;
    public Increment Increment { get; } = new(20);


    public BoardPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var args = e.TryGetParameter<int>();
        BoardId = args;
        BoardSymbol.Symbol = BoardIconHelper.GetSymbol(BoardId, "");
        // 版面信息与主题列表互不依赖,并行加载
        await Task.WhenAll(GetData(), LoadTopics());
    }



    private async Task GetData()
    {
        var boardDataUrl = ApiEndpoints.Board.BoardInfo(BoardId);
        var boardDataResult = await ApiService.Fetch<BoardData>(boardDataUrl);
        if (!boardDataResult.IsSuccess || boardDataResult.Data == null)
        {
            Flower.Play(FlowStatus.Fail, boardDataResult.Message);
            return;
        }

        var data = boardDataResult.Data;
        BoardData.Id = data.Id;
        BoardData.Name = data.Name;
        BoardData.Description = data.Description;
        BoardData.BigPaper = data.BigPaper;
        BoardData.BoardMasters = data.BoardMasters;
        BoardData.TopicCount = data.TopicCount;
        BoardData.TodayCount = data.TodayCount;
    }

    private async Task<bool> LoadTopics()
    {
        var topicUrl = ApiEndpoints.Board.TopicList((int)FilterType, BoardId, Increment.StartIndex);

        if (FilterType == BoardTopicFilterType.Best)
        {
            var result = await ApiService.Fetch<BoardBest>(topicUrl);
            if (result.IsNotValid)
            {
                Flower.Play(FlowStatus.Fail, result.Message);
                return false;
            }
            var bests = result.Data?.Topics;
            Increment.HasMore = bests.Count == Increment.PageSize;
            Topics.AddRange(bests);
            return true;
        }

        var topicResult = await ApiService.Fetch<List<SimpleTopicInfo>>(topicUrl);
        if (topicResult.IsNotValid)
        {
            Flower.Play(FlowStatus.Fail, topicResult.Message);
            return false;
        }

        var data = topicResult.Data;
        Increment.HasMore = data.Count == Increment.PageSize;
        Topics.AddRange(data);
        return true;
    }


    private void TileContent_Click(object sender, RoutedEventArgs e)
    {
        var h = sender as HyperlinkButton;
        if (h?.DataContext is not SimpleTopicInfo t) return;
        var param = new TopicNavigationInfo { TopicId = t.Id };
        Frame.Navigate(typeof(TopicPage), param);
    }




    private async void TopicRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        await Increment.LoadMore(args.Index, LoadTopics);
    }

    private async void BoardAction_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as AppBarButton;
        if (button == null) return;
        if (button.Tag is not string tag) return;
        switch (tag)
        {
            case "browse":
                await Launcher.LaunchUriAsync(new(ApiEndpoints.Board.WebUrl(BoardId)));
                break;
            case "refresh":
                Increment.Clear();
                Topics.Clear();
                await RefreshAsync();
                Flower.Play(FlowStatus.Success, "刷新成功");
                break;
            case "pin":
                await Pin();
                break;
            case "draft":
                var param = new SketchNavigationInfo
                {
                    EditorMode = EditorMode.DraftNewTopic,
                    BoardId = BoardId
                };
                Frame.Navigate(typeof(SketchPage), param);
                break;
            case "vote":
                var param2 = new SketchNavigationInfo
                {
                    EditorMode = EditorMode.Vote,
                    BoardId = BoardId
                };
                Frame.Navigate(typeof(SketchPage), param2);
                break;
        }
    }

    private async Task Pin()
    {
        var url = ApiEndpoints.Board.EditFocusBoards(BoardId);
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var result = await ApiService.Put(url, content);
        if (!result.IsSuccess)
        {
            //
            Flower.Play(FlowStatus.Fail, result.Message);
            //await App.Logger.WriteAsync("Board", "关注版面失败", result.Message);
            return;
        }

        var i = new NavigationItem
        {
            IconSymbol = BoardIconHelper.GetSymbol(BoardId, BoardData.Name),
            Name = BoardData.Name,
            IsEditable = true,
            Tag = BoardId.ToString()
        };
        Messenger.Instance.AddNavigationItem(i);
        Flower.Play(FlowStatus.Success, "已关注");
    }

    private async void TypeSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        // 防重入:加载期间忽略重复触发,避免"重复加载两次帖子"的问题
        if (_isLoading) return;
        var item = sender as SelectorBar;
        if (item?.SelectedItem?.Tag is not string tag) return;
        Increment.Clear();
        Topics.Clear();
        //切换时，清除已有列表，重置增量更新，修改当前筛选类型
        FilterType = tag switch
        {
            "latest" => BoardTopicFilterType.Latest,
            "top" => BoardTopicFilterType.Top,
            _ => BoardTopicFilterType.Best,
        };
        await RefreshAsync();
    }

    // 版面数据加载防重入标志
    private bool _isLoading;

    /// <summary>
    /// 版面信息与主题列表并行加载,并防止并发触发。
    /// </summary>
    private async Task RefreshAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        try
        {
            await Task.WhenAll(GetData(), LoadTopics());
        }
        finally
        {
            _isLoading = false;
        }
    }
}