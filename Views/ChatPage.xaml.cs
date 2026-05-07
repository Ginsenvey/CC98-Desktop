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

    public int CurrentUserId;

    //是否来自Profile页面的私信跳转功能
    public bool HasTarget;
    public ObservableCollection<ChatMessage> Messages = [];
    public ChatInfo TargetUserInfo = new();
    public Increment UserIncrement = new();

    public ChatPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var args = e.TryGetParameter<MessageNavigationInfo>();
        if (args == null) return;
        HasTarget = args.HasTarget;
        if (HasTarget) //由私信功能跳转
        {
            var info = args.ChatUserInfo;
            if (info != null) TargetUserInfo = info; //获取要私信的对象
        }

        await GetRecent();
        if (HasTarget)
            StartChat();
        else
            UserList.SelectedIndex = 0;
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
        var chatInfoResult = await RequestSender.Fetch<List<ChatInfo>>(chatInfoUrl);
        if (!chatInfoResult.IsSuccess || chatInfoResult.Data == null)
            //
            return false;
        var data = chatInfoResult.Data;

        var param = string.Join("&", data.Select(x => $"id={x.UserId}").ToHashSet());
        var userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
        var userInfoResult = await RequestSender.Fetch<List<BasicUserInfo>>(userInfoUrl);
        if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
            //报错
            return false;

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

        UserIncrement.HasMore = data.Count == ChatHistoryIncrement.PageSize;
        ChatInfoList.AddRange(data);
        return true;
    }

    private async Task<bool> GetMessageList()
    {
        var messageUrl = ApiEndpoints.User.ChatHistory(CurrentUserId, ChatHistoryIncrement.StartIndex);
        var messageResult = await RequestSender.Fetch<List<ChatMessage>>(messageUrl);
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
            Messages.Insert(0, message);
        }

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
        Send.IsEnabled = false;
        var url = ApiEndpoints.User.SendPrivateMessage;
        var post = new PrivateMessage
        {
            ReceiverId = CurrentUserId,
            Content = ReplyBody.Text
        };
        var postText = SerializationHelper.TrySerialize(post);
        var requestBody = new StringContent(postText, Encoding.UTF8, "application/json");
        var res = await RequestSender.Submit<object>(url, requestBody);
        if (res.IsSuccess)
            await RefreshMessageList();
        else
            Flower.Play(FlowStatus.Fail, "发送回复失败");
        Send.IsEnabled = true;
    }


    private async void Ref_Click(object sender, RoutedEventArgs e)
    {
        await RefreshMessageList();
    }
}