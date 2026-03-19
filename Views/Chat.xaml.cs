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
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        public bool hasTarget = false;
        public ChatInfo targetUserInfo = new();
        public int currentUserId = 0;
        public Increment userIncrement = new();
        public Increment chatHistoryIncrement = new();
        public Chat()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.TryGetParameter<MessageNavigationInfo>();
            if (args == null) return;
            hasTarget = args.HasTarget;
            if (hasTarget)//由私信功能跳转
            {
                var info = args.ChatUserInfo;
                if (info != null)
                {
                    targetUserInfo = info;//获取要私信的对象
                }
            }
            await GetRecent();
            if (hasTarget)
            {
                StartChat();
            }
            else
            {
                UserList.SelectedIndex = 0;
            }

        }
        //用于添加目标用户到聊天列表，并执行选中
        private void StartChat()
        {
            var list = chatInfoList.Select(x => x.UserId);
            if (list.Contains(targetUserInfo.UserId))
            {
                UserList.SelectedIndex = chatInfoList.ToList().FindIndex(x => x.UserId == targetUserInfo.UserId);
            }
            else
            {
                chatInfoList.Insert(0, targetUserInfo);
                UserList.SelectedIndex = 0;
            }
        }
        private async void ContactRepeater_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = UserList.SelectedIndex;
            if (i > -1)
            {
                chatHistoryIncrement.Clear();
                currentUserId = chatInfoList[i].UserId;
                await RefreshMessageList();
            }

        }
        private async Task<bool> GetRecent()
        {
            string chatInfoUrl = ApiEndpoints.User.RecentChatUserList(userIncrement.startIndex);
            var chatInfoResult = await RequestSender.Fetch<List<ChatInfo>>(chatInfoUrl);
            if (!chatInfoResult.IsSuccess || chatInfoResult.Data == null)
            {
                //
                return false;
            }
            var data = chatInfoResult.Data;

            var param = string.Join("&", data.Select(x => $"id={x.UserId}").ToHashSet());
            string userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
            var userInfoResult = await RequestSender.Fetch<List<BasicUserInfo>>(userInfoUrl);
            if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
            {
                //报错
                return false;
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
            userIncrement.hasMore = data.Count == chatHistoryIncrement.pageSize;
            chatInfoList.AddRange(data);
            return true;
        }
        
        private async Task<bool> GetMessageList()
        {
            string messageUrl = ApiEndpoints.User.ChatHistory(currentUserId, chatHistoryIncrement.startIndex);
            var messageResult = await RequestSender.Fetch<List<ChatMessage>>(messageUrl);
            if (!messageResult.IsSuccess)
            {
                //
                return false;
            }
            if(messageResult.Data == null)
            {
                //
                return false;
            }
            var data = messageResult.Data;
            chatHistoryIncrement.hasMore= data.Count == chatHistoryIncrement.pageSize;
            
            foreach(var message in data)
            {
                message.IsMe = message.ReceiverId == currentUserId;
                messages.Insert(0, message);
            }
            return true;
        }
        
        
        private async Task RefreshMessageList()
        {
            messages.Clear();
            chatHistoryIncrement.Clear();
            await GetMessageList();
            // 滚动到最底部
            ScrollTo(messages.Count - 1);
        }
        private void ScrollTo(int index)
        {
            try
            {
                var element = MessagesList.GetOrCreateElement(index);
                var options = new BringIntoViewOptions
                {
                    VerticalAlignmentRatio = 1, // 0=顶部对齐，0.5=居中，1=底部
                    AnimationDesired = true       // 启用平滑滚动动画
                };
                element.StartBringIntoView(options);
            }
            catch { }
        }
        private async void More_Click(object sender, RoutedEventArgs e)
        {
            await chatHistoryIncrement.LoadNextPage(GetMessageList);
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
                    await RefreshMessageList();
                }
            }
        }

        private async void Ref_Click(object sender, RoutedEventArgs e)
        {
            await RefreshMessageList();
        }

    }

    
    
    

    

    
}
