using CC98.Kernel;
using CC98.Kernel.UserExperience;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.UI;
using static CC98.Chat;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98

{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Chat : Page
    {
        public ObservableCollection<Contact> contacts = new();
        public ObservableCollection<Msg> Msgs = new();
        public string type = "0";
        public Contact NewSession = new();
        public Chat()
        {
            this.InitializeComponent();
            ContactRepeater.ItemsSource = contacts;
            MessagesList.ItemsSource = Msgs;
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var param = e.Parameter as Dictionary<string, object>;
            if (param != null)
            {
                type = ValidationHelper.GetKey(param, "Type");
                if (type == "1")//由私信功能跳转
                {
                    var contact = param["Info"] as Contact;
                    if (contact != null)
                    {
                        NewSession = contact;//获取要私信的对象
                    }
                }
                GetRecent();
            }

        }
        private void ContactRepeater_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = ContactRepeater.SelectedIndex;
            if (i > -1)
            {
                history = 0;
                currentuid = contacts[i].mid;
                RefDialogs();
            }

        }
        private async void GetRecent()
        {
            string url = "https://api.cc98.org/message/recent-contact-users?from=0&size=10";
            string res = await RequestSender.SimpleRequest(url);
            if (!res.StartsWith("404:"))
            {
                var list = Deserializer.ToArray(res);
                if (list != null)
                {
                    string porturl = "https://api.cc98.org/user/basic?";
                    List<string> users = new();
                    List<SMsg> msgs = new();
                    foreach (var c in list)
                    {
                        var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(c.ToString());
                        string mid = js["userId"].ToString();//联系人CCID
                        string time = js["time"].ToString();
                        string text = js["lastContent"].ToString();
                        users.Add("id=" + mid);
                        msgs.Add(new SMsg { Mid = mid, Time = time, Text = text });
                    }

                    if ((!users.Contains("id=" + NewSession.mid)) && type == "1")
                    {
                        contacts.Add(NewSession);
                        ContactRepeater.SelectedIndex = 0;
                    }
                    porturl += string.Join("&", users);
                    if (users.Count > 0)
                    {
                        var portres = await CCloginservice.vpn.GetAsync(porturl);
                        if (portres.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string port = await portres.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(port))
                            {
                                var portlist = JsonConvert.DeserializeObject<JArray>(port);
                                Dictionary<string, SInfo> personinfo = new();

                                foreach (var p in portlist)
                                {
                                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(p.ToString());
                                    string name = info["name"].ToString();
                                    string purl = info["portraitUrl"].ToString();
                                    string id = info["id"].ToString();
                                    personinfo.Add(id, new SInfo { Name = name, PortraitUrl = purl });
                                }
                                foreach (SMsg m in msgs)
                                {
                                    if (personinfo.ContainsKey(m.Mid))
                                    {
                                        contacts.Add(new Contact { mid = m.Mid, name = personinfo[m.Mid].Name, url = personinfo[m.Mid].PortraitUrl, text = m.Text, time = m.Time });
                                    }

                                }
                                
                                if (type == "1")
                                {
                                    ContactRepeater.SelectedItem = contacts.First(c => c.mid == NewSession.mid);
                                }
                                if (contacts.Count > 0 && type != "1")
                                {
                                    ContactRepeater.SelectedIndex = 0;
                                }
                                //如果删改web端的sessionStorage，web端会出现错位。说明98的代码也有一定问题。
                            }
                        }
                    }

                }
            }

        }
        public string currentuid = "";//用于在刷新时记忆当前对话
        private async Task GetDialogs(string uid,string start)
        {
            string murl = "https://api.cc98.org/message/user/" + uid + "?from="+start+"&size=10";
            string restext=await RequestSender.SimpleRequest(murl);
            if (!restext.StartsWith("404:"))
            {
                if (!string.IsNullOrEmpty(restext))
                {
                    var list = Deserializer.ToArray(restext);
                    if (list != null)
                    {
                        history += 10;
                        foreach (var c in list)
                        {
                            var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(c.ToString());
                            string msgid = js["id"].ToString();//消息ID
                            bool isMe = js["receiverId"].ToString() == uid;
                            string text = UBBConverter.Convert(js["content"].ToString(), true);
                            string time = js["time"].ToString();
                            Msgs.Insert(0,new Msg
                            {
                                msgid = msgid,
                                text = text,
                                time = time,
                                isme = isMe
                            });
                        }
                    }
                }
            }
            
        }
        public int history = 0;//此值用于防止重复刷新
        private async void MoreMsg_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            if (currentuid != "")
            {
                await GetDialogs(currentuid, history.ToString());
            }
            
        }
        private async void RefDialogs()
        {
            Msgs.Clear();
            await GetDialogs(currentuid, "0");
            if (MessagesList.Items.Count > 0)
            {
                // 获取最后一个项目并滚动到它
                var lastItem = MessagesList.Items[MessagesList.Items.Count - 1];
                MessagesList.ScrollIntoView(lastItem);
            }
        }
        
        private async void More_Click(object sender, RoutedEventArgs e)
        {
            if (currentuid != "")
            {
                await GetDialogs(currentuid, history.ToString());
            }
        }
        private void Drawer_ImageClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            string ImageUrl = e.Link.ToString();
            var param = new Dictionary<string, string>()
{
    {"url",ImageUrl },
    {"type","image" }
};
            var picviewer = new MediaViewer(param);
            picviewer.Activate();

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
        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ReplyBody.Text))
            {
                Send.IsEnabled = false;
                var r = await RequestSender.SendPrivateMsg(Convert.ToInt32(currentuid), ReplyBody.Text);
                Send.IsEnabled = true;
                if (r == "1")
                {
                    ReplyBody.Text = "";
                    RefDialogs();
                }
                else
                {
                    status.Title = "发送失败";
                    status.Content = "这可能是网络不佳导致的，或者存在代码问题。";
                    status.IsOpen = true;
                }
            }
        }

        private void Ref_Click(object sender, RoutedEventArgs e)
        {
            RefDialogs();
        }

        private async void Drawer_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            var url = e.Link.ToString();
            var result = LinkAnalyzer.LinkDefinite(url);
            switch (result.Key)
            {
                case "topic":
                    if((App.Current as App).m_window is MainWindow mainwindow)
                    mainwindow.RootFrame.Navigate(typeof(Topic), result.Value);
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
                                        if ((App.Current as App).m_window is MainWindow _mainwindow)
                                        {
                                            _mainwindow.RootFrame.Navigate(typeof(Profile), param);
                                        }
                                            
                                    }
                                }
                            }

                        }

                        break;
                    }
                //using语句不能在switch语句中直接出现。因此，使用大括号包围这个case.
                   
                case "backlink":
                    if (result.Value == "bili")
                    {
                        var _datapackage = new DataPackage();
                        _datapackage.SetText(url);
                        Clipboard.SetContent(_datapackage);
                        Flower.PlayAnimation("\uE930", "已复制Bili外链");
                    }
                    break;
                default://自动复制到用户剪切板
                    var datapackage = new DataPackage();
                    datapackage.SetText(url);
                    Clipboard.SetContent(datapackage);
                    Flower.PlayAnimation("\uE930", "已复制外部链接");
                    break;
            }
        }
    }
    public partial class Msg : INotifyPropertyChanged
    {
        private string _time;
        private string _text;
        private bool _isme;
        private string _msgid;
        public string time
        {
            get { return _time; }
            set
            {
                if (_time != value)
                {
                    _time = value;
                    OnPropertyChanged(nameof(time));
                }
            }
        }

        public string text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(text));
                }
            }
        }

        public bool isme
        {
            get { return _isme; }
            set
            {
                if (_isme != value)
                {
                    _isme = value;
                    OnPropertyChanged(nameof(isme));
                }
            }
        }
        public string msgid
        {
            get { return _msgid; }
            set
            {
                if (_msgid != value)
                {
                    _msgid = value;
                    OnPropertyChanged(nameof(msgid));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class SInfo
    {
        public string Name { get; set; }
        public string PortraitUrl { get; set; }
    }
    public class SMsg
    {
        public string Text { get; set; }
        public string Time { get; set; }
        public string Mid { get; set; }
    }
    
    public partial class Contact : INotifyPropertyChanged
    {
        private string _text;
        private string _mid;//会话ID
        private string _time;
        private string _name;//会话名
        private string _url;//头像URL
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




        public string mid
        {
            get => _mid;
            set
            {
                if (_mid != value)
                {
                    _mid = value;
                    OnPropertyChanged(nameof(mid));
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
        public string name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(name));
                }
            }
        }
        public string url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged(nameof(url));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public partial class AlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ?
                HorizontalAlignment.Right :
                HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    

    
}
