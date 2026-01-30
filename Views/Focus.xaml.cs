
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
        public int currentIndex = 0;
        public int history = 0;
        public Focus()
        {
            this.InitializeComponent();
            STileList.ItemsSource = topics;
            GetMoments("0");
        }
        
        private async void GetMoments()
        {
            string url = (mode == FocusContentType.Followee) ? ApiEndpoints.User.Moment(currentIndex) : ApiEndpoints.User.FavoriteTopicUpdate(currentIndex);
            var result = await RequestSender.Fetch<List<TopicInfo>>(url);
            if (!result.IsSuccess || result.Data == null)
            {
                //
                return;
            }
            var data=result.Data;
            topics.AddRange(data);
        }

        
        private void STileList_Loaded(object sender, RoutedEventArgs e)
        {
            STileList.ElementPrepared += (s, e) =>
            {
                if (STileList.ItemsSource != null)
                {
                    int current = e.Index;
                   
                    if (current > 0 && (current + 1) % 20 == 0 && current > history)
                    {
                        history = current;
                        GetMoments((current + 1).ToString());
                    }
                }

            };
        }

        private void TypeChoice_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var s=TypeChoice.SelectedItem as SelectorBarItem;
            if(s != null)
            {
                var tag = s.Tag;
                if(tag is string _tag)
                {
                    mode = _tag;
                    history = 0;
                    topics.Clear();
                    GetMoments();
                }
            }
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h != null)
            {
                var s = h?.DataContext as StandardPost;
                if (s != null)
                {
                    Frame.Navigate(typeof(Topic), s.pid);
                }
            }
        }
    }

    
}
