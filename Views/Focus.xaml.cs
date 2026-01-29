
using CC98.Kernel;
using CC98.Objects;
using CommunityToolkit.Labs.WinUI;
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
        public ObservableCollection<TopicInfo> Tiles=[];
        public Focus()
        {
            this.InitializeComponent();
            STileList.ItemsSource = Tiles;
            GetMoments("0");
        }
        public string mode = "0";
        private async void GetMoments(string start)
        {
            string MomentsUrl = $"https://api.cc98.org/me/followee/topic?from={start}&size=20&order=0"; ;
            if (mode == "1")
            {
                MomentsUrl = $"https://api.cc98.org/topic/me/favorite?from={start}&size=20&order=1";
            }
            
            string MomentsText = await RequestSender.SimpleRequest(MomentsUrl);
            if (!MomentsText.StartsWith("404"))
            {
                var Moments = Deserializer.ToArray(MomentsText);
                if (Moments != null)
                {
                    if (Moments.Count > 0)
                    {
                        foreach (var Topic in Moments)
                        {
                            try
                            {
                                var topic = Deserializer.ToItem(Topic.ToString());
                                if (topic != null)
                                {
                                    Tiles.Add(topic);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                }
            }

        }

        public int history = 0;
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
                    Tiles.Clear();
                    GetMoments("0");
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
