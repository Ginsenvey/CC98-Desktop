
using CC98.Kernel;
using CC98.Kernel.UserExperience;
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
        public ObservableCollection<SectionCard> cards;
        public ObservableCollection<FlipPost> ftiles;
        public ApplicationDataContainer Set;
        public ImageSource ThemePic;
        public Index()
        {
            this.InitializeComponent();
            cards = new ObservableCollection<SectionCard>(){};
            ftiles= new ObservableCollection<FlipPost>();
            RecomList.ItemsSource = ftiles;
            Set = ApplicationData.Current.LocalSettings;
            GetTopic();
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
        private void GetTopic()
        {
            //只从缓存中读取。
            List<string> SectionNames = new List<string>() { "hotTopic", "schoolEvent", "academics", "study", "emotion", "fleaMarket", "fullTimeJob", "partTimeJob" };
            List<string> _SectionNames = new List<string>() { "十大话题", "校园活动", "学术通知", "学习天地", "感性·情感", "跳蚤市场", "求职广场", "实习兼职" };
            string jpath = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "IndexCache.json");
            var json = ValidationHelper.JsonReader(jpath);
            if (!json.StartsWith("10"))
            {
                var AllTopics = Deserializer.ToDictionary(json);
                if (AllTopics != null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        string key = SectionNames[i];
                        string content = ValidationHelper.GetKey(AllTopics, key);
                        if (content != "0")
                        {
                            var TopicList = Deserializer.ToArray(content);
                            if (TopicList != null && TopicList.Count > 0)
                            {
                                var tiles = new List<SimplePost>();
                                foreach (var Topic in TopicList)
                                {
                                    var TopicInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(Topic.ToString());
                                    if (TopicInfo == null) return;
                                    string Section = "";
                                    if (TopicInfo.ContainsKey("boardName"))
                                    {
                                        Section = TopicInfo["boardName"].ToString();
                                    }


                                    string Title = string.Empty;
                                    if (TopicInfo["title"].ToString().Length > 21)
                                    {
                                        Title = TopicInfo["title"].ToString().Substring(0, 21) + "…";
                                    }
                                    else
                                    {
                                        Title = TopicInfo["title"].ToString();
                                    }

                                    string Pid = TopicInfo["id"].ToString();
                                    bool hasboardname = false;
                                    if (key == "hotTopic")
                                    {
                                        hasboardname = true;
                                    }
                                    tiles.Add(new SimplePost { section = Section, title = Title, pid = Pid, hasboardname = hasboardname });
                                }
                                cards.Add(new SectionCard { SectionName = _SectionNames[i], Tiles = tiles, HexColor = ColorPaint.GenerateMorandiColorHex() });
                            }
                        }
                    }
                    string recom = ValidationHelper.GetKey(AllTopics, "recommendationReading");
                    if (recom != "0")
                    {
                        var recomlist = Deserializer.ToArray(recom);
                        ftiles.Clear();
                        if (recomlist != null)
                        {
                            foreach (var r in recomlist)
                            {
                                var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(r.ToString());
                                string title = js["title"].ToString();
                                string content = js["content"].ToString();
                                string pid = js["url"].ToString();
                                string time = js["time"].ToString();
                                ftiles.Add(new FlipPost { content = content, time = time, pid = "cc98:/" + pid, title = title });
                            }
                            Pips.NumberOfPages = recomlist.Count;
                        }


                    }
                }
                
            }
            
            
            
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
            if (button == null) return;
            var tag = button.Tag as string;//当前绑定状态下，h没有DataContext.只能使用tag.
            if (!string.IsNullOrEmpty(tag))
            {
                Frame.Navigate(typeof(Topic), tag);
            }
            
            


        }

        private void AuthurName_Click(object sender, RoutedEventArgs e)
        {
            var h=sender as HyperlinkButton;
            var tag = h.Tag as string;
            if(!string.IsNullOrEmpty(tag))
            {
                Set.Values["ProfileNaviMode"] = "Others";
                Set.Values["CurrentPerson"] = tag; ;
                if (tag != "-1")
                {
                    Frame.Navigate(typeof(Profile),tag);
                }
            }
        }
        
        

        private void RecomHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            string url = (sender as Hyperlink).NavigateUri.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                string param = url.Replace("cc98://topic/", "");
                Frame.Navigate(typeof(Topic),param);
            }
            
            
        }

        private void Ref_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class FlipPost : INotifyPropertyChanged
    {
        private string _title;//标题


        private string _pid;//话题id


        private string _time;//时间

        private string _content;//内容
        public string title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(title));
                }
            }
        }





        public string pid
        {
            get => _pid;
            set
            {
                if (_pid != value)
                {
                    _pid = value;
                    OnPropertyChanged(nameof(pid));
                }
            }
        }



        public string time
        {
            get => _time;
            set
            {
                if (_time != value)
                {
                    _time = value;
                    OnPropertyChanged(nameof(time));
                }
            }
        }


        public string content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(content));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class SectionCard
    {
        public string SectionName { get; set; }
        public string HexColor { get; set; }
        public List<SimplePost> Tiles { get; set; }
    }
    public class SimplePost : INotifyPropertyChanged
    {
        private string _title;//标题
        private string _section;//版面    
        private string _pid;//话题id
        private bool _hasboardname;//是否已包含版面名
       
        public string title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(title));
                }
            }
        }
        public string section
        {
            get => _section;
            set
            {
                if (_section != value)
                {
                    _section = value;
                    OnPropertyChanged(nameof(section));
                }
            }
        }
        public string pid
        {
            get => _pid;
            set
            {
                if (_pid != value)
                {
                    _pid = value;
                    OnPropertyChanged(nameof(pid));
                }
            }
        }

        public bool hasboardname
        {
            get => _hasboardname;
            set
            {
                if (_hasboardname != value)
                {
                    _hasboardname = value;
                    OnPropertyChanged(nameof(hasboardname));
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    
  
    
}
