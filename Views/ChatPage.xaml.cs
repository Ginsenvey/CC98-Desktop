using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using CC98.Kernel;
using CC98.Objects;
using CC98.Services.Extensions;

using DevWinUI;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     聊天页面。
/// </summary>
public sealed partial class ChatPage : Page
{
    public Increment ChatHistoryIncrement = new();
    public ObservableCollection<ChatInfo> ChatInfoList = [];
    public ApiService ApiService = App.Current.GetService<ApiService>();
    public int CurrentUserId;

    public ObservableCollection<ChatMessage> Messages = [];
    public ChatInfo TargetUserInfo = new();
    public Increment UserIncrement = new();

    public ChatPage()
    {
        InitializeComponent();
        if (App.Current.AppMainWindow is MainWindow mainwindow) mainwindow.NavigationView.IsPaneOpen = false;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var args = e.TryGetParameter<ChatNavigationInfo>();
        await GetRecent();

        if (args != null && args.HasTarget) //由私信功能跳转
        {
            var info = args.ChatUserInfo;
            if (info != null) TargetUserInfo = info; //获取要私信的对象
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
        var list = ChatInfoList.Select(x => x.UserId);
        if (list.Contains(TargetUserInfo.UserId))
        {
            UserList.SelectedIndex = ChatInfoList.ToList().FindIndex(x => x.UserId == TargetUserInfo.UserId);
        }
        else
        {
            ChatInfoList.Insert(0, TargetUserInfo);
            UserList.SelectedIndex = 0;
        }
    }

    private async void ContactRepeater_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var i = UserList.SelectedIndex;
        if (i > -1)
        {
            ChatHistoryIncrement.Clear();
            CurrentUserId = ChatInfoList[i].UserId;
            await RefreshMessageList();
        }
    }

    private async Task<bool> GetRecent()
    {
        var chatInfoUrl = ApiEndpoints.User.RecentChatUserList(UserIncrement.StartIndex);
        var chatInfoResult = await ApiService.Fetch<List<ChatInfo>>(chatInfoUrl);
        if (!chatInfoResult.IsSuccess || chatInfoResult.Data == null)
            //
            return false;
        var data = chatInfoResult.Data;

        var param = string.Join("&", data.Select(x => $"id={x.UserId}").Distinct());
        var userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
        var userInfoResult = await ApiService.Fetch<List<BasicUserInfo>>(userInfoUrl);
        if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
            //报错
            return false;

        var userInfoList = userInfoResult.Data;
        foreach (var info in data)
        {
            var user = userInfoList.FirstOrDefault(x => x.Id == info.UserId);
            if (user != null)
            {
                info.Name = user.Name;
                info.PortraitUrl = user.PortraitUrl;
            }
        }

        UserIncrement.HasMore = data.Count == UserIncrement.PageSize;
        ChatInfoList.AddRange(data);
        return true;
    }

    private async Task<bool> GetMessageList()
    {
        var messageUrl = ApiEndpoints.User.ChatHistory(CurrentUserId, ChatHistoryIncrement.StartIndex);
        var messageResult = await ApiService.Fetch<List<ChatMessage>>(messageUrl);
        if (!messageResult.IsSuccess)
            //
            return false;
        if (messageResult.Data == null)
            //
            return false;
        var data = messageResult.Data;
        ChatHistoryIncrement.HasMore = data.Count == ChatHistoryIncrement.PageSize;

        foreach (var message in data)
        {
            message.IsMe = message.ReceiverId == CurrentUserId;
        }

        // 一次性构造完整列表(更早的消息在前)再整体填充:
        // 避免逐条 Insert(0) 导致的 O(n^2) 元素移动与多次布局,改为尾部 O(1) 追加
        var combined = new List<ChatMessage>(data.Count + Messages.Count);
        combined.AddRange(data);
        combined.AddRange(Messages);
        Messages.Clear();
        foreach (var message in combined) Messages.Add(message);

        return true;
    }


    private async Task RefreshMessageList()
    {
        Messages.Clear();
        ChatHistoryIncrement.Clear();
        await GetMessageList();
    }

    private async void More_Click(object sender, RoutedEventArgs e)
    {
        await ChatHistoryIncrement.LoadNextPage(GetMessageList);
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ReplyBody.Text)) return;
        SendButton.IsEnabled = false;
        try
        {
            var url = ApiEndpoints.User.SendPrivateMessage;
            var post = new PrivateMessage
            {
                ReceiverId = CurrentUserId,
                Content = ReplyBody.Text
            };
            var postText = SerializationHelper.TrySerialize(post);
            var requestBody = new StringContent(postText, Encoding.UTF8, "application/json");
            var res = await ApiService.Submit<object>(url, requestBody);
            if (res.IsSuccess)
                await RefreshMessageList();
            else
                Flower.Play(FlowStatus.Fail, "发送回复失败");
        }
        catch (Exception ex)
        {
            // 避免 async void 未捕获异常崩溃,并确保按钮恢复
            System.Diagnostics.Debug.WriteLine($"发送失败: {ex.Message}");
            Flower.Play(FlowStatus.Fail, "发送失败，请重试");
        }
        finally
        {
            SendButton.IsEnabled = true;
        }
    }


    private async void Ref_Click(object sender, RoutedEventArgs e)
    {
        await RefreshMessageList();
    }
}