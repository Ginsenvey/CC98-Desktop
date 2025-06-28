using CCkernel;
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
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Devices;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Message : Page
    {
        public ObservableCollection<Contact> contacts;
        public ObservableCollection<Notice> notices;
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public Message()
        {
            this.InitializeComponent();
            contacts = new ObservableCollection<Contact>();
            notices = new ObservableCollection<Notice>();
 
            
          
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // ��ȡ���ݵĲ���
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                if (parameter == "0")
                {
                    
                    PrivateMsg.IsSelected=true;
                    
                }
                else if(parameter == "1") 
                {
                    SystemNotice.IsSelected=true;
                    
                }
                
            }
            else
            {

            }
        }
        
        private void ContactRepeater_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = ContactRepeater.SelectedIndex;
            if (i > -1)
            {
                string uid = contacts[i].mid;
                MsgFrame.Navigate(typeof(MyMsg), uid);
            }
            
        }
        private async void GetRecent()
        {
            string url = "https://api.cc98.org/message/recent-contact-users?from=0&size=10";
            var res = await CCloginservice.client.GetAsync(url);
            if (res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string restext = await res.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(restext))
                {
                    var list = JsonConvert.DeserializeObject<JArray>(restext);
                    if (list != null)
                    {
                        string porturl = "https://api.cc98.org/user/basic?";
                        List<string> users = new();
                        List<SMsg> msgs = new();
                        foreach (var c in list)
                        {
                            var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(c.ToString());
                            string mid = js["userId"].ToString();//��ϵ��CCID
                            string time = js["time"].ToString();
                            string text = js["lastContent"].ToString();
                            users.Add("id=" + mid);
                            msgs.Add(new SMsg { Mid=mid,Time=time,Text=text} );
                        }
                        porturl += string.Join("&",users);
                        if (users.Count > 0)
                        {
                            var portres = await CCloginservice.client.GetAsync(porturl);
                            if (portres.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string port =await portres.Content.ReadAsStringAsync();
                                if (!string.IsNullOrEmpty(port))
                                {
                                    var portlist = JsonConvert.DeserializeObject<JArray>(port);
                                    Dictionary<string,SInfo> personinfo = new();

                                    foreach (var p in portlist)
                                    {
                                        var info = JsonConvert.DeserializeObject <Dictionary<string, object>>(p.ToString());
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
                                    ContactRepeater.ItemsSource = contacts;
                                    
                                    //��ʵ�����ɾ��web�˵�sessionStorage��web��Ҳ����ִ�λ��˵��98�Ĵ���Ҳ��һ�����⡣
                                }
                            }
                        }
                    }
                    
                }
            }
            if (contacts.Count > 0)
            {
                ContactRepeater.SelectedIndex = 0;
            }
        }
        public int history = 0;
        private async void GetNotice(string start)
        {
            string NoticeText=await RequestSender.SystemNotice(start);
            if (NoticeText.StartsWith("404:"))
            {
                return;
            }
            else
            {
                var NoticeList = Deserializer.ToArray(NoticeText);
                if (NoticeList != null)
                {
                    if(NoticeList.Count > 0)
                    {
                        history += 10;
                        notices.Clear();
                        foreach (var notice in NoticeList)
                        {
                            var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(notice.ToString());
                            if (js != null)
                            {
                                string floor = "0";
                                string topicid = "0";
                                if (js["topicId"] != null)
                                {
                                    topicid = js["topicId"].ToString();
                                }
                                if (js["postBasicInfo"] != null)
                                {
                                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(js["postBasicInfo"].ToString());
                                    floor = info["floor"].ToString();
                                }
                                notices.Add(new Notice
                                {
                                    Title = js["title"].ToString(),
                                    Time = js["time"].ToString(),
                                    TopicId = topicid,
                                    Content = js["content"].ToString(),
                                    NoticeId = js["id"].ToString(),
                                    Floor = floor,
                                });
                                NoticeRepeater.ItemsSource = notices ;
                            }
                        }
                    }
                }
            }
        }
        private void NaviBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var bar = NaviBar.SelectedItem as SelectorBarItem;
            if (bar != null)
            {
                string tag = bar.Tag as string;
                if (tag == "0")
                {
                    NoticeViewer.Visibility = Visibility.Collapsed;
                    NoticeRepeater.ItemsSource = null;
                    notices.Clear();
                    MessageViewer.Visibility = Visibility.Visible;
                    GetRecent();
                }
                else if (tag == "1")
                {
                    NoticeViewer.Visibility = Visibility.Visible;
                    ContactRepeater.ItemsSource = null;
                    contacts.Clear();
                    MessageViewer.Visibility = Visibility.Collapsed;
                    GetNotice("0");
                }
            }
        }

        private void MoreNotice_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            GetNotice(history.ToString());
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
        public class Notice
        {
            public string Time { get; set; }
            public string Title { get; set; }
            public string NoticeId { get; set; }
            public string TopicId {  get; set; }
            public string Content { get; set; }
            public string Floor {  get; set; }

        }
        public class Contact : INotifyPropertyChanged
        {
            private string _text;
            private string _mid;//�ỰID
            private string _time;
            private string _name;//�Ự��
            private string _url;//ͷ��URL
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

        
    }
}
