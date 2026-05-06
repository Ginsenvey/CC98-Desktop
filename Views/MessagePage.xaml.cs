using Windows.Storage;
using CC98.Objects;
using CC98.Services;
using Microsoft.UI.Xaml.Controls;
using CC98.Services.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
/// 消息页面。
/// </summary>
public sealed partial class MessagePage
{
    public MessageNavigationInfo NavigationInfo { get; set; } = new();
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    public GlobalService GlobalService = GlobalService.Instance;
    public MessagePage()
    {
        InitializeComponent();
    }
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (GlobalService.ShouldReplaceNavigationArgs)
        {
            if (GlobalService.NavigationAnchor is int targetIndex)
            {
                NaviBar.SelectedItem = NaviBar.Items[targetIndex];
            }
            else
            {
                NaviBar.SelectedItem = NaviBar.Items[0];
            }
            return;
        }
        var args = e.TryGetParameter<MessageNavigationInfo>();
        if (args == null) return;
        NavigationInfo = args;
        NaviBar.SelectedItem = NaviBar.Items[0];
    }

    private void NaviBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var bar = NaviBar.SelectedItem as SelectorBarItem;
        var selected = NaviBar.Items.IndexOf(bar);
        GlobalService.Instance.NavigationAnchor = selected;
        if (bar?.Tag is not string tag) return;
        switch (tag)
        {
            case "Chat":
                MsgFrame.Navigate(typeof(ChatPage), NavigationInfo);
                MsgCount.Text = $"{GlobalService.MessageCount}条未读信息";
                break;
            case "System":
                MsgFrame.Navigate(typeof(NoticePage), NoticeType.System);
                MsgCount.Text = $"{GlobalService.SystemCount}条未读信息";
                break;
            case "Reply":
                MsgFrame.Navigate(typeof(NoticePage), NoticeType.Reply);
                MsgCount.Text = $"{GlobalService.ReplyCount}条未读信息";
                break;
            case "At":
                MsgFrame.Navigate(typeof(NoticePage), NoticeType.At);
                MsgCount.Text = $"{GlobalService.AtCount}条未读信息";
                break;
        }

    }


}