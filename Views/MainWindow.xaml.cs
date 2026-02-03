using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using CommunityToolkit.WinUI.Converters;
using DevWinUI;
using FluentIcons.Common;
using FluentIcons.WinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.BadgeNotifications;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;
using Windows.Security.Credentials;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Streams;
using static CC98.Kernel.ApiScope.ApiEndpoints;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<CategoryBase> MenuItems { get; } = new ObservableCollection<CategoryBase>();
        public ObservableCollection<CategoryBase> FooterMenuItems { get; } = new ObservableCollection<CategoryBase>();
        public Frame RootFrame => contentframe;//用于在嵌套的Frame中导航
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<string> collections=[];

        public int UnreadCount { get; set; }
        public MainWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(UserArea);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cc98.ico");
            AppWindow.SetIcon(iconPath);
            AppWindow.SetTaskbarIcon(iconPath);
            this.AppWindow.Changed += AppWindow_Changed; ;
            LoadSettings();
            App.ThemeChanged += OnAppThemeChanged;
            Messenger.Instance.NavigationItemAdded += OnNavigationItemAdded;
            var dataManager = CC98HomeDataManager.Instance;
            Surfing();
        }

        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (args.DidPresenterChange && this.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                // 检查窗口是否最小化
                if (presenter.State == OverlappedPresenterState.Minimized)
                {
                    // 取消最小化到任务栏，改为隐藏到托盘
                    this.AppWindow.Hide();
                }
            }
        }

        private void ShowTips(string title,string content)
        {
            StatusReport.Title = title;
            StatusReport.Content = content;
            StatusReport.IsOpen = true;
        }
        private void Surfing()
        {
            CheckLoginStatus();
            LoadProfile();
            LoadMenuItem();
            InitializeTimer();
        }
        private async void LoadProfile()
        {
            string port = ValidationHelper.GetValue(Set, "Portrait");
            if (port != "0")
            {
                try
                {
                    MyPicture.ProfilePicture = await ImageResolver.LoadWebImage(port);
                }
                catch { }
            }
            else
            {
               MyPicture.ProfilePicture = new BitmapImage(new Uri("ms-appx:///Assets/cc98.png"));
            }

           
        }
        private void OnNavigationItemAdded(NavigationItem item)
        {
            bool flag = true;
            foreach (var menu in MenuItems)
            {
                if(menu is NavigationItem i)
                {
                    if (i.Tag == item.Tag)
                    {
                        flag = false;
                    }
                }
            }
            if (flag)
            {
                MenuItems.Add(item);
            }
        }
        private void LoadMenuItem()
        {
            var Favorite=new NavigationGroup {Name="集锦", IsEditable = false };
            var PinnedGroup = new NavigationGroup{Name = "推荐", IsEditable = true };
            MenuItems.Add(new NavigationItem{Name = "今日话题",IconSymbol = FluentIcons.Common.Symbol.DesignIdeas,Tag = "Index",IsEditable=false} );
            MenuItems.Add(new NavigationItem { Name = "全部版面", IconSymbol = FluentIcons.Common.Symbol.Board, Tag = "Section", IsEditable = false });
            MenuItems.Add(new NavigationItem { Name = "新帖", IconSymbol = FluentIcons.Common.Symbol.Note, Tag = "Discover", IsEditable = false });
            MenuItems.Add(Favorite);
            MenuItems.Add(new NavigationItem { Name = "动态", IconSymbol = FluentIcons.Common.Symbol.Home, Tag = "Focus", IsEditable = false });    
            MenuItems.Add(new NavigationItem { Name = "收藏集", IconSymbol = FluentIcons.Common.Symbol.StarLineHorizontal3, Tag = "Favorite", IsEditable = false });
            MenuItems.Add(PinnedGroup);      
            FooterMenuItems.Add(new NavigationItem { Name = "消息", IconSymbol = FluentIcons.Common.Symbol.MailRead, Tag = "Message",IsEditable=false });
            FooterMenuItems.Add(new NavigationItem { Name = "设置", IconSymbol = FluentIcons.Common.Symbol.StarSettings, Tag = "Setting",IsEditable=false});
        }
        private async void PinOff_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as MenuFlyoutItem)?.Tag as string;
            if (tag == null) return;
            int boardId=int.Parse(tag);
            string url = ApiEndpoints.Board.EditFocusBoards(boardId);
            var result = await RequestSender.Delete(url);
            if (!result.IsSuccess)
            {
                //
                return;
            }
            string custom_boards = ValidationHelper.GetValue(Set, "CustomBoards");
            if (custom_boards != "0")
            {
                var boardinfo = JsonSerializer.Deserialize<Dictionary<string, string>>(custom_boards);
                if (boardinfo != null)
                {
                    if (boardinfo.ContainsKey(tag))
                    {
                        boardinfo.Remove(tag);
                    }
                }
            }
            var item = MenuItems.OfType<NavigationItem>().First(g => g.Tag == tag);
            MenuItems.Remove(item);

        }

        private async void GetFocusBoards()//同步客户端和在线关注版块的信息
        {
            string custom_boards = ValidationHelper.GetValue(Set, "CustomBoards");
            if (custom_boards!="0")
            {
                memory = JsonSerializer.Deserialize<Dictionary<int, string>>(custom_boards)??new Dictionary<int, string>();
            }
            else
            {
                Set.Values["CustomBoards"] = "0";
            }
            //初始化本地缓存
            string profileUrl = ApiEndpoints.User.UserProfile(true,0);
            var profileResult = await RequestSender.Fetch<UserInfo>(profileUrl);
            if (!profileResult.IsSuccess || profileResult.Data == null)
            {
                if (memory != null)
                {
                    foreach (var b in memory)
                    {
                        MenuItems.Add(new NavigationItem { Name = b.Value, IconSymbol = BoardIcon.GetSymbol(b.Key, b.Value), Tag = b.Key.ToString(), IsEditable = true });
                    }
                }
                return;
            }
            var data = profileResult.Data;
            var boards = data.CustomBoards;
            foreach (var board in boards)
            {
                AddBoards(board);
            }   
        }
        public Dictionary<int, string> memory = new();
        private async void AddBoards(int boardId)
        {
            //此方法将检测本地是否已存储板块，没有则添加。无论本地是否已经存在，都会加载到导航栏。
            //先判断本地存储是否有此板块
            if (!memory.ContainsKey(boardId))
            {
                string boardDataUrl = ApiEndpoints.Board.BoardInfo(boardId);
                var boardDataResult = await RequestSender.Fetch<BoardData>(boardDataUrl);
                if (!boardDataResult.IsSuccess || boardDataResult.Data == null)
                {
                    return;
                }
                var data=boardDataResult.Data;
                memory.Add(boardId, data.Name);
                MenuItems.Add(new NavigationItem { Name = data.Name, IconSymbol = BoardIcon.GetSymbol(boardId, data.Name), Tag = boardId.ToString(), IsEditable = true });
                string boardjsontext = JsonSerializer.Serialize(memory);
                Set.Values["CustomBoards"] = boardjsontext;
               
            }
            else
            {
                //如果本地存储有此板块，则直接添加
                MenuItems.Add(new NavigationItem { Name = memory[boardId], IconSymbol = BoardIcon.GetSymbol(boardId, memory[boardId]), Tag = boardId.ToString(), IsEditable = true });
            }
        }
        private async void LoadIndex()
        {
            string tag = ValidationHelper.GetValue(Set, "TitlePage");
            if (tag!="0")
            {
                switch (tag)
                {
                    case "1":

                        var index = await FetchIndex();
                        if (!index)
                        {
                            Flower.Play("\uEA39", "刷新首页失败");
                        }
                        contentframe.Navigate(typeof(Index));
                        break;
                    
                    case"2":
                        var param = new ProfileNavigationInfo { IsMe = true };
                        contentframe.Navigate(typeof(Profile),param);
                        break;
                    case "3":
                        contentframe.Navigate(typeof(Discover));
                        break;
                    
                }
            }
            else
            {
                Set.Values["TitlePage"] = "1";
                var index = await FetchIndex();
                if (index)
                {
                    contentframe.Navigate(typeof(Index));
                }
                
            }
            
        }
        private  void InitializeTimer()
        {
            SyncTimer = new DispatcherTimer();
            SyncTimer.Interval = TimeSpan.FromSeconds(120);  
            SyncTimer.Tick += DispatcherTimer_Tick;  
            SyncTimer.Start();  
        }
        
        
        private  async void DispatcherTimer_Tick(object? sender, object e)
        {
            await FetchIndex();
            RefreshMessage();
        }
        private async Task<bool> FetchIndex()
        {
            string url = ApiEndpoints.Forum.Index();
            return await CC98HomeDataManager.Instance.RefreshFromApiAsync(url);
        }
        private  void LoadSettings()
        {
            string effect = ValidationHelper.GetValue(Set, "Effect");
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
            string theme = ValidationHelper.GetValue(Set, "Theme");
            if (theme == "1")
            {
                RootGrid.RequestedTheme = ElementTheme.Light;
            }
            else if (theme == "2")
            {
                RootGrid.RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                RootGrid.RequestedTheme = ElementTheme.Default;
            }
            
            if (ValidationHelper.GetValue(Set,"ThemePic")=="0")
            {
                string themesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Themes");
                var Files = Directory.GetFiles(themesPath, "*.jpg", SearchOption.AllDirectories);
                var file = Files[0];
                Set.Values["Themepic"]= file;
            }
            //string color = "CardGradient1Brush";
            //GridTitleBar.Background = (Brush)Application.Current.Resources[color];
            //Navi.Background= (Brush)Application.Current.Resources[color];
        }
        
        private DispatcherTimer SyncTimer { get; set; }
        private void OnAppThemeChanged(ElementTheme theme)
        {
            // 更新 RootGrid 的主题
            RootGrid.RequestedTheme = theme;
        }
        
        
        private async void CheckLoginStatus()   
        {
            string Access = PasswordManager.RetrievePassword("Access");
            if (string.IsNullOrEmpty(Access))
            {
                //
                return;
            }
            LoginService.vpn.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Access);
            var url = ApiEndpoints.User.UnreadMessage();
            var result = await RequestSender.Fetch<UnreadMessageInfo>(url);
            if (!result.IsSuccess || result.Data == null)
            {
                //
                if (result.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    //登录失效
                }
                return;
            }
            var data= result.Data;
            UnreadCount = data.MessageCount + data.AtCount + data.ReplyCount + data.SystemCount;
            BadgeNotificationManager.Current.SetBadgeAsCount((uint)UnreadCount);
            GetFocusBoards();
            await GetFavorites();
            LoadIndex();
        }
        
        

        private async void RefreshMessage()
        {
            var url = ApiEndpoints.User.UnreadMessage();
            var result = await RequestSender.Fetch<UnreadMessageInfo>(url);
            if (!result.IsSuccess || result.Data == null)
            {
                App.Logger.Write("MainWindow","刷新未读消息失败", $"{result.StatusCode}:{result.Message}");
                return;
            }
            var data = result.Data;
            UnreadCount = data.MessageCount + data.AtCount + data.ReplyCount + data.SystemCount;
            BadgeNotificationManager.Current.SetBadgeAsCount((uint)UnreadCount);
        }

        
        private async Task<bool> GetFavorites()
        {
            string favoritesInfoUrl=ApiEndpoints.User.FavoritesList();
            var favoritesInfoResult = await RequestSender.Fetch<FavoritesInfo>(favoritesInfoUrl);
            if (!favoritesInfoResult.IsSuccess || favoritesInfoResult.Data == null)
            {
                //
                return false;
            }
            var data= favoritesInfoResult.Data;
            var groups = data.FavoriteTopicGroups;
            if (groups.Count > 0)
            {
                //临时存储收藏夹列表
                string FavoJson = JsonSerializer.Serialize(groups);
                Set.Values["Favorites"] = FavoJson;
                return true;
            }
            else
            {
                Flower.Play("\uE783", "暂无收藏夹");
                return false;
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
                    List<string> suggestions = new List<string>() { "搜索主题 #" + input + "#", "搜索用户 #" + input + "#" };
                    sender.ItemsSource = suggestions;
                    string pattern = @"^\d{7}$";
                    bool isTopic = Regex.IsMatch(input, pattern);
                    if (isTopic)
                    {
                        suggestions.Add("浏览主题:" + input);
                    }
                }
                
            }

        }
        public int SearchMode = -1;
        private  void search_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem != null)
            {
                string temp = args.SelectedItem.ToString();
                List<string> type = new List<string>()
                {
                    "搜索主题 #" + sender.Text + "#",
                    "搜索用户 #" + sender.Text + "#",
                    "浏览主题:" + sender.Text
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

        
      
        private void SemanticSearch(string key)
        {
            var p = new Dictionary<string, string>();
            switch (SearchMode)
            {
                case 0:
                    var param = new SearchNavigationInfo { SearchType = SearchType.Topic };
                    contentframe.Navigate(typeof(Search), p);
                    break;
                case 1:
                    //SearchUser(key);
                    break;
                case 2:
                    contentframe.Navigate(typeof(Topic), key.Replace("cc",""));
                    break;
                default:
                    break;
            }
        }
       

        private void Back_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            AnimatedIcon.SetState(BackIcon, "PointerOver");
        }

        private void Back_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            AnimatedIcon.SetState(BackIcon, "Normal");
            
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (contentframe.CanGoBack)
            {
                contentframe.GoBack();
            }
        }

        private void Navi_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if(args.InvokedItemContainer?.Tag is string tag)
            {
                var item = args.InvokedItem as NavigationViewItem;
                switch (tag)
                {
                    case "Index":
                        contentframe.Navigate(typeof(Index));
                        break;
                    case "Section":
                        contentframe.Navigate(typeof(Section));
                        break;
                    case "Discover":
                        contentframe.Navigate(typeof(Discover));
                        break;
                    case "Favorite":
                        var param_1 = new Dictionary<string, string>()
                        {
                            {"name","默认收藏夹" },
                            { "gid","0"},
                            {"mode","favorite"}
                        };
                        contentframe.Navigate(typeof(Favorite), param_1);
                        break;
                    case "Setting":
                        contentframe.Navigate(typeof(Setting));
                        break;
                    case "Message":
                        var param = new MessageNavigationInfo {IsFromProfile = false };
                        contentframe.Navigate(typeof(Message), param);
                        break;
                    case "Focus":
                        contentframe.Navigate(typeof(Focus));
                        break;
                    default:
                        if (tag.All(char.IsDigit))
                        {
                            try
                            {
                                contentframe.Navigate(typeof(Board),int.Parse(tag));
                            }
                            catch
                            {

                            }
                        }
                        
                        break;
                }
            }
        }

        private void PaneExpand_Click(object sender, RoutedEventArgs e)
        {
            Navi.IsPaneOpen=!Navi.IsPaneOpen;
        }

        private void PaneExpand_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            AnimatedIcon.SetState(NaviIcon, "PointerOver");
        }

        private void PaneExpand_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            AnimatedIcon.SetState(NaviIcon, "Normal");
        }
      

        private void Me_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            AnimateButton(PortScaleTransform, 0.95, 0.95);
        }

        private void Me_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            AnimateButton(PortScaleTransform, 1, 1);
        }
        private void AnimateButton(ScaleTransform transform, double X, double Y)
        {
            var storyboard = new Storyboard();

            var animationX = new DoubleAnimation
            {
                To = X,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animationX, transform);
            Storyboard.SetTargetProperty(animationX, "ScaleX");

            var animationY = new DoubleAnimation
            {
                To = Y,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animationY, transform);
            Storyboard.SetTargetProperty(animationY, "ScaleY");

            storyboard.Children.Add(animationX);
            storyboard.Children.Add(animationY);
            storyboard.Begin();
        }

        private void Me_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            AnimateButton(PortScaleTransform, 0.85, 0.85);
        }

        private void Me_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            AnimateButton(PortScaleTransform, 1, 1);
        }

        private void Me_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            AnimateButton(PortScaleTransform, 1, 1);
        }

        private void Me_Click(object sender, RoutedEventArgs e)
        {
            LoadProfile();
            var param = new ProfileNavigationInfo { IsMe = true };
            contentframe.Navigate(typeof(Profile), param);
        }

        private async void CCZone_Click(object sender, RoutedEventArgs e)
        {
            var m = sender as MenuFlyoutItem;
            if (m!= null)
            {
                var _tag = m.Tag;
                if(_tag is string tag)
                {
                    switch (tag)
                    {
                        case "0":
                            //网页端OpenID未注册权限，不支持抽卡
                            if (ValidationHelper.GetValue(Set, "IsActive") != "1")
                            {
                                Flower.Play("\uEA39", "当前登录方式不支持抽卡");
                                return;
                            }
                            contentframe.Navigate(typeof(Game));
                            break;
                        case "1":
                            ForumStat.XamlRoot = RootGrid.XamlRoot;
                            ForumStat.IsOpen = true;
                            await LoadForumStat();
                            break;
                        
                    }
                        
                }
            }
            
        }

        private async Task LoadForumStat()
        {
            //由于此方法只需要缓存文件中极小的一部分，使用jsonreader读取整个文件会浪费内存。
            string jpath = System.IO.Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "IndexCache.json");
            if (File.Exists(jpath))
            {
                var file = await StorageFile.GetFileFromPathAsync(jpath);

                await using var stream = await file.OpenStreamForReadAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                var root = doc.RootElement;
                var stats = new
                {
                    todayCount = ValidationHelper.GetPropertyAsInt(root, "todayCount"),
                    todayTopicCount = ValidationHelper.GetPropertyAsInt(root, "todayTopicCount"),
                    topicCount = ValidationHelper.GetPropertyAsInt(root, "topicCount"),
                    userCount = ValidationHelper.GetPropertyAsInt(root, "userCount"),
                    onlineUserCount = ValidationHelper.GetPropertyAsInt(root, "onlineUserCount"),
                    postCount = ValidationHelper.GetPropertyAsInt(root, "postCount"),
                    lastUserName = ValidationHelper.GetPropertyAsString(root, "lastUserName")
                };

                // 直接使用提取的数据
                welcome.Text = $"欢迎新用户 {stats.lastUserName}";
                ForumStatList.ItemsSource = new List<CardStatInfoPair>
    {
        new() { StatItem = "今日帖数", Value = stats.todayCount },
        new() { StatItem = "今日主题数", Value = stats.todayTopicCount },
        new() { StatItem = "全站帖数", Value = stats.postCount },
        new() { StatItem = "全站话题", Value = stats.topicCount },
        new() { StatItem = "在线用户", Value = stats.onlineUserCount },
        new() { StatItem = "全站用户", Value = stats.userCount }
    };
            }
            
        }

        private void ForumStat_Unloaded(object sender, RoutedEventArgs e)
        {
            welcome.Text = "";
            ForumStatList.ItemsSource = null;
        }
    }
    public class CategoryBase { }

    public class NavigationItem : CategoryBase,INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public FluentIcons.Common.Symbol IconSymbol { get; set; }
        public string Tag { get; set; }= string.Empty;
        public bool IsPinned { get; set; } // 是否固定

        public bool IsEditable {  get; set; }
        

        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class NavigationGroup:CategoryBase
    {
        public string Name { get; set; } 
        public bool IsEditable { get; set; } 
    }

    
    
}
