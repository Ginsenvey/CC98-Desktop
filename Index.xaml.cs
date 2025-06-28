using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using Windows.Storage;
using CCkernel;
using Microsoft.UI.Xaml.Documents;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Index : Page
    {
        public ObservableCollection<SectionCard> cards;
        public ObservableCollection<FTile> ftiles;
        public ApplicationDataContainer Set;
        
        public Index()
        {
            this.InitializeComponent();
            cards = new ObservableCollection<SectionCard>(){};
            
            ftiles= new ObservableCollection<FTile>();
            RecomList.ItemsSource = ftiles;
            Set = ApplicationData.Current.LocalSettings;
            
            Jar = new();
            client=new HttpClient();

            
        }
        private CookieContainer Jar;
        private HttpClient client;
        
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // ��ȡ���ݵĲ���
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                GetHotTopic();
            }
            else
            {
                GetHotTopic();
            }
        }
        private async void GetHotTopic()
        {
            //ֻ�ӻ����ж�ȡ��
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            string path = cacheFolder.Path+"/"+"IndexCache.json";
            string IndexText = ValidationHelper.JsonReader(path);
            var AllTopics = Deserializer.ToDictionary(IndexText);
            
            List<string> SectionNames = new List<string>() {"hotTopic","schoolEvent", "academics", "study", "emotion", "fleaMarket", "fullTimeJob", "partTimeJob" };
            List<string> _SectionNames = new List<string>() { "ʮ����", "У԰�","ѧ��֪ͨ", "ѧϰ���","���ԡ����", "�����г�" ,"��ְ�㳡","ʵϰ��ְ"};
            if(AllTopics != null)
            {
                for(int i = 0; i < 8; i++)
                {
                    string key = SectionNames[i];
                    if (AllTopics.TryGetValue(key, out object Topics))
                    {
                        if (Topics != null)
                        {
                            var TopicList = JsonConvert.DeserializeObject<JArray>(Topics.ToString());
                            if (TopicList != null && TopicList.Count > 0)
                            {
                                var tiles = new List<Tile>();
                                foreach (var Topic in TopicList)
                                {
                                    var TopicInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(Topic.ToString());
                                    
                                    string Section = "";
                                    if (TopicInfo.ContainsKey("boardName"))
                                    {
                                        Section = TopicInfo["boardName"].ToString();
                                    }

                                   
                                    string Title = string.Empty;
                                    if (TopicInfo["title"].ToString().Length > 21)
                                    {
                                        Title = TopicInfo["title"].ToString().Substring(0, 21) + "��";
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
                                    tiles.Add(new Tile {  section = Section, title = Title, uid = Pid,hasboardname=hasboardname });
                                }
                                cards.Add(new SectionCard { SectionName = _SectionNames[i], Tiles = tiles });
                            }
                        }
                    }
                }
                
                if(AllTopics.TryGetValue("recommendationReading",out var recom))
                {
                    if (recom != null)
                    {
                        var recomlist = JsonConvert.DeserializeObject<JArray>(recom.ToString());
                        ftiles.Clear();
                        foreach(var r in recomlist)
                        {
                            var js=JsonConvert.DeserializeObject<Dictionary<string,object>>(r.ToString());
                            string title = js["title"].ToString();
                            string content = js["content"].ToString();
                            string uid = js["url"].ToString();
                            string time = js["time"].ToString();
                            ftiles.Add(new FTile { content = content, time = time, uid ="cc98:/"+uid, title = title });
                        }
                        Pips.NumberOfPages = recomlist.Count;
                    }
                }
            }
            
            
        }
        

        private void ContentCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var translate = h.RenderTransform as TranslateTransform;
            AnimateCard(translate, 0, -5); // ���Ϸ��ƶ�
        }

        private void ContentCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var translate = h.RenderTransform as TranslateTransform;
            AnimateCard(translate, 0, 0); // �ָ�ԭλ
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
        private async void TopicItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as HyperlinkButton;
            var tag = button.Tag as string;//��ǰ��״̬�£�hû��DataContext.ֻ��ʹ��tag.
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
        
        public class FTile : INotifyPropertyChanged
        {
            private string _title;//����
            
            
            private string _uid;//����id
            

            private string _time;//ʱ��
            
            private string _content;//¥��id
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

            

            

            public string uid
            {
                get => _uid;
                set
                {
                    if (_uid != value)
                    {
                        _uid = value;
                        OnPropertyChanged(nameof(uid));
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

        private void RecomHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            string url = (sender as Hyperlink).NavigateUri.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                string param = url.Replace("cc98://topic/", "");
                Frame.Navigate(typeof(Topic),param);
            }
            
            
        }
    }
    public class SectionCard
    {
        public string SectionName { get; set; }
        public FluentIcons.Common.Symbol SectionIcon { get; set; }
        public List<Tile> Tiles { get; set; }
    }
    public class Tile : INotifyPropertyChanged
    {
        private string _title;//����
        private string _section;//����
        private string _author;//¥��
        private string _uid;//����id
        private string _reply;//�ظ���

        private string _time;//ʱ��
        private string _hit;//�ȶ�
        private string _rid;//¥��id
        private string _sort;//���
        private bool _hasboardname;
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

        public string author

        {
            get => _author;
            set
            {
                if (_author != value)
                {
                    _author = value;
                    OnPropertyChanged(nameof(author));
                }
            }
        }

        public string uid
        {
            get => _uid;
            set
            {
                if (_uid != value)
                {
                    _uid = value;
                    OnPropertyChanged(nameof(uid));
                }
            }
        }

        public string reply
        {
            get => _reply;
            set
            {
                if (_reply != value)
                {
                    _reply = value;
                    OnPropertyChanged(nameof(reply));
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

        public string hit
        {
            get => _hit;
            set
            {
                if (_hit != value)
                {
                    _hit = value;
                    OnPropertyChanged(nameof(hit));
                }
            }
        }
        public string rid
        {
            get => _rid;
            set
            {
                if (_rid != value)
                {
                    _rid = value;
                    OnPropertyChanged(nameof(rid));
                }
            }
        }
        public string sort
        {
            get => _sort;
            set
            {
                if (_sort != value)
                {
                    _sort = value;
                    OnPropertyChanged(nameof(sort));
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
