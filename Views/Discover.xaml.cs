
using CC98.Controls;
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
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
        public ObservableCollection<SimpleTopicInfo> randomTopics=new();//必须是可观测集合，否则UI不显示。
        public int currentPage = 0;
        public int PageSize = 20;
        public Discover()
        {
            this.InitializeComponent();
            Set = ApplicationData.Current.LocalSettings;
            RandomTiles.ItemsSource = randomTopics;
            GetNewTopic(0);
            GetRandomTile();
        }
        
        private async Task<bool> GetNewTopic(int start)
        {
            string newTopicUrl = ApiEndpoints.Topic.NewTopicList(currentPage*PageSize);
            var newTopicResult = await RequestSender.Fetch<List<TopicInfo>>(newTopicUrl);
            if (!newTopicResult.IsSuccess || newTopicResult.Data == null)
            {
                return false;
            }
            var data= newTopicResult.Data;
            topics.AddRange(data);   
            return true;
        }

        
        private async void GetRandomTile()
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

        private void RefRandomTiles_Click(object sender, RoutedEventArgs e)
        {
            randomTopics.Clear();
            GetRandomTile();
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
                        if (!await GetNewTopic(current.ToString()))
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
                    if (!await GetNewTopic(current.ToString()))
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
                var p=h?.DataContext as RandomPost;
                if(p != null)
                {
                    Frame.Navigate(typeof(Topic),p.pid);
                }
            }
        }

        private void MainText_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h != null)
            {
                var pid = h.Tag as string;
                if (pid != null)
                {
                    Frame.Navigate(typeof(Topic), pid);
                }
            }
        }

        private void Person_Click(object sender, RoutedEventArgs e)
        {
            var h= sender as HyperlinkButton;
            if(h != null)
            {
                var tag=h.Tag as string;
                if(tag != null&&tag!="0")
                {
                    var param = new Dictionary<string, string>()
                        {
                            {"Mode","Others" },
                            {"UserId",tag }
                        };
                    Frame.Navigate(typeof(Profile), param);
                }
            }
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
    
    public partial class PostTemplateSelector : DataTemplateSelector
    {
        // 定义不同模板属性
        public DataTemplate? ImageTemplate { get; set; }
        public DataTemplate? TextOnlyTemplate { get; set; }
        

        // 重写选择方法
        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is TopicInfo topic)
            {
                if (topic.MediaContent.Thumbnail.Count > 0)
                {
                    return ImageTemplate;
                }
            }
            return TextOnlyTemplate;
        }

        
        
    }
    
}
