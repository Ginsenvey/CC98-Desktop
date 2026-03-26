using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
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
using System.Net;
using System.Net.Http;
using System.Text;
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
        public Increment increment = new();
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
            string url = ApiEndpoints.User.SignIn();
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var result = await RequestSender.Submit<string>(url, content);
            if (result.IsSuccess)
            {
                SignStatus.Text = "签到中";
                SignStatusIcon.IconVariant = IconVariant.Filled;
                return;
            }
            if (result.StatusCode == (int)HttpStatusCode.BadRequest)
            {
                var info = result.Data;
                if (info == null)
                {
                    SignStatus.Text = "签到失败";
                    SignStatusIcon.IconVariant = IconVariant.Regular;
                    return;
                }
                if (info == "has_signed_in_today")
                {
                    SignStatus.Text = "已签到";
                    SignStatusIcon.IconVariant = IconVariant.Filled;
                    return;
                }
            }
            SignStatus.Text = "签到失败";
            SignStatusIcon.IconVariant = IconVariant.Regular;
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
            MyProfile.ProfilePicture = await UrlEx.LoadWebImage(profile.PortraitUrl);
            InfoContent.DataContext = profile;
            SignBoard.DataContext = profile; 
        }
        private async Task<bool> LoadRecentTopic()
        {
            string RecentTopicUrl = ApiEndpoints.Topic.RecentTopic(isMe, userId, increment.startIndex);
            var RecentTopicResult = await RequestSender.Fetch<List<SimpleTopicInfo>>(RecentTopicUrl);
            if (!RecentTopicResult.IsSuccess||RecentTopicResult.Data==null)
            {
                Flower.Play(FlowStatus.Fail, RecentTopicResult.Message);
                return false;
            }
            var data=RecentTopicResult.Data;
            //移除末尾
            if(data.Count==11)data.RemoveAt(10);
            increment.hasMore = data.Count == increment.pageSize;
            recentTopics.AddRange(data);
            return true;
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


        

       
        private void FollowList_Click(object sender, RoutedEventArgs e)
        {
            if (!isMe) return;
            var h = sender as HyperlinkButton;
            if (h?.Tag is not string tag) return;
            GlobalService.Instance.NavigationAnchor = tag;
            Frame.Navigate(typeof(Follow), tag);
        }
        
        

        private void StartChat_Click(object sender, RoutedEventArgs e)
        {
            var c = new ChatInfo { UserId = profile.Id, Name = profile.Name, PortraitUrl = profile.PortraitUrl };
            var param=new MessageNavigationInfo { ChatUserInfo = c ,HasTarget=true};
            Frame.Navigate(typeof(Message), param);
        }

        private void Follow_Click(object sender, RoutedEventArgs e)
        {
            bool flag = profile.IsFollowing;
            string mode=  flag?"0":"1";
            
        }

        private async void RecentTopicRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            await increment.LoadMore(args.Index, LoadRecentTopic);
        }
    }


    

}
