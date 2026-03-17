using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Collections.ObjectModel;
using FluentIcons.Common;
using Windows.Storage;
using System.Threading.Tasks;
using CC98.Kernel;
using CC98.Objects;
using System.Text.Json;
using DevWinUI;
using CC98.Kernel.ApiScope;
using CC98.Services.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Favorite : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<SimpleTopicInfo> topics=new();
        public Favorites? selectedFavorites { get; set; }
        public ObservableCollection<Favorites> favoritesList = new()
        {
            
        };
        public int sortId = 0;
        public int groupId = 0;
        public Increment increment = new();
        public PostOrder currenOrder = PostOrder.Mark;
        public Favorite()
        {
            this.InitializeComponent();
        }
        
        
        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LoadFavorites();

            await GetFavoriteTopic();
        }
        private void LoadFavorites()
        {
            var f = ValidationHelper.GetValue(Set, "Favorites");
            if (f != "0")
            {
                //likecollection.MenuItems.Clear();
                var data = JsonSerialize.Deserialize<List<Favorites>>(f);
                if(data != null)
                {
                    favoritesList.Clear();
                    favoritesList.AddRange(data);
                }   
            }
        }
        private async Task<bool> GetFavoriteTopic()
        {
            string favoriteTopicUrl = ApiEndpoints.Topic.FavoriteTopicList(increment.startIndex,(int)currenOrder,groupId);
            var favoriteTopicResult = await RequestSender.Fetch<List<SimpleTopicInfo>>(favoriteTopicUrl);
            if (!favoriteTopicResult.IsSuccess || favoriteTopicResult.Data == null)
            {
                //
                return false;
            }          
            var data = favoriteTopicResult.Data;
            if(data.Count==11)data.RemoveAt(10);
            increment.hasMore = data.Count == increment.pageSize;
            topics.AddRange(data);
            return true;
        }
        

        private void Content_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;            
            var t = h?.DataContext as SimpleTopicInfo;
            if (t == null) return;
            var param = new TopicNavigationInfo { TopicId = t.Id };
            Frame.Navigate(typeof(Topic), param);
        }
        
        
        private async void ChangeSort_Click(object sender, RoutedEventArgs e)
        {
            currenOrder= (PostOrder)(((int)currenOrder + 1) % 3);
            topics.Clear();
            increment.Clear();

            await GetFavoriteTopic();
            string Sort_Method = string.Empty;
            if (currenOrder == PostOrder.Time)
            {
                Sort_Method = "发帖时间";
                SortIcon.Symbol = FluentIcons.Common.Symbol.History;
            }
            else if (currenOrder == PostOrder.LastReply)
            {
                Sort_Method = "最后回复";
                SortIcon.Symbol = FluentIcons.Common.Symbol.ArrowReply;
            }
            else
            {
                Sort_Method = "收藏顺序";
                SortIcon.Symbol = FluentIcons.Common.Symbol.StarAdd;
            }
            Flower.Play("\uE8CB", "切换为" + Sort_Method + "排序");
        }

        private async void FavoriteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedFavorites != null)
            {
                topics.Clear();
                increment.Clear();
                sortId = 0;
                groupId = selectedFavorites.Id;
                de.Text = selectedFavorites.Name;
                await GetFavoriteTopic();
            }
        }

        private async void Remove_Click(object sender, RoutedEventArgs e)
        {
            var m=sender as MenuFlyoutItem;
            if(m != null)
            {
                var t = m?.DataContext as SimpleTopicInfo;
                if(t != null)
                {
                    //bool res=await RequestSender.RemoveFavorite(t.Id);
                    bool res = true; //待实现
                    if (res)
                    {
                        topics.Clear();
                        increment.Clear();
                        sortId = 0;
                        await GetFavoriteTopic();
                        Flower.Play("\uE930", "已取消收藏");
                    }
                    else
                    {
                        Flower.Play("\uEA39", "取消收藏失败");
                    }
                }
            }
        }

        private async void FavoriteTopicRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            await increment.LoadMore(args.Index, GetFavoriteTopic);
        }
    }
}
