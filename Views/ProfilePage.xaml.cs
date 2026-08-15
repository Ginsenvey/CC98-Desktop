using CC98.Controls.Primitives;
using CC98.Controls.UbbTextBlock;
using CC98.Controls.UbbTextBlock.Common.Events;
using CC98.Controls.UbbTextBlock.Parser;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CC98.Services.Helpers;
using DevWinUI;
using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
/// 用户资料页面。
/// </summary>
public sealed partial class ProfilePage : Page
{
    public ObservableCollection<SimpleTopicInfo> RecentTopics = [];
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    public ApiService ApiService=App.Current.GetService<ApiService>();
    public UserInfo UserProfile { get; } = new()
    {
        Id = 0,
        Name = "未知用户",
        PortraitUrl = "",
        IsFollowing = false,
    };
    public bool IsMe = false;
    public int UserId = 0;
    public Increment Increment = new();
    public ProfilePage()
    {
        InitializeComponent();
    }
    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // 获取传递的参数
        var args = e.TryGetParameter<ProfileNavigationInfo>();
        if (args != null)
        {
            UserId = args.UserId;
            IsMe = args.IsMe;
            await LoadUserProfile();
            await LoadRecentTopic();
            if (IsMe) SignIn();
        }
    }
    private async void SignIn()
    {
        var url = ApiEndpoints.User.SignIn();
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var result = await ApiService.Submit<string>(url, content);
        if (result.IsSuccess)
        {
            SignStatus.Text = "签到中";
            SignStatusIcon.IconVariant = IconVariant.Filled;
            return;
        }
        if (result.StatusCode == (int)HttpStatusCode.BadRequest)
        {
            var info = result.Message;
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
    private async Task LoadUserProfile()
    {
        var UserProfileUrl = ApiEndpoints.User.UserProfile(IsMe, UserId);
        var UserProfileResult = await ApiService.Fetch<UserInfo>(UserProfileUrl);
        if (!UserProfileResult.IsSuccess || UserProfileResult.Data == null)
        {
            return;
        }
        var data = UserProfileResult.Data;
        UserProfile.Name = data.Name;
        UserProfile.Id = data.Id;
        UserProfile.Popularity = data.Popularity;
        UserProfile.FanCount = data.FanCount;
        UserProfile.FollowCount = data.FollowCount;
        UserProfile.LastLogOnTime = data.LastLogOnTime;
        UserProfile.PortraitUrl = data.PortraitUrl;
        UserProfile.SignatureCode = data.SignatureCode;
        UserProfile.PostCount = data.PostCount;
        UserProfile.Wealth = data.Wealth;
        UserProfile.RegisterTime = data.RegisterTime;
        UserProfile.IsFollowing = data.IsFollowing;
        if (IsMe && AppSettings.Current.UserId == 0)
        {
            AppSettings.Current.UserId = data.Id;
            AppSettings.Current.Portrait = data.PortraitUrl;
        }
        UserProfile.IsOthers = !IsMe;
        try
        {
            var source = await Services.Helpers.ImageHelper.LoadWebImageAsync(UserProfile.PortraitUrl);
            MyProfile.ProfilePicture = source;
        }
        catch (Exception ex)
        {
            //await App.Logger.WriteAsync("UserProfile", "加载头像失败", ex.Message);
        }
        InfoContent.DataContext = UserProfile;
        SignBoard.DataContext = UserProfile;
    }
    private async Task<bool> LoadRecentTopic()
    {
        var recentTopicUrl = ApiEndpoints.Topic.RecentTopic(IsMe, UserId, Increment.StartIndex);
        var recentTopicResult = await ApiService.Fetch<List<SimpleTopicInfo>>(recentTopicUrl);
        if (!recentTopicResult.IsSuccess || recentTopicResult.Data == null)
        {
            Flower.Play(FlowStatus.Fail, recentTopicResult.Message);
            return false;
        }
        var data = recentTopicResult.Data;
        //移除末尾
        if (data.Count == 11) data.RemoveAt(10);
        Increment.HasMore = data.Count == Increment.PageSize;
        RecentTopics.AddRange(data);
        return true;
    }

    private void STileButton_Click(object sender, RoutedEventArgs e)
    {
        var h = sender as HyperlinkButton;
        if (h?.DataContext is SimpleTopicInfo s)
        {
            var param = new TopicNavigationInfo { TopicId = s.Id };
            Frame.Navigate(typeof(TopicPage), param);
        }
    }





    private void FollowList_Click(object sender, RoutedEventArgs e)
    {
        if (!IsMe) return;
        var h = sender as HyperlinkButton;
        if (h?.Tag is not string tag) return;
        GlobalService.Instance.NavigationAnchor = tag;
        Frame.Navigate(typeof(FollowPage), tag);
    }



    private void StartChat_Click(object sender, RoutedEventArgs e)
    {
        var c = new ChatInfo { UserId = UserProfile.Id, Name = UserProfile.Name, PortraitUrl = UserProfile.PortraitUrl };
        var param = new ChatNavigationInfo { ChatUserInfo = c, HasTarget = true };
        Frame.Navigate(typeof(MessagePage), param);
    }

    private void Follow_Click(object sender, RoutedEventArgs e)
    {
        var flag = UserProfile.IsFollowing;
        var mode = flag ? "0" : "1";

    }

    private async void RecentTopicRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        await Increment.LoadMore(args.Index, LoadRecentTopic);
    }

    private async void UbbTextBlock_MediaClicked(object sender, MediaClickEventArgs e)
    {
        switch (e.MediaType)
        {
            case MediaType.Link:
                //await HandleLink(e.Source);
                break;
            case MediaType.AtUser:
                await SearchForUser(e.Source);
                break;
            case MediaType.File or MediaType.Audio:
                var fileRes = await Downloader.DownloadFileAsync(e.Source);
                if (fileRes == null)
                {
                    Flower.Play(FlowStatus.Fail, "下载失败");
                }
                else
                {
                    Flower.Play(FlowStatus.Success, $"已下载到{fileRes}");
                }
                break;
        }
    }

    private async Task SearchForUser(string userName)
    {
        var url = ApiEndpoints.User.SearchUserByName(userName);
        var result = await ApiService.Fetch<UserInfo>(url);
        if (!result.IsSuccess || result.Data == null)
        {
            //
            return;
        }
        var user = result.Data;
        if (user == null)
        {
            Flower.Play(FlowStatus.Fail, "未找到用户");
        }
        else
        {
            var info = new ProfileNavigationInfo { IsMe = userName == AppSettings.Current.UserName, UserId = user.Id };
            Frame.Navigate(typeof(ProfilePage), info);
        }

    }
}