using CCkernel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using static App3.Index;
using static App3.Topic;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Discover : Page
    {
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        public ObservableCollection<StandardPost> tiles=new();
        public ObservableCollection<RandomPost> randomtiles=new();//必须是可观测集合，否则UI不显示。
        public Discover()
        {
            this.InitializeComponent();
           
            Set = ApplicationData.Current.LocalSettings;
            
            RandomTiles.ItemsSource = randomtiles;
            GetNewTopic("0");
            GetRandomTile();
            
        }
        
        private async void GetNewTopic(string start)
        {
            
            string NewTopicUrl = "https://api.cc98.org/topic/new?from=" + start + "&size=20";
            string NewTopicText=await RequestSender.SimpleRequest(NewTopicUrl);
            if (!NewTopicText.StartsWith("404"))
            {
                var NewTopicList = Deserializer.ToArray(NewTopicText);
                if (NewTopicList != null)
                {
                    if(NewTopicList.Count > 0)
                    {
                        tiles.Clear();
                        foreach (var Topic in NewTopicList)
                        {
                            try
                            {
                                var topic = Deserializer.ToItem(Topic.ToString());
                                if (topic != null)
                                {
                                    tiles.Add(topic);
                                    //此加载过程有问题。
                                }
                            }
                            catch(Exception ex)
                            {

                            }
                        }
                    }
                }
            }
            
        }

        
        private async void GetRandomTile()
        {
            string NewTopicUrl = "https://api.cc98.org/topic/random-recent?size=10";
            try
            {
                string access = Set.Values["Access"] as string;
                if (!string.IsNullOrEmpty(access))
                {
                    
                    var RandomRes = await CCloginservice.client.GetAsync(NewTopicUrl);
                    if (RandomRes.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string RandomText = await RandomRes.Content.ReadAsStringAsync();
                        var js = JsonConvert.DeserializeObject<JArray>(RandomText);
                        if (js != null)
                        {
                            if (js.Count > 0)
                            {
                                randomtiles.Clear();
                                foreach (var news in js)
                                {
                                    var tile = JsonConvert.DeserializeObject<Dictionary<string, object>>(news.ToString());
                                    string rid = "0";
                                    if (tile["userId"] != null)
                                    {
                                        rid = tile["userId"].ToString();
                                    }
                                    string pid = tile["id"].ToString();
                                    string hit = tile["hitCount"].ToString();
                                    string title = tile["title"].ToString();
                                    string time = tile["time"].ToString();
                                    string reply = tile["replyCount"].ToString();
                                    randomtiles.Add(new RandomPost { title = title, pid = pid, time = time, hit = hit, reply = reply });
                                }
                            }
                        }
                    }
                    else
                    {

                    }
                }

            }
            catch (Exception ex)
            {
                
            }
        }

        private void RefRandomTiles_Click(object sender, RoutedEventArgs e)
        {
            GetRandomTile();
        }

        

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var ImageFrame = sender as Image;
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

        private void NaviBar_Click(object sender, RoutedEventArgs e)
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
                }
                GetNewTopic(current.ToString());
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
    }
    public class RandomPost
    {
        public string title { get; set; }
        public string reply { get; set; }
        public string hit { get; set; }
        public string pid { get; set; }
        public string time { get; set; }
    }
    public class PostTemplateSelector : DataTemplateSelector
    {
        // 定义不同模板属性
        public DataTemplate? ImageTemplate { get; set; }
        public DataTemplate? TextOnlyTemplate { get; set; }
        

        // 重写选择方法
        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is StandardPost post)
            {
                if (post.images.Count > 0)
                {
                    return ImageTemplate;
                }
            }
            return TextOnlyTemplate;
        }

        
        
    }
    public class StringToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    return new BitmapImage(new Uri(path));
                }
                catch
                {
                    return GetFallbackImage();
                }
            }
            return GetFallbackImage(); // 处理空值
        }

        private static ImageSource GetFallbackImage()
        {
            // 返回默认图片或null
            return new BitmapImage(new Uri("ms-appx:///Assets/DefaultImage.png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
