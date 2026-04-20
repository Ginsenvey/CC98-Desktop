
using CC98.Controls;
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using CC98.Services.Extensions;
using CC98.Share.Controls.Primitives;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using CC98.Services;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Discover : Page
    {
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        public ObservableCollection<TopicInfo> topics=[];
        public HashSet<int> topicIds = [];
        public ObservableCollection<SimpleTopicInfo> randomTopics = [];
        public Increment increment = new(20);
        public Discover()
        {
            this.InitializeComponent();
            SizeChanged += Discover_SizeChanged;
        }

        private void Discover_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var size = e.NewSize;
            bool isWidthEnough = size.Width > 800;
            RefRandomTiles.Visibility = isWidthEnough ? Visibility.Visible : Visibility.Collapsed;
            RandomTopicViewer.Visibility = isWidthEnough ? Visibility.Visible : Visibility.Collapsed;
            Grid.SetColumnSpan(NewTopicViewer, isWidthEnough ? 1 : 2);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await GetNewTopic();
            await GetRandomTile();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged-= Discover_SizeChanged;
        }
        
        private async Task<bool> GetNewTopic()
        {
            string newTopicUrl = ApiEndpoints.Topic.NewTopicList(increment.startIndex);
            var newTopicResult = await RequestSender.Fetch<List<TopicInfo>>(newTopicUrl);
            if (!newTopicResult.IsSuccess || newTopicResult.Data == null)
            {
                //忽略加载过快报错
                if (newTopicResult.StatusCode == (int)HttpStatusCode.Forbidden) return false;
                Flower.Play(FlowStatus.Fail,newTopicResult.Message);
                await App.Logger.WriteAsync("Discover", "加载新帖失败", newTopicResult.Message);
                return false;
            }
            var data= newTopicResult.Data;
            increment.hasMore = data.Count == increment.pageSize;
            var param = string.Join("&", data.Where(x => !x.IsAnonymous && x.UserId.HasValue).Select(x => $"id={x.UserId}").ToHashSet());
            string userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
            var userInfoResult = await RequestSender.Fetch<List<BasicUserInfo>>(userInfoUrl);
            if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
            {
                //报错
                Flower.Play(FlowStatus.Fail, "获取用户头像出错");
            }
            var userInfoList = userInfoResult.Data;
            foreach(var topic in data)
            {
                if (topic.IsAnonymous)
                {
                    topic.PortraitUrl = "ms-appx:///Assets/hide.gif";
                    //跳过
                    continue;
                }
                var user = userInfoList?.First(x => x.Id == topic.UserId);
                if (user != null)
                {
                    topic.PortraitUrl = user.PortraitUrl;
                }
            }
            data = [.. data.Where(x => !topicIds.Contains(x.Id))];
            topics.AddRange(data);   
            topicIds.AddRange(data.Select(x => x.Id));
            return true;
        }

        
        private async Task GetRandomTile()
        {
            string randomTopicUrl = ApiEndpoints.Topic.RandomTopicList();
            var randomTopicResult = await RequestSender.Fetch<List<SimpleTopicInfo>>(randomTopicUrl);
            if (!randomTopicResult.IsSuccess || randomTopicResult.Data == null)
            {
                return;
            }
            var data = randomTopicResult.Data;
            randomTopics.AddRange(data);
        }

        private async void RefRandomTiles_Click(object sender, RoutedEventArgs e)
        {
            randomTopics.Clear();
            await GetRandomTile();
        }

        

       

        

        private void RandomPost_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h != null)
            {
                var p=h?.DataContext as SimpleTopicInfo;
                if(p != null)
                {
                    var param = new TopicNavigationInfo { TopicId = p.Id };
                    Frame.Navigate(typeof(Topic),param);
                }
            }
        }

     

   

        private async void ItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            await increment.LoadMore(args.Index, GetNewTopic);
        }

       

        private void ContentCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var h = sender as Grid;
            var translate = h?.RenderTransform as TranslateTransform;
            UIEx.AnimateCard(translate!, 0, -5); // 向上方移动
        }

        private void ContentCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var h = sender as Grid;
            var translate = h?.RenderTransform as TranslateTransform;
            UIEx.AnimateCard(translate!, 0, 0); // 恢复原位
        }
        

        private void ContentCard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var h = sender as Grid;
            var tag = h?.Tag;
            if (tag == null) return;
            var param = new TopicNavigationInfo { TopicId = tag.ToInt() };
            Frame.Navigate(typeof(Topic), param);
        }

        private void ContentCard_Loaded(object sender, RoutedEventArgs e)
        {

        }
    } 
}
