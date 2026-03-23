
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CommunityToolkit.Labs.WinUI;
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Focus : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<TopicInfo> topics=[];
        public FocusContentType mode = FocusContentType.Followee;
        public Increment increment = new(20,0,true);
        public HashSet<int> topicIds = [];
        public GlobalService GlobalService=GlobalService.Instance;
        public Focus()
        {
            this.InitializeComponent();
            
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {

            base.OnNavigatedTo(e);
            if (GlobalService.ShouldReplaceNavigationArgs)
            {
                if (GlobalService.NavigationAnchor is int targetIndex)
                {
                    Navibar.SelectedItem =Navibar.Items[targetIndex];
                }
                else
                {
                    Navibar.SelectedItem = Navibar.Items[0];
                }
                return;
            }
            await GetMoments();
        }


        private async Task<bool> GetMoments()
        {
            string url = (mode == FocusContentType.Followee) ? 
                ApiEndpoints.User.Moment(increment.startIndex) : 
                ApiEndpoints.User.FavoriteTopicUpdate(increment.startIndex);
            var result = await RequestSender.Fetch<List<TopicInfo>>(url);
            if (!result.IsSuccess || result.Data == null)
            {
                //
                Flower.Play(FlowStatus.Fail, "加载动态失败");
                await App.Logger.WriteAsync("Focus", "加载动态失败", result.Message);
                return false;
            }
            var data=result.Data;
            increment.hasMore = data.Count==increment.pageSize;

            var param = string.Join("&", data.Where(x => !x.IsAnonymous && x.UserId.HasValue).Select(x => $"id={x.UserId}").ToHashSet());
            string userInfoUrl = ApiEndpoints.User.BasicUserInfoList(param);
            var userInfoResult = await RequestSender.Fetch<List<BasicUserInfo>>(userInfoUrl);
            if (!userInfoResult.IsSuccess || userInfoResult.Data == null)
            {
                //报错
                Flower.Play(FlowStatus.Fail, "获取用户头像出错");
            }
            var userInfoList = userInfoResult.Data;
            foreach (var topic in data)
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

        
        

        private async void TypeChoice_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var s=Navibar.SelectedItem as SelectorBarItem;
            if (s?.Tag is not string tag) return;
            mode = tag == "0" ? FocusContentType.Followee : FocusContentType.FavoriteUpdate;
            GlobalService.NavigationAnchor = Navibar.Items.IndexOf(s);
            increment.Clear();
            topics.Clear();
            topicIds.Clear();
            await GetMoments();
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h?.DataContext is not TopicInfo s) return;
            var param = new TopicNavigationInfo { TopicId = s.Id };
            Frame.Navigate(typeof(Topic), param);
        }

        private async void MomentRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            await increment.LoadMore(args.Index, GetMoments);
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
    }

    
}
