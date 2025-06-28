using CCkernel;
using DevWinUI;
using FluentIcons.Common;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.System;
using static App3.Topic;
using static System.Net.WebRequestMethods;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Profile : Page
    {
        public ObservableCollection<STile> stiles=new ObservableCollection<STile>();
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        
        public Profile()
        {
            this.InitializeComponent();
            
            SimpleTile.ItemsSource = stiles;
            
            
            
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var param = e.Parameter as Dictionary<string,string>;
            string uid = param["UserId"];
            string mode=param["Mode"];
            if (mode=="Others")
            {
                CurrentPerson = uid;
                CurrentMode=mode;
                LoadProfile(mode,uid);
                LoadMyTopic("0",uid,mode);   
            }
            else
            {
                LoadProfile(mode,uid);
                LoadMyTopic("0",uid,mode);
                SignIn(); 
            }
        }
        public string CurrentPerson = "1";
        public string CurrentMode = "Me";
        private async void SignIn()
        {
            string SignInResult = await RequestSender.SignIn();
            if(SignInResult=="0")
            {
                WealthInfo.Text = "财富值(签到失败)";
            }
            else if (SignInResult == "1")
            {
                WealthInfo.Text = "财富值(签到中)";
            }
            else
            {
                WealthInfo.Text = "财富值(已签到)";
            }
        }
        //用户个人页面检查跳转参数
        private async void LoadProfile(string mode,string uid)
        {
            string ProfileUrl = "https://api.cc98.org/me";
            if (mode == "Others")
            {
                ProfileUrl = "https://api.cc98.org/user/" + uid;
            }
            var ProfileRes = await CCloginservice.client.GetAsync(ProfileUrl);
            if (ProfileRes.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string ProfileText = await ProfileRes.Content.ReadAsStringAsync();
                var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(ProfileText);
                Set.Values["Uid"] = js["id"].ToString();
                bool isme = false;
                if (mode == "Me")
                {
                    isme = true;
                    Set.Values["Me"] = js["name"].ToString();
                    Set.Values["Portrait"] = js["portraitUrl"].ToString();
                }
                var profile = new Info()
                {
                    Name = js["name"].ToString(),
                    Id = js["id"].ToString(),
                    Popularity = js["popularity"].ToString(),
                    Fan = js["fanCount"].ToString(),
                    Follow = js["followCount"].ToString(),
                    Logtime = js["lastLogOnTime"].ToString(),
                    Port = js["portraitUrl"].ToString(),
                    Signature = UBBConverter.Convert(js["signatureCode"].ToString(), true),
                    Posts = js["postCount"].ToString(),
                    Wealth = js["wealth"].ToString(),
                    IsMe = isme

                };
                InfoContent.DataContext = profile;
                SignBoard.DataContext = profile;
            }
            else
            {
                de.Text = "网络问题";
            }

        }
        private async void LoadMyTopic(string start,string uid,string mode)
        {
            string RecentTopicUrl = "https://api.cc98.org/me/recent-topic?from=" + start + "&size=11";
            if (mode=="Others")
            {
                RecentTopicUrl = "https://api.cc98.org/user/" + uid + "/recent-topic?userid=" + uid + "&from=" + start + "&size=11";
            }
            var Res = await CCloginservice.client.GetAsync(RecentTopicUrl);
            if (Res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string ProfileText = await Res.Content.ReadAsStringAsync();
                var Posts = JsonConvert.DeserializeObject<JArray>(ProfileText);
                if (Posts != null)
                {
                    foreach (var post in Posts)
                    {
                        var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(post.ToString());
                        stiles.Add(new STile
                        {
                            text = js["title"].ToString(),
                            pid = js["id"].ToString(),
                            author = js["boardId"].ToString(),
                            time = js["time"].ToString()
                        });
                    }
                }
            }
            else
            {
                de.Text = "获取失败";
            }


        }

        public class Info
        {
            public string Name { get; set; }
            public string Signature { get; set; }
            public string Id { get; set; }
            public string Posts { get; set; }
            public string Wealth { get; set; }
            public string Logtime { get; set; }
            public string Port { get; set; }
            public string Popularity {  get; set; }
            public string Fan {  get; set; }
            public string Follow { get; set; }
            public bool IsMe { get; set; }
        }
        public class STile : INotifyPropertyChanged
        {
            private string _text;
            private string _section;
            
            private string _pid;//主题ID
            private string _author;
            private string _hit;
            private string _reply;
            private string _time;
            private FluentIcons.Common.Symbol _symbol;
            public string text
            {
                get => _text;
                set
                {
                    if (_text != value)
                    {
                        _text = value;
                        OnPropertyChanged(nameof(text));
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
            public FluentIcons.Common.Symbol symbol
            {
                get => _symbol;
                set
                {
                    if (_symbol != value)
                    {
                        _symbol = value;
                        OnPropertyChanged(nameof(symbol));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void STileButton_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var s = h?.DataContext as STile;
            if (s != null)
            {
                if (s.pid != null)
                {
                    Frame.Navigate(typeof(Topic),s.pid);
                }
                
            }
        }
        
        
        private void SimpleTile_Loaded(object sender, RoutedEventArgs e)
        {
            SimpleTile.ElementPrepared += (s, e) =>
            {
                if (SimpleTile.ItemsSource != null)
                {
                    int current = e.Index;
                    if (current > 0 && (current+1 )% 11 == 0)
                    {
                        LoadMyTopic((current + 1).ToString(), CurrentPerson,CurrentMode);
                    }
                }
            };
        }

        private async void SignBoard_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            string link = e.Link;
            var result = LinkAnalyzer.LinkDefinite(link);
            if (result.Key == "topic")
            {
                Frame.Navigate(typeof(Topic), result.Value);
            }
            else if(result.Key == "user")
            {
                string url = "https://api.cc98.org/user/name/" + result.Value;
                using var client = new HttpClient();
                var PortRes = await client.GetAsync(url);
                if (PortRes.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string content = await PortRes.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        try
                        {
                            var Info = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                            string uid = Info["id"].ToString();
                            //de.Text = uid;
                            Set.Values["ProfileNaviMode"] = "Others";
                            Frame.Navigate(typeof(Profile), uid);
                        }
                        catch (Exception ex)
                        {
                            //de.Text = ex.Message;
                        }
                    }
                    else
                    {
                        //de.Text = "空返回";
                    }
                }
            }
        }

        private void Follow_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            
            if(h!= null)
            {
                var i=h.DataContext as Info;
                if (i!= null)
                {
                    if (i.IsMe)
                    {
                        string tag = h.Tag as string;
                        if (!string.IsNullOrEmpty(tag))
                        {
                            Frame.Navigate(typeof(Friend), tag);
                        }
                    }
                }
                
                
            }
        }
    }

}
