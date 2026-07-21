using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services.Extensions;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CC98.Views;

public sealed partial class NoticePage : Page
{
    public Increment Increment = new();
    public ObservableCollection<Notice> Notices = [];
    public NoticeType Type = NoticeType.System;
    public ApiService ApiService = App.Current.GetService<ApiService>();
    public NoticePage()
    {
        InitializeComponent();
    }

    public string GetTypeName(NoticeType type)
    {
        return type switch
        {
            NoticeType.System => "system",
            NoticeType.At => "at",
            NoticeType.Reply => "reply",
            _ => ""
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var args = e.TryGetParameter<NoticeType>();
        Type = args;
        await GetNotice();
    }

    //At和系统通知只显示最新10条
    private async Task<bool> GetNotice()
    {
        var url = ApiEndpoints.User.SystemNotice(GetTypeName(Type), Increment.StartIndex);
        var result = await ApiService.Fetch<List<Notice>>(url);
        if (!result.IsSuccess || result.Data == null)
        {
            //
            Flower.Play(FlowStatus.Fail, "加载通知失败");
            //await App.Logger.WriteAsync("NoticeMsg", "加载通知失败", result.Message);
            return false;
        }

        var data = result.Data;
        Increment.HasMore = data.Count == Increment.PageSize;
        if (!Increment.HasMore) Flower.Play(FlowStatus.Info, "没有更多通知了");
        if (Type == NoticeType.Reply || Type == NoticeType.At)
        {
            var topicIds = data.Where(n => n.TopicId.HasValue).Select(n => n.TopicId!.Value).Distinct().ToList();
            var topicInfos = await GetBasicTopicInfo(topicIds);
            foreach (var notice in data)
            {
                var info = topicInfos.FirstOrDefault(t => t.Id == notice.TopicId);
                if (info != null)
                {
                    var operation = Type == NoticeType.Reply ? "回复" : "@";
                    var content = $"在帖子《{info.Title}》的{notice.PostBasicInfo?.Floor}L{operation}了你。";
                    notice.Content = content;
                }
            }
        }

        Notices.AddRange(data);
        return true;
    }


    private async Task<List<BasicTopicInfo>> GetBasicTopicInfo(List<int> topicIds)
    {
        var param = string.Join("&", topicIds.Select(id => $"id={id}"));
        var url = ApiEndpoints.Topic.BasicTopicInfoList(param);
        var result = await ApiService.Fetch<List<BasicTopicInfo>>(url);
        if (!result.IsSuccess || result.Data == null)
        {
            //
            Flower.Play(FlowStatus.Fail, "获取帖子基本信息失败");
            //await App.Logger.WriteAsync("NoticeMsg", "获取帖子基本信息失败", result.Message);
            return [];
        }

        var data = result.Data;
        return data;
    }

    private async void NoticeRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        await Increment.LoadMore(args.Index, GetNotice);
    }

    private void NoticeCard_Click(object sender, RoutedEventArgs e)
    {
        var h = sender as HyperlinkButton;
        var n = h?.DataContext as Notice;
        if (n == null) return;
        if (n.TopicId is not int topicId || n.PostBasicInfo == null) return;
        if (n.PostBasicInfo.IsDeleted) Flower.Play(FlowStatus.Info, "该帖子已被删除");
        ;
        //这里存在一个问题，当应用首次启动时，该项返回false,从而不能跳转
        if (App.Current.AppMainWindow is MainWindow mainwindow)
        {
            var param = new TopicNavigationInfo
            {
                IsJumpingMode = true,
                TargetFloor = n.PostBasicInfo.Floor,
                TopicId = topicId
            };
            mainwindow.RootFrame.Navigate(typeof(TopicPage), param);
        }
    }
}