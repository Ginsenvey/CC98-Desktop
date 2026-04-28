using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CC98.Services.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Follow : Page
{
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    public ObservableCollection<Friend> Friends = [];
    public string Type = "follower";
    public Increment Increment= new();
    public GlobalService GlobalService = GlobalService.Instance;
    public Follow()
    {
        InitializeComponent();
    }

        
    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var param = "";
        if (GlobalService.ShouldReplaceNavigationArgs)
        {
            if (GlobalService.NavigationAnchor is string targetType)
            {
                param= targetType;
            }
            else
            {
                param = "follower";
            }
        }
        else
        {
            param = e.TryGetParameter<string>() ?? "";
        }
                

        if (!string.IsNullOrEmpty(param))
        {
            Type= param;
            if (param == "follower")
            {
                FriendType.Text = "粉丝";
            }
            else
            {
                FriendType.Text = "关注";
            }  
        }
        await LoadFriend();
    }
        

    private async Task<bool> LoadFriend()
    {
        //获取好友ID列表
        var friendListUrl = ApiEndpoints.User.FreiendList(Type, Increment.StartIndex);
        var friendIdsResult = await RequestSender.Fetch<List<int>>(friendListUrl);
        //处理第一层异常
        if (!friendIdsResult.IsSuccess || friendIdsResult.Data == null)
        {
            //
            Flower.Play(FlowStatus.Fail, "加载好友Id列表失败");
            await App.Logger.WriteAsync("Follow", "加载好友Id列表失败", friendIdsResult.Message);
            return false;
        }
        var ids = friendIdsResult.Data;
        Increment.HasMore = ids.Count == Increment.PageSize;
        if (!Increment.HasMore)
        {
            Flower.Play(FlowStatus.Info, "已全部加载");
        }
        var param = string.Join("&", ids.Select(id => $"id={id}"));
        var userInfoUrl = ApiEndpoints.User.UserInfoList(param);

        // 获取好友详情
        var friendsResult = await RequestSender.Fetch<List<Friend>>(userInfoUrl);
        if (!friendsResult.IsSuccess || friendsResult.Data == null)
        {
            //
            Flower.Play(FlowStatus.Fail, "加载好友信息失败");
            await App.Logger.WriteAsync("NoticeMsg", "加载好友信息失败", friendsResult.Message);
            return false;
        }
        Friends.AddRange(friendsResult.Data); 
        return true;
    }

    private void TileContent_Click(object sender, RoutedEventArgs e)
    {
        var h = sender as HyperlinkButton;
        var tag = h?.Tag;
        if (tag == null) return;
        var param = new ProfileNavigationInfo { IsMe=false,UserId=tag.ToInt()};
        Frame.Navigate(typeof(Profile), param);
    }
        

    private async void UnFollow_Click(object sender, RoutedEventArgs e)
    {
        var m = (MenuFlyoutItem)sender;

        if (m.Tag is string tag)
        {
            //string restext = await RequestSender.Follow("0", tag);
                    
        }
    }

    private void Chat_Click(object sender, RoutedEventArgs e)
    {
        var m= sender as MenuFlyoutItem;           
        var f = m?.DataContext as Friend;
        if (f == null) return;
        var c = new ChatInfo { UserId = f.Id, Name = f.Name, PortraitUrl = f.PortraitUrl };
        var param = new MessageNavigationInfo { HasTarget = true, ChatUserInfo = c };
        Frame.Navigate(typeof(Message), param);
    }

    private async void FriendRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        await Increment.LoadMore(args.Index, LoadFriend);
    }
}