using Windows.Storage;
using CC98.Objects;
using CC98.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

public sealed partial class Message : Page
{
    public GlobalService GlobalService = GlobalService.Instance;
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;

    public Message()
    {
        InitializeComponent();
    }

    public MessageNavigationInfo NavigationInfo { get; set; } = new();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (GlobalService.ShouldReplaceNavigationArgs)
        {
            if (GlobalService.NavigationAnchor is int targetIndex)
                NaviBar.SelectedItem = NaviBar.Items[targetIndex];
            else
                NaviBar.SelectedItem = NaviBar.Items[0];
            return;
        }

        var args = e.TryGetParameter<MessageNavigationInfo>();
        if (args == null) return;
        NavigationInfo = args;
        NaviBar.SelectedItem = NaviBar.Items[0];
    }

    private void NaviBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var bar = NaviBar.SelectedItem;
        var selected = NaviBar.Items.IndexOf(bar);
        GlobalService.Instance.NavigationAnchor = selected;
        if (bar?.Tag is not string tag) return;
        switch (tag)
        {
            case "Chat":
                MsgFrame.Navigate(typeof(Chat), NavigationInfo);
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