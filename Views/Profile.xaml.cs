using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services.Extensions;
using CommunityToolkit.WinUI.UI.Controls;
using DevWinUI;
using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Profile : Page
    {
        public ObservableCollection<SimpleTopicInfo> recentTopics=new ObservableCollection<SimpleTopicInfo>();
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public UserInfo profile=new UserInfo() { 
            Id=0,
            Name="未知用户",
            PortraitUrl="",
            IsFollowing=false,
        };
        public bool isMe=false;
        public int userId = 0;
        public int currentIndex = 0;
        public int history = 0;
        public bool hasMore = true;
        public Profile()
        {
            this.InitializeComponent();
        }
        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var args = e.TryGetParameter<ProfileNavigationInfo>();
            if (args != null)
            {
                userId = args.UserId;
                isMe = args.IsMe;
                await LoadProfile();
                await LoadRecentTopic();
                if(isMe)SignIn();
            } 
        }
        private async void SignIn()
        {
            string SignInResult = await RequestSender.SignIn();
            if(SignInResult=="0")
            {
                SignStatus.Text = "签到失败";
                SignStatusIcon.IconVariant = IconVariant.Regular;
            }
            else if (SignInResult == "1")
            {
                SignStatus.Text = "签到中";
                SignStatusIcon.IconVariant = IconVariant.Filled;
            }
            else
            {
                SignStatus.Text = "已签到";
                SignStatusIcon.IconVariant = IconVariant.Filled;
            }
        }
        //用户个人页面检查跳转参数
        private async Task LoadProfile()
        {
            string profileUrl = ApiEndpoints.User.UserProfile(isMe,userId);
            var profileResult = await RequestSender.Fetch<UserInfo>(profileUrl);
            if (!profileResult.IsSuccess || profileResult.Data == null)
            {
                return;
            }
            var data= profileResult.Data;
            profile.Name = data.Name;
            profile.Id = data.Id;
            profile.Popularity = data.Popularity;
            profile.FanCount = data.FanCount;
            profile.FollowCount = data.FollowCount;
            profile.LastLogOnTime = data.LastLogOnTime;
            profile.PortraitUrl = data.PortraitUrl;
            profile.SignatureCode = data.SignatureCode;
            profile.PostCount = data.PostCount;
            profile.Wealth = data.Wealth;
            profile.RegisterTime = data.RegisterTime;
            profile.IsFollowing = data.IsFollowing;
            if (isMe&& ValidationHelper.GetValue(Set, "Uid")=="0")
            {
                Set.Values["Uid"] = data.Id.ToString();
                Set.Values["Portrait"] = data.PortraitUrl;
            }
            profile.IsOthers = !isMe;
            MyProfile.ProfilePicture = await ImageResolver.LoadWebImage(profile.PortraitUrl);
            InfoContent.DataContext = profile;
            SignBoard.DataContext = profile; 
        }
        private async Task LoadRecentTopic()
        {
            string RecentTopicUrl = ApiEndpoints.Topic.RecentTopic(isMe, userId, currentIndex);
            var RecentTopicResult = await RequestSender.Fetch<List<SimpleTopicInfo>>(RecentTopicUrl);
            if (!RecentTopicResult.IsSuccess||RecentTopicResult.Data==null)
            {
                Flower.Play("\uE739", RecentTopicResult.Message);
                return;
            }
            var data=RecentTopicResult.Data;
            hasMore = data.Count == 11;
            //移除末尾
            if(hasMore)data.RemoveAt(10);
            recentTopics.AddRange(data);
        }
        private async void Drawer_ImageResolving(object sender, ImageResolvingEventArgs e)
        {
            var defr = e.GetDeferral();
            var Source = e.Url;
            if (Source == null) return;

            try
            {
                switch (Source)
                {
                    case string url when ImageResolver.IsWebUrl(url):
                        e.Image = await ImageResolver.LoadWebImage(url);
                        break;

                    case string path when ImageResolver.IsLocalPath(path):
                        e.Image = await ImageResolver.LoadLocalImage(path);
                        break;
                }
            }
            catch
            {
                e.Image = null;
            }
            e.Handled = true;
            defr.Complete();

        }
        private void STileButton_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var s = h?.DataContext as SimpleTopicInfo;
            if (s != null)
            {
                var param = new TopicNavigationInfo { TopicId = s.Id };
                Frame.Navigate(typeof(Topic), param);    
            }
        }


        private void SimpleTile_Loaded(object sender, RoutedEventArgs e)
        {
            SimpleTile.ElementPrepared += async (s, e) =>
            {
                if (SimpleTile.ItemsSource != null)
                {
                    int current = e.Index;
                    if (current > history && (current + 1) % 10 == 0&&hasMore)
                    {
                        currentIndex = current;
                        await LoadRecentTopic();
                        //如果没有实际加载到数据，history不增加，下次滚动时继续触发加载
                        //如果history仍增加，则界面卡死
                        if (recentTopics.Count > currentIndex + 1)
                        {
                            history = currentIndex;
                        }
                    }
                }
            };
        }

        private async void SignBoard_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            string link = e.Link;
            var result = LinkAnalyzer.Parse(link);
            switch (result.Key)
            {
                case "topic":
                    Frame.Navigate(typeof(Topic), result.Value);
                    break;
                case "user":
                    {
                        string _url = "https://api.cc98.org/user/name/" + result.Value;
                        

                        break;
                    }
                //using语句不能在switch语句中直接出现。因此，使用大括号包围这个case.
                case "board":
                    Frame.Navigate(typeof(Board), result.Value);
                    break;
                case "file":
                    if (result.Value == "image")
                    {
                        try
                        {
                            var param = new Dictionary<string, string>()
                            {
                                {"url",link },
                                {"type","image" }
                             };
                            var picviewer = new MediaViewer(param);
                            picviewer.Activate();
                        }
                        catch
                        {

                        }
                    }   
                    break;
                case "backlink":
                    if (result.Value == "bili")
                    {
                        Flower.Play("\uE930", "已复制Bili外链");
                    }
                    break;
                default://自动复制到用户剪切板
                    var datapackage = new DataPackage();
                    datapackage.SetText(result.Value);
                    Clipboard.SetContent(datapackage);
                    Flower.Play("\uE930", "已复制外部链接");
                    break;
            }
        }

        private void FollowList_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;

            if (h != null)
            {
                var i = h.DataContext as UserInfo;
                if (i != null)
                {
                    if (!i.IsOthers)
                    {
                        string tag = h.Tag as string;
                        if (!string.IsNullOrEmpty(tag))
                        {
                            Frame.Navigate(typeof(Follow), tag);
                        }
                    }
                }


            }
        }
        
        

        private void StartChat_Click(object sender, RoutedEventArgs e)
        {
            var c = new ChatInfo { UserId = profile.Id, Name = profile.Name, PortraitUrl = profile.PortraitUrl };
            var param=new MessageNavigationInfo { ChatUserInfo = c ,IsFromProfile=true};
            Frame.Navigate(typeof(Message), param);
        }

        private void Follow_Click(object sender, RoutedEventArgs e)
        {
            bool flag = profile.IsFollowing;
            string mode=  flag?"0":"1";
            
        }
    }
    
    
    public partial class BooltoVisibilityConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool flag)
            {
                return flag ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public partial class BooltoVariantConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool flag)
            {
                return flag ? IconVariant.Filled : IconVariant.Regular;
            }
            else
            {
                return IconVariant.Regular;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public partial class RBooltoVisibilityConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool flag)
            {
                return flag ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public partial class BoolToFollowTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isFollowing)
            {
                return isFollowing ? "取消关注" : "关注";
            }
            return "关注";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
