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
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public Message()
        {
            this.InitializeComponent();
            contacts = new()
            {   
            };
            
            InitializeClient();
            
            

        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                if (parameter == "0")
                {
                    GetRecent();
                    PrivateMsg.IsSelected=true;
                }
                
            }
            else
            {

            }
        }
        private void InitializeClient()
        {
            if (Set.Values.ContainsKey("Access"))
            {
                MainWindow.loginservice.client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Set.Values["Access"] as string);
            }

        }
        private void ContactRepeater_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = ContactRepeater.SelectedIndex;
            string uid = contacts[i].mid;
            MsgFrame.Navigate(typeof(MyMsg), uid);
        }
        private async void GetRecent()
        {
            string url = "https://api.cc98.org/message/recent-contact-users?from=0&size=10";
            var res = await MainWindow.loginservice.client.GetAsync(url);
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
                            string mid = js["userId"].ToString();//联系人CCID
                            string time = js["time"].ToString();
                            string text = js["lastContent"].ToString();
                            users.Add("id=" + mid);
                            msgs.Add(new SMsg { Mid=mid,Time=time,Text=text} );
                        }
                        porturl += string.Join("&",users);
                        if (users.Count > 0)
                        {
                            var portres = await MainWindow.loginservice.client.GetAsync(porturl);
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
                                    
                                    //事实上如果删改web端的sessionStorage，web端也会出现错位。说明98的代码也有一定问题。
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
        
        public class Contact : INotifyPropertyChanged
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

        
        
    }
}
