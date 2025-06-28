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
using Windows.ApplicationModel.DataTransfer;
using System.Text.RegularExpressions;
using System.Reflection;
using Windows.Security.Cryptography.Certificates;
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
            
            LoadSettings();
            App.ThemeChanged += OnAppThemeChanged;
            Vault = new();
            collections = new ObservableCollection<string>() { };      
            CheckLoginStatus();
            UnreadCount = 0;
            InitializeTimer();
            FetchIndex();
            contentframe.Navigate(typeof(Index),"hotTopic");
        }
        private void InitializeTimer()
        {
            
            SyncTimer = new DispatcherTimer();
            SyncTimer.Interval = TimeSpan.FromSeconds(120);  
            SyncTimer.Tick += DispatcherTimer_Tick;  
            SyncTimer.Start();  
        }
        
        
        private void DispatcherTimer_Tick(object? sender, object e)
        {
            FetchIndex();
        }
        private async void FetchIndex()
        {
            string IndexText = await RequestSender.SimpleRequest("https://api.cc98.org/config/index");
            if (!IndexText.StartsWith("404"))
            {
                ValidationHelper.JsonWritter(IndexText, "IndexCache.json");
            }
            else
            {
                Flower.PlayAnimation("\uEA39", "������ҳ����ʧ��");
            }
        }
        private void LoadSettings()
        {
            if (Set.Values.ContainsKey("Effect"))
            {
                string effect = (string)Set.Values["Effect"];
                switch (effect)
                {
                    case "0":
                        
                        this.SystemBackdrop = new MicaSystemBackdrop();
                        break;
                    case "1":
                        
                        this.SystemBackdrop = new MicaSystemBackdrop(MicaKind.BaseAlt);
                        break;
                    case "2":
                        
                        this.SystemBackdrop = new AcrylicSystemBackdrop();
                        break;
                    case "3":
                        
                        this.SystemBackdrop = new AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
                        break;
                    case "4":
                        
                        this.SystemBackdrop = null;
                        break;
                    default:
                        
                        this.SystemBackdrop = new MicaSystemBackdrop();
                        break;
                }
            }
            else
            {
                Set.Values["Effect"] = "0";
                this.SystemBackdrop = new MicaSystemBackdrop();

            }
            if (Set.Values.ContainsKey("Theme"))
            {
                string theme = (string)Set.Values["Theme"];
                if (theme == "0")
                {
                    RootGrid.RequestedTheme = ElementTheme.Light;
                }
                else if (theme == "1")
                {
                   RootGrid.RequestedTheme = ElementTheme.Dark;
                }
                else
                {
                   RootGrid.RequestedTheme = ElementTheme.Default;
                }
            }
            else
            {
                Set.Values["Theme"] = "2";
                RootGrid.RequestedTheme = ElementTheme.Default;
            }
            //�������ɫ�Ƿ��ʼ����Ĭ��ֵ��mid-autumn.
            if (!ValidationHelper.IsTokenExist(Set,"ThemePic"))
            {
                string themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Themes");
                var Files = Directory.GetFiles(themesPath, "*.jpg", SearchOption.AllDirectories);
                var file = Files[0];
                Set.Values["Themepic"]= file;
            }
            
        }
        //ͬ������ʱ��:ÿ���ӷ���һ��Tick�źţ�ʹ�ÿɵ�������enum������ֵ֮���л���ֻ��Ϊ����һֵʱ��������ҳˢ�¡�
        private DispatcherTimer SyncTimer { get; set; }
        private void OnAppThemeChanged(ElementTheme theme)
        {
            // ���� RootGrid ������
            RootGrid.RequestedTheme = theme;
        }
        private PasswordVault Vault;
        
        public ApplicationDataContainer Set= ApplicationData.Current.LocalSettings;
        public ObservableCollection<string> collections;
        
        private async void CheckLoginStatus()
            
        {
            var info = Set.Values;
            if(info.TryGetValue("Access", out var AccessCode))//�Ƿ��ʼ��Access
            {
                //�Ƿ�Ϊ��Ч��¼״̬
                if (AccessCode != null)
                {
                    if (!string.IsNullOrEmpty(AccessCode.ToString()))
                    {
                        using (HttpClient client = new HttpClient() { })
                        {
                            try
                            {
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessCode as string);
                                HttpResponseMessage response = await client.GetAsync("https://api.cc98.org/me/unread-count");
                                if (response.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    Auth();
                                }
                                else if(response.StatusCode == HttpStatusCode.OK)
                                {
                                   
                                    string CheckResponse = await response.Content.ReadAsStringAsync();
                                    var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(CheckResponse);
                                    UnreadCount = Convert.ToInt16(js["messageCount"])+ Convert.ToInt16(js["replyCount"]);
                                    MsgCount.Value = UnreadCount;
                                    CCloginservice.client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",AccessCode as string);
                                    bool r=await GetFavorites();
                                    if(r)
                                    {
                                        LoadFavorites();
                                    }
                                    
                                }

                            }
                            catch (HttpRequestException ex)
                            {
                             
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
            else//û�г�ʼ�����״ε�¼
            {
                //de.Text = "δ��ʼ��";
                Auth();
            }

        }
        //����һ���ֶ�IsActive(�Ƿ��ȡR��A)�����򵯳���¼���ڡ���¼�ɹ�IsActive��ֵ1.
        private  async void Auth()
        {
            
            if (Set.Values.ContainsKey("Refresh"))
            {
                if (Set.Values["Refresh"] != null)
                {
                    string refresh = Set.Values["Refresh"] as string;
                    if (!string.IsNullOrEmpty(refresh)&&refresh!="0")
                    {
                        string token = await CCloginservice.RefreshToken(refresh);
                        Set.Values["Access"] = token;
                        Set.Values["IsActive"] = "1";
                        CCloginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        
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
        private async Task<bool> GetFavorites()
        {
            var Favorites = await RequestSender.FavoritesList();
            if (Favorites != null)
            {
                if (Favorites.Count > 0)
                {
                    //��ʱ�洢�ղؼ��б�
                    string FavoJson = JsonConvert.SerializeObject(Favorites);
                    Set.Values["Favorites"] = FavoJson;
                    return true;
                }
                else
                {
                    Flower.PlayAnimation("\uE783", "�����ղؼ�");
                    likecollection.MenuItems.Add(new NavigationViewItem { Content = "����������ղؼ�", Tag = "200", Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.List } });
                    return false;
                }
            }
            else
            {
                Flower.PlayAnimation("\uEA39", "ͬ���ղؼ�ʧ��");
                return false;
            }
        }
        private void LoadFavorites()
        {
            if (ValidationHelper.IsTokenExist(Set, "Favorites"))
            {
                var LikeList = JsonConvert.DeserializeObject<JArray>(Set.Values["Favorites"].ToString());
                foreach (var like in LikeList)
                {
                    var likeinfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(like.ToString());
                    string collection = likeinfo["name"].ToString();
                    string sortid = "f" + like["id"].ToString();
                    likecollection.MenuItems.Add(new NavigationViewItem { Content = collection, Tag = sortid, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.List } });  
                }
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
                        var param = new Dictionary<string, string>()
                        {
                            {"Mode","Me" },
                            {"UserId","1" }//�Լ���1��������ģʽ0���ֿ���
                        };
                        contentframe.Navigate(typeof(Profile),param);
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
                        
                        contentframe.Navigate(typeof(Setting), "0");
                        break;
                    case "10":
                        
                        break;
                    case "11":
                        contentframe.Navigate(typeof(Focus));
                        break;
                    default:
                        var selection = args.SelectedItem as NavigationViewItem;
                        if ((selection.Tag as string).Contains("f"))
                        {
                            string GroupId = (selection.Tag as string).Replace("f", "");
                            string GroupName = selection.Content as string;
                            var param2 = new Dictionary<string, string>()
                            { 
                                {"mode","favorite" },
                                { "gid", GroupId},
                                { "name",GroupName}
                            }
                        ;
                        contentframe.Navigate(typeof(Repeater), param2);
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

        

        private void search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var a = sender as AutoSuggestBox;
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string input=a.Text;
                if (input != "")
                {
                    List<string> suggestions = new List<string>() { "�������� #" + input + "#", "�����û� #" + input + "#" };
                    sender.ItemsSource = suggestions;
                    string pattern = @"^CC\d{7}$";
                    bool isTopic = Regex.IsMatch(input, pattern);
                    if (isTopic)
                    {
                        suggestions.Add("�������:" + input);
                    }
                }
                
            }

        }
        public int SearchMode = -1;
        private async void search_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem != null)
            {
                string temp = args.SelectedItem.ToString();
                List<string> type = new List<string>()
                {
                    "�������� #" + sender.Text + "#",
                    "�����û� #" + sender.Text + "#",
                    "�������:" + sender.Text
                };
                
                for (int i=0;i<type.Count;i++)
                {
                    if (temp.Equals(type[i]))
                    {
                        SearchMode = i;
                        break;
                    }
                }
                if (temp!=null)
                {
                    SemanticSearch(sender.Text);

                }
            }
            
        }

        
        //���ڣ�������ʷ���ᱻ��¼�����ػ��档ֻ����ǰ100����¼���Ƚ��ȳ���
        //���û������ı�ʱ���Զ�ģ��������ʷ��¼��
        private void SemanticSearch(string key)
        {
            var p = new Dictionary<string, string>();
            switch (SearchMode)
            {
                case 0:
                    p = new Dictionary<string, string>()
                            {
                                {"type","topic" },
                                {"key",key }
                            };
                    contentframe.Navigate(typeof(Search), p);
                    break;
                case 1:
                    p = new Dictionary<string, string>()
                            {
                                {"type","user" },
                                {"key",key}
                            };
                    contentframe.Navigate(typeof(Search), p);
                    break;
                case 2:
                    contentframe.Navigate(typeof(Topic), key.Replace("CC",""));
                    break;
                default:
                    break;
            }
        }

        
    }
}
