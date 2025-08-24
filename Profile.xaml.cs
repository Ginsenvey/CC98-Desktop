using CCkernel;
using CCUserModel;
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.WinUI.UI.Controls;
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
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.Playback;
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
        public Info profile=new Info() { 
            Id="0",
            Name="未知用户",
            Port="",
            IsFollowing=false,
        };
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
            if (param != null)
            {
                string uid = param["UserId"];
                string mode = param["Mode"];
                if (mode == "Others")
                {
                    CurrentPerson = uid;
                    CurrentMode = mode;
                    LoadProfile(mode, uid);
                    LoadMyTopic("0", uid, mode);
                }
                else
                {
                    LoadProfile(mode, uid);
                    LoadMyTopic("0", uid, mode);
                    SignIn();
                }
            }
            
        }
        public string CurrentPerson = "0";
        public string CurrentMode = "Me";
        private async void SignIn()
        {
            string SignInResult = await RequestSender.SignIn();
            if(SignInResult=="0")
            {
                SignStatus.Text = "签到失败";
                SignStatusIcon.IconVariant = IconVariant.Regular;
            }
            else if (SignInResult == "1")
            {
                SignStatus.Text = "签到中";
                SignStatusIcon.IconVariant = IconVariant.Filled;
            }
            else
            {
                SignStatus.Text = "已签到";
                SignStatusIcon.IconVariant = IconVariant.Filled;
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
            string ProfileText = await RequestSender.SimpleRequest(ProfileUrl);
            if (!ProfileText.StartsWith("404:"))
            {
                var js = Deserializer.ToDictionary(ProfileText);
                if (js == null) return;
                bool isme = false;
                if (mode == "Me")
                {
                    isme = true;
                    Set.Values["Uid"] = ValidationHelper.GetKey(js, "id");
                    Set.Values["Me"] = ValidationHelper.GetKey(js, "name");
                    Set.Values["Portrait"] = ValidationHelper.GetKey(js, "portraitUrl");
                }
                //针对从其他页面进入Profile
                //如果还没有打开过个人空间，从其他地方进入自己的主页时，前者返回0，isme仍为false.
                //因此可以确保不能关注自己
                if(ValidationHelper.IsTokenExist(Set,"Uid") ==ValidationHelper.GetKey(js, "id"))
                {
                    isme=true;
                }
                profile = new Info()
                {
                    Name = ValidationHelper.GetKey(js, "name"),
                    Id = ValidationHelper.GetKey(js, "id"),
                    Popularity = ValidationHelper.GetKey(js, "popularity"),
                    Fan = ValidationHelper.GetKey(js, "fanCount"),
                    Follow = ValidationHelper.GetKey(js, "followCount"),  
                    Logtime = ValidationHelper.GetKey(js, "lastLogOnTime"),
                    Port = ValidationHelper.GetKey(js, "portraitUrl"),
                    Signature = UBBConverter.Convert(ValidationHelper.GetKey(js, "signatureCode"), true),
                    Posts = ValidationHelper.GetKey(js, "postCount"),
                    Wealth = ValidationHelper.GetKey(js,"wealth"),
                    IsOthers = !isme,
                    Regtime = ValidationHelper.GetKey(js,"registerTime"),
                    IsFollowing=ValidationHelper.GetKey(js, "isFollowing")=="True",//注意大写
                };
                MyProfile.ProfilePicture = await ImageResolver.LoadWebImage(profile.Port);
                InfoContent.DataContext = profile;
                SignBoard.DataContext = profile;
            }
            else
            {
                de.Text = ProfileText;
            }   
            
            

        }
        private async void LoadMyTopic(string start,string uid,string mode)
        {
            string RecentTopicUrl = "https://api.cc98.org/me/recent-topic?from=" + start + "&size=11";
            if (mode=="Others")
            {
                RecentTopicUrl = "https://api.cc98.org/user/" + uid + "/recent-topic?userid=" + uid + "&from=" + start + "&size=11";
            }
            var Res = await CCloginservice.vpn.GetAsync(RecentTopicUrl);
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
        private async void Drawer_ImageResolving(object sender, ImageResolvingEventArgs e)
        {
            var defr = e.GetDeferral();
            var Source = e.Url;
            if (Source == null) return;

            try
            {
                switch (Source)
                {
                    case string url when ImageResolver.IsWebUrl(url):
                        e.Image = await ImageResolver.LoadWebImage(url);
                        break;

                    case string path when ImageResolver.IsLocalPath(path):
                        e.Image = await ImageResolver.LoadLocalImage(path);
                        break;
                }
            }
            catch
            {
                e.Image = null;
            }
            e.Handled = true;
            defr.Complete();

        }
        private void STileButton_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var s = h?.DataContext as STile;
            if (s != null)
            {
                if (s.pid != null)
                {
                    Frame.Navigate(typeof(Topic), s.pid);
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
                    if (current > 0 && (current + 1) % 11 == 0)
                    {
                        LoadMyTopic((current + 1).ToString(), CurrentPerson, CurrentMode);
                    }
                }
            };
        }

        private async void SignBoard_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            string link = e.Link;
            var result = LinkAnalyzer.LinkDefinite(link);
            switch (result.Key)
            {
                case "topic":
                    Frame.Navigate(typeof(Topic), result.Value);
                    break;
                case "user":
                    {
                        string _url = "https://api.cc98.org/user/name/" + result.Value;
                        string infotext = await RequestSender.SimpleRequest(_url);

                        if (!infotext.StartsWith("404:"))
                        {
                            var Info = Deserializer.ToDictionary(infotext);
                            if (Info != null)
                            {
                                string uid = Info["id"].ToString();
                                if (uid != null)
                                {
                                    if (uid.All(char.IsDigit))
                                    {
                                        var param = new Dictionary<string, string>()
                                        {
                                            {"Mode","Others" },
                                            {"UserId",uid }
                                        };
                                        Frame.Navigate(typeof(Profile), param);
                                    }
                                }
                            }

                        }

                        break;
                    }
                //using语句不能在switch语句中直接出现。因此，使用大括号包围这个case.
                case "board":
                    Frame.Navigate(typeof(Board), result.Value);
                    break;
                case "file":
                    if (result.Value == "image")
                    {
                        try
                        {
                            var param = new Dictionary<string, string>()
                            {
                                {"url",link },
                                {"type","image" }
                             };
                            var picviewer = new MediaViewer(param);
                            picviewer.Activate();
                        }
                        catch
                        {

                        }
                    }   
                    break;
                case "backlink":
                    if (result.Value == "bili")
                    {
                        Flower.PlayAnimation("\uE930", "已复制Bili外链");
                    }
                    break;
                default://自动复制到用户剪切板
                    var datapackage = new DataPackage();
                    datapackage.SetText(result.Value);
                    Clipboard.SetContent(datapackage);
                    Flower.PlayAnimation("\uE930", "已复制外部链接");
                    break;
            }
        }

        private void FollowList_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;

            if (h != null)
            {
                var i = h.DataContext as Info;
                if (i != null)
                {
                    if (!i.IsOthers)
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

            public string Regtime { get; set; }

            public bool IsOthers { get; set; }
            public bool IsFollowing {  get; set; }
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

        private void StartChat_Click(object sender, RoutedEventArgs e)
        {
            var c = new Contact { mid = profile.Id, name = profile.Name, url = profile.Port };
            var p = new Dictionary<string, object>()
                    {
                        {"Type","1" },
                        {"Info",c }
                    };
            if (profile.Id != "0")
            {
                Frame.Navigate(typeof(Message), p);
            }
            
        }

        private async void Follow_Click(object sender, RoutedEventArgs e)
        {
            bool flag = profile.IsFollowing;
            string mode=  flag?"0":"1";
            string restext = await RequestSender.Follow(mode, profile.Id);
            if (restext == "1")
            {
                LoadProfile(CurrentMode, CurrentPerson);
                Flower.PlayAnimation("\uE930", flag?"已取消关注":"已关注");
            }
            else
            {
                Flower.PlayAnimation("\uEA39", "操作失败");
            }
        }
    }
    public partial class BooltoVisibilityConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool flag)
            {
                return flag?Visibility.Visible:Visibility.Collapsed;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public partial class BooltoVariantConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool flag)
            {
                return flag ? IconVariant.Filled:IconVariant.Regular;
            }
            else
            {
                return IconVariant.Regular;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public partial class RBooltoVisibilityConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool flag)
            {
                return flag ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public partial class BoolToFollowTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isFollowing)
            {
                return isFollowing ? "取消关注" : "关注";
            }
            return "关注";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
