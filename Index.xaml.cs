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
        public ObservableCollection<Tile> tiles;
        public ObservableCollection<FTile> ftiles;
        public ApplicationDataContainer Set;
        public CCloginservice loginservice;
        public Index()
        {
            this.InitializeComponent();
            tiles = new ObservableCollection<Tile>(){};
            TileList.ItemsSource = tiles;
            ftiles= new ObservableCollection<FTile>();
            RecomList.ItemsSource = ftiles;
            Set = ApplicationData.Current.LocalSettings;
            loginservice=new CCloginservice();
            Jar = new();
            client=new HttpClient();

            
        }
        private CookieContainer Jar;
        private HttpClient client;

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
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
            string IndexUrl = "https://api.cc98.org/config/index";
            HttpClientHandler handler= new HttpClientHandler()
            {
                CookieContainer=Jar,
                UseCookies = true
            };

            var response = await client.GetStringAsync(IndexUrl);
            var AllTopics=JsonConvert.DeserializeObject<Dictionary<string,object>>(response);
            if(AllTopics != null)
            {
                if (AllTopics.TryGetValue("hotTopic", out object HotTopics))
                {
                    if (HotTopics != null)
                    {
                        var TopicList = JsonConvert.DeserializeObject<JArray>(HotTopics.ToString());
                        if(TopicList != null && TopicList.Count > 0)
                        {
                            tiles.Clear();
                            foreach(var Topic in TopicList)
                            {
                                var TopicInfo = JsonConvert.DeserializeObject<Dictionary<string,object>>(Topic.ToString());
                                string Author = "匿名";
                                if (TopicInfo["authorName"] != null)
                                {
                                    Author = TopicInfo["authorName"].ToString();
                                }
                                string AuthorId = "-1";
                                if (TopicInfo["authorUserId"] as string != "-1")
                                {
                                    AuthorId= TopicInfo["authorUserId"].ToString() ;
                                }
                                string Section = TopicInfo["boardName"].ToString();
                                string Time = TopicInfo["createTime"].ToString();
                                string Title=TopicInfo["title"].ToString();
                                string Uid = TopicInfo["id"].ToString();
                                string Hit = TopicInfo["hitCount"].ToString();
                                string Reply = TopicInfo["replyCount"].ToString();
                                tiles.Add(new Tile {author="@ "+Author,section=Section,title=Title,uid=Uid,hit=Hit,reply=Reply ,rid=AuthorId});
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
                        Pips.NumberOfPages = ftiles.Count;
                    }
                }
            }
            
        }
        public class Tile : INotifyPropertyChanged
        {
            private string _title;//标题
            private string _section;//版面
            private string _author;//楼主
            private string _uid;//话题id
            private string _reply;//回复数

            private string _time;//时间
            private string _hit;//热度
            private string _rid;//楼主id
            private string _sort;//序号
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
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void ContentCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var border = sender as Border;
            var translate = border.RenderTransform as TranslateTransform;
            AnimateCard(translate, 0, -5); // 向上方移动
        }

        private void ContentCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var border = sender as Border;
            var translate = border.RenderTransform as TranslateTransform;
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
        private async void TopicItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as HyperlinkButton;
            var SelectedTile = button?.DataContext as Tile;
            if(SelectedTile != null)
            {
                string uid = SelectedTile.uid;
                if (uid != null)
                {
                    Frame.Navigate(typeof(Topic),uid);
                }
                
            }
            
            


        }

        private void AuthurName_Click(object sender, RoutedEventArgs e)
        {
            var h=sender as HyperlinkButton;
            var tile=h?.DataContext as Tile;
            if(tile != null)
            {
                Set.Values["ProfileNaviMode"] = "Others";
                Set.Values["CurrentPerson"] = tile.rid; ;
                if (tile.rid != "-1")
                {
                    Frame.Navigate(typeof(Profile),tile.rid);
                }
            }
        }
        public class FTile : INotifyPropertyChanged
        {
            private string _title;//标题
            
            
            private string _uid;//话题id
            

            private string _time;//时间
            
            private string _content;//楼主id
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
}
