
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;


namespace CC98;

public sealed partial class Board : Page
{
    public ObservableCollection<SimpleTopicInfo> Topics = [];
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    //是否精华帖
    public bool IsBest = false;
    public int BoardId = 0;
    public Increment Increment = new(20);

    public BoardData BoardData = new() { BoardMasters = [],Id=0,BigPaper="",Description="", Name = "版面", TodayCount = 9898, TopicCount = 9898 };
    public Board()
    {
        this.InitializeComponent();
        LoadSet();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var args = e.TryGetParameter<int>();
        BoardId = args;
        BoardSymbol.Symbol=BoardIcon.GetSymbol(BoardId,"");
        await GetData();
        await LoadTopics();
    }
    private void LoadSet()
    {
        var showBigPaper = ValidationHelper.GetValue(Set, "ShowBigPaper");
        if (showBigPaper== "0")
        {
            //赋予默认值：打开
            Set.Values["ShowBigPaper"] = "1";
        }
        if (showBigPaper == "2")
        {
            BannerBox.Visibility = Visibility.Collapsed;
        }
    }
    private async Task GetData()
    {
        var boardDataUrl = ApiEndpoints.Board.BoardInfo(BoardId);
        var boardDataResult = await RequestSender.Fetch<BoardData>(boardDataUrl);
        if (!boardDataResult.IsSuccess || boardDataResult.Data == null) 
        {
            Flower.Play("\uEA39", boardDataResult.Message);
            return;
        }
        var data= boardDataResult.Data;
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
        var topicUrl = ApiEndpoints.Board.TopicList(IsBest, BoardId, Increment.StartIndex);
        if (IsBest)
        {
            var result = await RequestSender.Fetch<BoardBest>(topicUrl);
            if (result.IsNotValid)
            {
                Flower.Play(FlowStatus.Fail, result.Message);
                return false;
            }
            var bests= result.Data?.Topics;
            Increment.HasMore= bests.Count == Increment.PageSize;
            Topics.AddRange(bests);
            return true;
        }
        else
        {
            var topicResult = await RequestSender.Fetch<List<SimpleTopicInfo>>(topicUrl);
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
    }


    private void TileContent_Click(object sender, RoutedEventArgs e)
    {
        var h = sender as HyperlinkButton;
        if (h?.DataContext is not SimpleTopicInfo t) return;
        var param = new TopicNavigationInfo { TopicId = t.Id };
        Frame.Navigate(typeof(Topic), param);
    }

    private async void Gooey_Click(object sender, RoutedEventArgs e)
    {
        var s = sender as GooeyButtonItem;
        if (s != null)
        {
            var tag = s.Tag;
            if (tag is string tag)
            {
                switch (tag)
                {
                    case "Send":
                        var param = new SketchNavigationInfo
                        {
                            EditorMode=EditorMode.DraftNewTopic,
                            BoardId=BoardId
                        };
                        Frame.Navigate(typeof(Sketch), param);
                        break;
                    case "Pin":
                        var url = ApiEndpoints.Board.EditFocusBoards(BoardId);
                        var content = new StringContent("", Encoding.UTF8, "application/json");
                        var result = await RequestSender.Put(url, content);
                        if (!result.IsSuccess)
                        {
                            //
                            return;
                        }
                        var i = new NavigationItem
                        {
                            IconSymbol = BoardIcon.GetSymbol(BoardId, BoardData.Name),
                            Name = BoardData.Name,
                            IsEditable = true,
                            Tag = BoardId.ToString()
                        };
                        Messenger.Instance.AddNavigationItem(i);
                        break;
                    case "Best":
                        //GooeyGroup.Visibility = Visibility.Collapsed;
                        //BackFromBest.Visibility = Visibility.Visible;
                        Increment.Clear();
                        Topics.Clear();
                        IsBest = true;
                            
                        await LoadTopics();
                        break;
                }

            }
        }

    }

    private async void BackFromBest_Click(object sender, RoutedEventArgs e)
    {
        //BackFromBest.Visibility = Visibility.Collapsed;
        //GooeyGroup.Visibility = Visibility.Visible;
        IsBest = false;
        Increment.Clear();
        Topics.Clear();
        await LoadTopics();
    }

        

    private async void TopicRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        await Increment.LoadMore(args.Index,LoadTopics);
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
                await GetData();
                await LoadTopics();
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
                Frame.Navigate(typeof(Sketch), param);
                break;
            case "vote":
                var param2 = new SketchNavigationInfo
                {
                    EditorMode = EditorMode.Vote,
                    BoardId = BoardId
                };
                Frame.Navigate(typeof(Sketch), param2);
                break;
        }
    }

    private async Task Pin()
    {
        var url = ApiEndpoints.Board.EditFocusBoards(BoardId);
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var result = await RequestSender.Put(url, content);
        if (!result.IsSuccess)
        {
            //
            Flower.Play(FlowStatus.Fail, result.Message);
            await App.Logger.WriteAsync("Board", "关注版面失败",result.Message);
            return;
        }
        var i = new NavigationItem
        {
            IconSymbol = BoardIcon.GetSymbol(BoardId, BoardData.Name),
            Name = BoardData.Name,
            IsEditable = true,
            Tag = BoardId.ToString()
        };
        Messenger.Instance.AddNavigationItem(i);
        Flower.Play(FlowStatus.Success, "已关注");
    }
}