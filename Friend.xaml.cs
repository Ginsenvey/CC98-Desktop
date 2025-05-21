using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static App3.Message;
using System.ComponentModel;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Friend : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<Friends> friends;
        public Friend()
        {
            this.InitializeComponent();
            friends = new ObservableCollection<Friends>();
            InitializeClient();
            
        }

        private void InitializeClient()
        {
            if (Set.Values.ContainsKey("Access"))
            {
                MainWindow.loginservice.client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Set.Values["Access"] as string);
            }
            
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (!string.IsNullOrEmpty(parameter))
            {
                Set.Values["CurrentFriendType"] = parameter;
                if (parameter == "follower")
                {
                    FriendType.Text = "粉丝";
                }
                else if(parameter == "followee")
                {
                    FriendType.Text = "关注";
                }
                

               
            }
            if(Set.Values["CurrentFriendType"] as string != null)
            {
                LoadFriend(Set.Values["CurrentFriendType"] as string, "0");
            }
            

        }
        private async void LoadFriend(string type,string start)
        {
            string url = "https://api.cc98.org/me/"+type+"?from="+start+"&size=10";
            var response =await MainWindow.loginservice.client.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string json = await response.Content.ReadAsStringAsync();
                var js = JsonConvert.DeserializeObject<JArray>(json);
                if (js != null)
                {
                    List<string> Uids= new List<string>();
                    List<string> Params = new List<string>();
                    foreach (var item in js)
                    {
                        Uids.Add(item.ToString());
                        Params.Add("id="+item.ToString());
                    }
                    if (Uids.Count > 0)
                    {
                        string MInfoUrl = "https://api.cc98.org/user?" + string.Join("&", Params);
                        var portres = await MainWindow.loginservice.client.GetAsync(MInfoUrl);
                        if (portres.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string port = await portres.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(port))
                            {
                                var portlist = JsonConvert.DeserializeObject<JArray>(port);
                                Dictionary<string, MInfo> personinfo = new();

                                foreach (var p in portlist)
                                {
                                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(p.ToString());
                                    string name = info["name"].ToString();
                                    string purl = info["portraitUrl"].ToString();
                                    string id = info["id"].ToString();
                                    string post = info["postCount"].ToString();
                                    string follower = info["fanCount"].ToString();
                                    personinfo.Add(id, new MInfo { name = name, url = purl,post=post,follower=follower });
                                }
                                foreach(var f in Uids)
                                {
                                    friends.Add(new Friends
                                    {
                                        uid = f,
                                        name = personinfo[f].name,
                                        url = personinfo[f].url,
                                        post = personinfo[f].post,
                                        follower = personinfo[f].follower
                                    });
                                }
                                Collection.ItemsSource = friends;
                            }
                        }
                    }

                }
            }
        }

        private void TileContent_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if(h!= null)
            {
                var f = h?.DataContext as Friends;
                if (f != null)
                {
                    Set.Values["ProfileNaviMode"] = "Others";
                    Set.Values["CurrentPerson"] = f.uid;
                    Frame.Navigate(typeof(Profile), f.uid);
                }
            }
        }
        public int history = 0;
        private void Collection_Loaded(object sender, RoutedEventArgs e)
        {
            Collection.ElementPrepared += (s, e) =>
            {
                if (Collection.ItemsSource != null)
                {
                    int current = e.Index;
                    
                    if (current > 0 && (current + 1) % 10 == 0 && current > history)
                    {
                        
                        history = current;
                        LoadFriend(Set.Values["CurrentFriendType"] as string, (current + 1).ToString());

                    }
                }
            };
        }

        
    }
    public class MInfo
    {
        public string name { get; set; }
        public string url { get; set; }
        public string post { get; set; }
        public string follower { get; set; }
    }
    public class Friends: INotifyPropertyChanged
    {
        private string _follower;
        private string _uid;//UID
        private string _post;
        private string _name;//用户名
        private string _url;//头像URL
        public string follower

        {
            get => _follower;
            set
            {
                if (_follower != value)
                {
                    _follower = value;
                    OnPropertyChanged(nameof(follower));
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



        public string post
        {
            get => _post;
            set
            {
                if (_post != value)
                {
                    _post = value;
                    OnPropertyChanged(nameof(post));
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
