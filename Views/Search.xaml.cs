using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using CC98.Services.Extensions;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Web;

using Windows.Storage;

using DevWinUI;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Search : Page
{
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    public ObservableCollection<TopicInfo> Topics = [];
    public Search()
    {
        this.InitializeComponent();
        SearchList.ItemsSource = Topics;
    }
    public string Key = "";
    public SearchType Type = SearchType.Topic;
    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // 获取传递的参数
        var args = e.TryGetParameter<SearchNavigationInfo>();

        if (args != null)
        {
            Type = args.SearchType;
            Key = args.Key;
            switch (Type)
            {
                case SearchType.Topic:
                    await SearchTopic(HttpUtility.UrlEncode(Key), CurrentIndex);
                    break;
                case SearchType.User:
                    SearchUser(Key);//弃用
                    break;
            }
        }



    }

    private void SearchUser(string key)
    {
        var url = ApiEndpoints.User.SearchUserByName(key);
        //替换实现

    }

    private async Task<bool> SearchTopic(string key, int start)
    {
        var searchUrl = ApiEndpoints.Topic.SearchTopic(key, start);
        var searchResult = await RequestSender.Fetch<List<TopicInfo>>(searchUrl);
        if (!searchResult.IsSuccess || searchResult.Data == null)
        {
            //
            return false;
        }
        var data = searchResult.Data;

        Topics.AddRange(data);
        if (data.Count > 0) return true;

        return false;

    }
    public int CurrentIndex = 0;
    private async void NaviBar_Click(object sender, RoutedEventArgs e)
    {
        var b = sender as Button;
        if (b != null)
        {
            var tag = b.Tag.ToString();
            if (tag == "Back")
            {
                if (CurrentIndex > 0)
                {
                    CurrentIndex -= 20;
                    if (!await SearchTopic(Key, CurrentIndex))
                    {
                        CurrentIndex += 20;
                    }
                }
                else
                {
                    CurrentIndex = 0;
                    Flower.Play("\uE946", "已到达最新页面");
                }
            }
            else if (tag == "Forward")
            {
                CurrentIndex += 20;
                if (!await SearchTopic(Key, CurrentIndex))
                {
                    CurrentIndex -= 20;
                }
            }

            PageIndex.Text = "第 " + (CurrentIndex / 20 + 1).ToString() + " 页";
            RootViewer.ScrollToVerticalOffset(0);
        }
    }
    private void SearchContent_Click(object sender, RoutedEventArgs e)
    {
        var h = (HyperlinkButton)sender;
        if (h.Tag is not string t) return;

        Frame.Navigate(typeof(Topic), t);

    }

    private void Person_Click(object sender, RoutedEventArgs e)
    {
        var h = (HyperlinkButton)sender;
        if (h.Tag is not string tag || tag == "0") return;

        var param = new Dictionary<string, string>()
        {
            {"Mode","Others" },
            {"UserId",tag }
        };
        Frame.Navigate(typeof(Profile), param);
    }
}