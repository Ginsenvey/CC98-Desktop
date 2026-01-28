using CC98.Kernel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;



namespace CC98
{

    public sealed partial class NoticeMsg : Page
    {
        public ObservableCollection<Notice> notices = new();
        public string type = "system";
        public NoticeMsg()
        {
            InitializeComponent();
            NoticeRepeater.ItemsSource = notices;
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var p = e.Parameter as string;

            if (p != null)
            {
                type = p;
                GetNotice(type, "0");
            }
        }
        //At和系统通知只显示最新10条
        private async void GetNotice(string type,string start)
        {
            notices.Clear();
            string NoticeText = await RequestSender.SystemNotice(type,start);
            if (NoticeText.StartsWith("404:"))
            {
                return;
            }
            else
            {
                var NoticeList = Deserializer.ToArray(NoticeText);
                if (NoticeList != null)
                {
                    if (NoticeList.Count > 0)
                    {
                        
                        foreach (var notice in NoticeList)
                        {
                            var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(notice.ToString());
                            if (js != null)
                            {
                                string floor = "0";
                                string username = "匿名";
                                if (js["postBasicInfo"] != null)
                                {
                                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(js["postBasicInfo"].ToString());
                                    floor = ValidationHelper.GetKey(info, "floor");
                                    username=ValidationHelper.GetKey(info, "userName")=="0"?"匿名":ValidationHelper.GetKey(info, "userName") ;
                                }
                                string NoticeType = ValidationHelper.GetKey(js, "type");
                                if (NoticeType == "1")
                                {
                                    notices.Add(new Notice
                                    {
                                        Title = js["title"].ToString(),
                                        Time = js["time"].ToString(),
                                        TopicId = ValidationHelper.GetKey(js, "topicId"),
                                        Content = js["content"].ToString(),
                                        NoticeId = js["id"].ToString(),
                                        Floor = floor,
                                        NoticeType = NoticeType
                                    });
                                }
                                else
                                {
                                    notices.Add(new Notice
                                    {
                                        Title ="@ "+ username,
                                        Time = js["time"].ToString(),
                                        TopicId = ValidationHelper.GetKey(js, "topicId"),
                                        Content = $"在主题 CC{ValidationHelper.GetKey(js, "topicId")} 中回复了你。",
                                        NoticeId = js["id"].ToString(),
                                        Floor = floor,
                                        NoticeType = NoticeType
                                    });
                                }
                                //根据类型确定显示内容。
                                
                                
                            }
                        }
                    }
                }
            }
        }

        private void NoticeCard_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h != null)
            {
                var n=h?.DataContext as Notice;
                if (n != null)
                {
                    if (n.NoticeType == "2")
                    {
                        if (n.TopicId != "0")
                        {
                            if ((App.Current as App).m_window is MainWindow mainwindow)
                            {
                                mainwindow.RootFrame.Navigate(typeof(Topic), n.TopicId);
                            }               
                        }
                        
                    }
                }
            }
        }
    }
    public class Notice
    {
        public string Time { get; set; }
        public string Title { get; set; }
        public string NoticeId { get; set; }
        public string TopicId { get; set; }
        public string Content { get; set; }
        public string Floor { get; set; }
        public string NoticeType {  get; set; }

    }
}
