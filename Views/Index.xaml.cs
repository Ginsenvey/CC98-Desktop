
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using DevWinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Index : Page
    {
        public ObservableCollection<SectionCard> sections=[];
        public ObservableCollection<FlipTopic> flipTopics=[];
        public CC98HomeDataManager.HomeStatistics? Statistics { get; private set; }
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        private readonly CC98HomeDataManager _dataManager;
        public ImageSource? ThemePic;
        public Index()
        {
            this.InitializeComponent();
            _dataManager=CC98HomeDataManager.Instance;
            LoadFromCacheAsync();
            LoadSet();
        }
       
        
        private void LoadSet()
        {
            var _Theme = ValidationHelper.GetValue(Set, "ThemePic");
            if (_Theme != "0")
            {
                ThemePresenter.ImageSource = new BitmapImage(new Uri(_Theme));
            }
        }
        private async Task LoadFromCacheAsync()
        {
            //只从缓存中读取。
            List<string> SectionNames = new List<string>() { "hotTopic", "schoolEvent", "academics", "study", "emotion", "fleaMarket", "fullTimeJob", "partTimeJob" };
            List<string> _SectionNames = new List<string>() { "十大话题", "校园活动", "学术通知", "学习天地", "感性·情感", "跳蚤市场", "求职广场", "实习兼职" };
            sections.Clear();
            flipTopics.Clear();
            for(int i=0; i<SectionNames.Count; i++)
            {
                string propertyName = SectionNames[i];
                string name= _SectionNames[i];
                var hotTopics = await _dataManager.GetTopicPartitionAsync(propertyName);
                var section=new SectionCard { SectionName=name,IndexTopics=hotTopics,HexColor=ColorPaint.GenerateMorandiColorHex()};
                sections.Add(section);
            }
            var recommendations = await _dataManager.GetRecommendationReadingAsync();
            if (recommendations == null)
            {
                App.Logger.Write("Index", "获取推荐阅读列表失败");
                return;
            }
            foreach (var item in recommendations)
            {
                item.Url = $"cc98:/{item.Url}";
            }
            flipTopics.AddRange(recommendations);
            Pips.NumberOfPages = recommendations.Count;         
        }

        
        private void ContentCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var translate = h?.RenderTransform as TranslateTransform;
            AnimateCard(translate, 0, -5); // 向上方移动
        }

        private void ContentCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var translate = h?.RenderTransform as TranslateTransform;
            AnimateCard(translate, 0, 0); // 恢复原位
        }
        private void AnimateCard(TranslateTransform transform, double targetX, double targetY)
        {
            var storyboard = new Storyboard();

            var animationX = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animationX, transform);
            Storyboard.SetTargetProperty(animationX, "X");

            var animationY = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animationY, transform);
            Storyboard.SetTargetProperty(animationY, "Y");

            storyboard.Children.Add(animationX);
            storyboard.Children.Add(animationY);
            storyboard.Begin();
        }

        public bool IsOnlineMode = false;
        public string NaviCode = "";
        private void TopicItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as HyperlinkButton;
            var tag = button?.Tag;
            if (tag == null) return;
            var param = new TopicNavigationInfo { TopicId = tag.ToInt()};
            Frame.Navigate(typeof(Topic), param);
        }

        

        private void RecomHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            string url = (sender as Hyperlink).NavigateUri.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                int topicId=int.Parse(url.Replace("cc98://topic/", ""));
                var param=new TopicNavigationInfo { TopicId = topicId };
                Frame.Navigate(typeof(Topic),param);
            } 
        }

    }
    
}
