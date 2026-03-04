using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98

{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Chat : Page
    {
        public ObservableCollection<ChatInfo> chatInfoList = new();
        public ObservableCollection<ChatMessage> messages = new();
        //是否来自Profile页面的私信跳转功能
        public bool isFromProfile = false;
        public ChatInfo targetUserInfo = new();
        public int currentUserId = 0;
        public int currentIndex = 0;
        public int history = 0;
        public bool hasMore = true;
        public Chat()
        {
            this.InitializeComponent();
            ContactRepeater.ItemsSource = chatInfoList;
            MessagesList.ItemsSource = messages;
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.TryGetParameter<MessageNavigationInfo>();
            if (args != null)
            {
                isFromProfile = args.IsFromProfile;
                if (isFromProfile)//由私信功能跳转
                {
                    var info = args.ChatUserInfo;
                    if (info != null)
                    {
                        targetUserInfo = info;//获取要私信的对象
                    }
                }
                GetRecent();
            }

        }
        private void ContactRepeater_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = ContactRepeater.SelectedIndex;
            if (i > -1)
            {
                history = 0;
                currentUserId = chatInfoList[i].UserId;
                RefreshMessageList();
            }

        }
        private async void GetRecent()
        {
            string chatInfoUrl = ApiEndpoints.User.RecentChatUserList();
            var chatInfoResult = await RequestSender.Fetch<List<ChatInfo>>(chatInfoUrl);
            if (!chatInfoResult.IsSuccess || chatInfoResult.Data == null)
            {
                //
                return;
            }
            var data = chatInfoResult.Data;

            var param = string.Join("&", data.Select(x => $"id={x.UserId}").ToHashSet());
            string userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
            var userInfoResult = await RequestSender.Fetch<List<BasicUserInfo>>(userInfoUrl);
            if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
            {
                //报错
                return;
            }

            var userInfoList = userInfoResult.Data;
            foreach (var info in data)
            {
                var user = userInfoList.First(x => x.Id == info.UserId);
                if (user != null)
                {
                    info.Name = user.Name;
                    info.PortraitUrl = user.PortraitUrl;
                }
            }
            chatInfoList.AddRange(data);
            if (chatInfoList.Count == 0)
            {
                //
                return;
            }
            //选中要私信的用户
            if (isFromProfile)
            {
                ContactRepeater.SelectedItem = chatInfoList.First(c => c.UserId == targetUserInfo.UserId);
            }
            else
            {
                ContactRepeater.SelectedIndex = 0;
            }
        }
        
        private async Task GetMessageList()
        {
            string messageUrl = ApiEndpoints.User.ChatHistory(currentUserId, currentIndex);
            var messageResult = await RequestSender.Fetch<List<ChatMessage>>(messageUrl);
            if (!messageResult.IsSuccess)
            {
                //
                return;
            }
            if(messageResult.Data == null)
            {
                //
                return;
            }
            var data = messageResult.Data;
            hasMore = data.Count == 10;
            if (hasMore)
            {
                history += 10;
            }
            foreach(var message in data)
            {
                message.IsMe = message.ReceiverId == currentUserId;
                messages.Insert(0, message);
            }
        }
        
        private async void MoreMsg_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            if (currentUserId != 0 && hasMore)
            {
                currentIndex = history;
                await GetMessageList();
            }
            
        }
        private async void RefreshMessageList()
        {
            messages.Clear();
            currentIndex = 0;
            await GetMessageList();
            if (MessagesList.Items.Count > 0)
            {
                // 获取最后一个项目并滚动到它
                var lastItem = MessagesList.Items[MessagesList.Items.Count - 1];
                MessagesList.ScrollIntoView(lastItem);
            }
        }
        
        private async void More_Click(object sender, RoutedEventArgs e)
        {
            if (currentUserId != 0 && hasMore)
            {
                currentIndex = history;
                await GetMessageList();
            }
        }
        

        
        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ReplyBody.Text))
            {
                Send.IsEnabled = false;
                var r = await RequestSender.SendPrivateMsg(currentUserId, ReplyBody.Text);
                Send.IsEnabled = true;
                if (r == "1")
                {
                    ReplyBody.Text = "";
                    RefreshMessageList();
                }
                else
                {
                    status.Title = "发送失败";
                    status.Content = "这可能是网络不佳导致的，或者存在代码问题。";
                    status.IsOpen = true;
                }
            }
        }

        private void Ref_Click(object sender, RoutedEventArgs e)
        {
            RefreshMessageList();
        }

        
    }

    
    
    

    

    
}
