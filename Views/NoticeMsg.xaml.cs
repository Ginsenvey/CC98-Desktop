using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using CC98.Services.Extensions;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;



namespace CC98
{

    public sealed partial class NoticePage : Page
    {
        public ObservableCollection<Notice> notices = new();
        public NoticeType type = NoticeType.System;
        public int currentPage = 0;
        public int pageSize = 10;
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
            NoticeRepeater.ItemsSource = notices;
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.TryGetParameter<NoticeType>();
            type = args;
            GetNotice();
        }
        
        //At和系统通知只显示最新10条
        private async void GetNotice()
        {
            notices.Clear();
            var url = ApiEndpoints.User.SystemNotice(GetTypeName(type), currentPage * pageSize);
            var result = await RequestSender.Fetch<List<Notice>>(url);
            if (!result.IsSuccess || result.Data == null)
            {
                //
                ValidationHelper.Log("加载数据失败", result.Message);
                return;
            }
            var data= result.Data;
            notices.AddRange(data);      
        }

        private void NoticeCard_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var n=h?.DataContext as Notice;
            if (n == null) return;
            if (n.TopicId is not int topicId || n.PostBasicInfo == null) return;
            if ((App.Current as App).m_window is MainWindow mainwindow)
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
