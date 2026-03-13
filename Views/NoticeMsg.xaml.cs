using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using CC98.Services.Extensions;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;



namespace CC98
{

    public sealed partial class NoticePage : Page
    {
        public ObservableCollection<Notice> notices = new();
        public NoticeType type = NoticeType.System;
        public Increment increment = new(); 
        public string GetTypeName(NoticeType type) => type switch
        { 
            NoticeType.System=>"system",
            NoticeType.At=>"at",
            NoticeType.Reply=>"reply",
            _=>""
         };
        public NoticePage()
        {
            InitializeComponent();
        }
        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.TryGetParameter<NoticeType>();
            type = args;
            await GetNotice();
        }
        
        //At和系统通知只显示最新10条
        private async Task<bool> GetNotice()
        {
            var url = ApiEndpoints.User.SystemNotice(GetTypeName(type), increment.startIndex);
            var result = await RequestSender.Fetch<List<Notice>>(url);
            if (!result.IsSuccess || result.Data == null)
            {
                //
                Flower.Play(FlowStatus.Fail, "加载通知失败");
                await App.Logger.WriteAsync("NoticeMsg", "加载通知失败", result.Message);
                return false;
            }
            var data= result.Data;
            increment.hasMore = data.Count == increment.pageSize;
            if (!increment.hasMore)
            {
                Flower.Play(FlowStatus.Info, "没有更多通知了");
            }
            if (type == NoticeType.Reply ||type==NoticeType.At)
            {
                var topicIds = data.Where(n => n.TopicId.HasValue).Select(n => n.TopicId!.Value).Distinct().ToList();
                var topicInfos = await GetBasicTopicInfo(topicIds);
                foreach (var notice in data)
                {
                    var info= topicInfos.FirstOrDefault(t => t.Id == notice.TopicId);
                    if (info != null)
                    {
                        string operation = type == NoticeType.Reply ? "回复" : "@";
                        string content=$"在帖子《{info.Title}》的{notice.PostBasicInfo?.Floor}L{operation}了你。";
                        notice.Content = content;
                    }
                }
            }
            notices.AddRange(data);
            return true;
        }

        

        private async Task<List<BasicTopicInfo>> GetBasicTopicInfo(List<int> topicIds)
        {
            var param= string.Join("&", topicIds.Select(id => $"id={id}"));
            var url=ApiEndpoints.Topic.BasicTopicInfoList(param);
            var result = await RequestSender.Fetch<List<BasicTopicInfo>>(url);
            if (!result.IsSuccess || result.Data == null)
            {
                //
                Flower.Play(FlowStatus.Fail, "获取帖子基本信息失败");
                await App.Logger.WriteAsync("NoticeMsg", "获取帖子基本信息失败", result.Message);
                return [];
            }
            var data= result.Data;
            return data;
        }

        private async void NoticeRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            await increment.LoadMore(args.Index, GetNotice);
        }

        private void NoticeCard_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var n = h?.DataContext as Notice;
            if (n == null) return;
            if (n.TopicId is not int topicId || n.PostBasicInfo == null) return;
            if (n.PostBasicInfo.IsDeleted) 
            { 
                Flower.Play(FlowStatus.Info, "该帖子已被删除");
            };
            //这里存在一个问题，当应用首次启动时，该项返回false,从而不能跳转
            if (App.Current.m_window is MainWindow mainwindow)
            {
                var param = new TopicNavigationInfo
                {
                    IsJumpingMode = true,
                    TargetFloor = n.PostBasicInfo.Floor,
                    TopicId = topicId
                };
                mainwindow.RootFrame.Navigate(typeof(Topic), param);
            }
        }
    }
    
}
