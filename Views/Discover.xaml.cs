
using CC98.Controls;
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using CC98.Services.Extensions;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
        public ObservableCollection<TopicInfo> topics=new();
        public ObservableCollection<SimpleTopicInfo> randomTopics = new();
        public int currentPage = 0;
        public int PageSize = 20;
        public Discover()
        {
            this.InitializeComponent();
            Set = ApplicationData.Current.LocalSettings;
            GetNewTopic();
            GetRandomTile();
        }
        
        private async Task<bool> GetNewTopic()
        {
            string newTopicUrl = ApiEndpoints.Topic.NewTopicList(currentPage*PageSize);
            var newTopicResult = await RequestSender.Fetch<List<TopicInfo>>(newTopicUrl);
            if (!newTopicResult.IsSuccess || newTopicResult.Data == null)
            {
                ValidationHelper.Log("加载数据失败", newTopicResult.Message);
                return false;
            }
            var data= newTopicResult.Data;
            topics.AddRange(data);   
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

        

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var ImageFrame = sender as SmartImage;
            if (ImageFrame != null)
            {
                string url = ImageFrame.Tag.ToString();
                var param = new Dictionary<string, string>()
                {
                    {"url",url },
                    {"type","image" }
                };
                var picviewer = new MediaViewer(param);
                picviewer.Activate();
            }
        }
        public int current = 0;

        private async void NaviBar_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b != null)
            {
                string tag=b.Tag.ToString();
                if (tag == "Back")
                {
                    if (current > 0)
                    {
                        current -= 20;
                        if (!await GetNewTopic())
                        {
                            current += 20;
                        }
                        
                    }
                    else
                    {
                        current = 0;
                        Flower.PlayAnimation("\uE946", "已到达最新页面");
                    }
                }
                else if (tag == "Forward")
                {
                    current += 20;
                    if (!await GetNewTopic())
                    {
                        current -= 20;
                    }
                    
                }
                PageIndex.Text = "第 " + (current / 20 + 1).ToString() + " 页";
                NewTopicViewer.ScrollToVerticalOffset(0);
            }
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

        private void MainText_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var tag = h?.Tag;
            if (tag == null) return;
            var param = new TopicNavigationInfo { TopicId = tag.ToInt() };
            Frame.Navigate(typeof(Topic), param);
           
        }

        private void Person_Click(object sender, RoutedEventArgs e)
        {
            var h= sender as HyperlinkButton;
            var tag = h?.Tag as TopicInfo;
            if (tag == null) return;
            if (tag.IsAnonymous) return;
            var param = new ProfileNavigationInfo { IsMe = tag.IsMe, UserId = tag.UserId??0};
            Frame.Navigate(typeof(Profile), param);
        }

        private void CopyId_Click(object sender, RoutedEventArgs e)
        {
            var m = sender as MenuFlyoutItem;
            if (m != null)
            {
                var _tag = m.Tag;
                if(_tag is string tag)
                {
                    var package = new DataPackage();
                    package.SetText(tag);
                    Clipboard.SetContent(package);
                    Flower.PlayAnimation("\uE930", "已复制帖子ID");
                }
            }
        }       
    } 
}
