
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
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
        public Focus()
        {
            this.InitializeComponent();
            
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
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
                Flower.Play(FlowStatus.Fail, "º”‘ÿ∂ØÃ¨ ß∞‹");
                App.Logger.Write("Focus","º”‘ÿ∂ØÃ¨ ß∞‹", result.Message);
                return false;
            }
            var data=result.Data;
            increment.hasMore = data.Count==increment.pageSize;
            topics.AddRange(data);
            return true;
        }

        
        

        private async void TypeChoice_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var s=TypeChoice.SelectedItem as SelectorBarItem;
            if(s != null)
            {
                var tag = s.Tag;
                if(tag is string _tag)
                {
                    mode = _tag=="0"?FocusContentType.Followee:FocusContentType.FavoriteUpdate;
                    increment.Clear();
                    topics.Clear();
                    await GetMoments();
                }
            }
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var s = h?.DataContext as TopicInfo;
            if (s == null) return;
            var param = new TopicNavigationInfo { TopicId = s.Id };
            Frame.Navigate(typeof(Topic), param);
        }

        private async void MomentRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            await increment.LoadMore(args.Index, GetMoments);
        }
    }

    
}
