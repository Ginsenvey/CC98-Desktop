using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CCkernel;
using System.Net.Security;
using Windows.Security.Credentials;
using System.Net.Http.Json;
using Newtonsoft.Json;
using Windows.Storage;
using System.Collections;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using DevWinUI;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using Windows.UI.WebUI;
using FluentIcons.WinUI;
using Microsoft.UI.Composition.SystemBackdrops;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(GridTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            this.SystemBackdrop = new AcrylicSystemBackdrop(DesktopAcrylicKind.Base);
            Vault = new();
            collections = new ObservableCollection<string>()
            {
                
            };
            //likecollection.MenuItemsSource = collections;
            Set = ApplicationData.Current.LocalSettings;
            CheckLoginStatus();
            UnreadCount = 0;
            
            contentframe.Navigate(typeof(Index),"hotTopic");
        }   
        private PasswordVault Vault;
        public ApplicationDataContainer Set;
        public ObservableCollection<string> collections;
        public static CCloginservice loginservice =new CCloginservice();
        private async void CheckLoginStatus()
            
        {
            var info = Set.Values;
            if(info.TryGetValue("Access", out var AccessCode))//是否初始化Access
            {
                //是否为有效登录状态
                if (AccessCode != null)
                {
                    if (!string.IsNullOrEmpty(AccessCode.ToString()))
                    {
                        using (HttpClient client = new HttpClient() { })
                        {
                            try
                            {
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessCode as string);
                                // 发送 GET 请求
                                HttpResponseMessage response = await client.GetAsync("https://api.cc98.org/me/unread-count");

                                // 确保响应成功
                                if (response.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    Auth();
                                }
                                else if(response.StatusCode == HttpStatusCode.OK)
                                {
                                    de.Text = "在线模式";
                                    string CheckResponse = await response.Content.ReadAsStringAsync();
                                    var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(CheckResponse);
                                    UnreadCount = Convert.ToInt16(js["messageCount"]);
                                    MsgCount.Value= Convert.ToInt16(js["messageCount"]);
                                    loginservice.client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",AccessCode as string);
                                    
                                    
                                }

                            }
                            catch (HttpRequestException ex)
                            {
                                de.Text=ex.Message;
                            }
                        }
                    }
                    else
                    {
                        Auth();
                    }
                }
                else
                {
                    Auth();
                }
            }
            else//没有初始化，首次登录
            {
                de.Text = "未初始化";
                Auth();
            }

        }
        //检查第一个字段IsActive(是否获取R和A)，否则弹出登录窗口。登录成功IsActive赋值1.
        private  async void Auth()
        {
            var loginservice=new CCkernel.CCloginservice();
            if (Set.Values.ContainsKey("Refresh"))
            {
                if (Set.Values["Refresh"] != null)
                {
                    string refresh = Set.Values["Refresh"] as string;
                    if (!string.IsNullOrEmpty(refresh)&&refresh!="0")
                    {
                        string token = await loginservice.RefreshToken(refresh);
                        Set.Values["Access"] = token;
                        Set.Values["IsActive"] = "1";
                        loginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        de.Text = "鉴权状态正常";
                    }
                    else
                    {
                        Set.Values["IsActive"] = "0";
                    }
                }
                else
                {
                    Set.Values["IsActive"] = "0";
                }
            }
            else
            {
                Set.Values["IsActive"] = "0";
            }
            
        }
        private async void GetLikeCollection()
        {
            string LikeUrl = "https://api.cc98.org/me/favorite-topic-group";
            try
            {
                var LikeRes = await loginservice.client.GetAsync(LikeUrl);
                if (LikeRes.StatusCode == HttpStatusCode.OK)
                {
                    collections.Clear();
                    string LikeText = await LikeRes.Content.ReadAsStringAsync();
                    var likes = JsonConvert.DeserializeObject<Dictionary<string, object>>(LikeText);
                    var LikeList = JsonConvert.DeserializeObject<JArray>(likes["data"].ToString());
                    likecollection.MenuItems.Clear();
                    foreach (var like in LikeList)
                    {
                        var likeinfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(like.ToString());
                        string collection = likeinfo["name"].ToString();
                        string sortid = (Convert.ToInt16(like["id"]) + 100).ToString();

                        likecollection.MenuItems.Add(new NavigationViewItem { Content = collection, Tag = sortid, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.Add } });
                        collections.Add(collection);
                    }
                }
            }
            catch(Exception ex)
            {

            }
        }
        public int UnreadCount { get; set; }
        private void Navi_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if(args.SelectedItem as NavigationViewItem != null)
            {
                switch((args.SelectedItem as NavigationViewItem).Tag as string)
                {
                    case "0":
                        contentframe.Navigate(typeof(Index));
                        break;
                    case "1":
                        Set.Values["ProfileNaviMode"] = "Me";
                        Set.Values["CurrentPerson"] = "1";
                        contentframe.Navigate(typeof(Profile),"Me");
                        break;
                    case "2":
                        contentframe.Navigate(typeof(Post), "Recommend");
                        break;
                    case "3":
                        contentframe.Navigate(typeof(Section));
                        break;
                    case "4":
                        contentframe.Navigate(typeof(Discover));
                        break;
                    case "5":
                        contentframe.Navigate(typeof(Board), "68");
                        break;
                    case "6":
                        contentframe.Navigate(typeof(Board), "81");
                        break;
                    case "7":
                        contentframe.Navigate(typeof(Board), "80");
                        break;
                    case "8":
                        contentframe.Navigate(typeof(Board), "235");
                        break;
                    case "9":
                        contentframe.Navigate(typeof(Setting));
                        break;
                    case "10":
                        GetLikeCollection();
                        break;
                    default:
                        var selection = args.SelectedItem as NavigationViewItem;
                        if ((selection.Tag as string).Length > 2)
                        {
                            string GroupId = (Convert.ToInt16((args.SelectedItem as NavigationViewItem).Tag as string) - 100).ToString();
                            string GroupName=selection.Content as string;
                            var param = new Dictionary<string, string>()
                            {
                                {"mode","favorite" },
                                {"gid", GroupId},
                                {"name",GroupName}
                            };
                            contentframe.Navigate(typeof(Repeater), param);
                        }
                        break;
                }
                
            }
            
        }
        
        private void Navi_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (contentframe.CanGoBack)
            {
                contentframe.GoBack();
            }
        }

        private void msgflyout_Click(object sender, RoutedEventArgs e)
        {
            contentframe.Navigate(typeof(Message),"0");
        }
    }
}
