using CC98.Kernel;
using CC98.Kernel.Authorize;
using CC98.Kernel.Network;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CC98.Services.Helpers;
using CSharpMath;
using DevWinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.BadgeNotifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using Symbol = FluentIcons.Common.Symbol;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly HashSet<(Type from, Type to)> _rules =
    [
        (typeof(SketchPage), typeof(TopicPage)),
        (typeof(TopicPage), typeof(MessagePage)),
        (typeof(TopicPage), typeof(FocusPage)),
        (typeof(ProfilePage), typeof(FollowPage))
    ];

    public ObservableCollection<string> Collections = [];
    public GlobalService GlobalService = GlobalService.Instance;
    //需要迁移到新的存取关注版面的方式
    public Dictionary<int, string> Memory = [];

    public ObservableCollection<CategoryBase> MenuItems { get; } = [];
    public ObservableCollection<CategoryBase> FooterMenuItems { get; } = [];
    public Frame RootFrame => ContentFrame; //用于在嵌套的Frame中导航
    public NavigationView NavigationView => Navi;
    public int UnreadCount { get; set; }
    public ApiService ApiService = App.Current.GetService<ApiService>();
    private DispatcherTimer? SyncTimer { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        App.ThemeChanged += OnAppThemeChanged;
        //设置窗口状态
        SetWindowState();
        //加载自定义设置
        LoadSettings();
        PrepareContent();
    }
    

    private void SetWindowState()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(UserArea);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cc98.ico");
        AppWindow.SetIcon(iconPath);
        AppWindow.SetTaskbarIcon(iconPath);
        Messenger.Instance.NavigationItemAdded += OnNavigationItemAdded;
    }
    

    private void LoadSettings()
    {
        int effect = AppSettings.Current.Effect;
        SystemBackdrop = effect switch
        {
            0 => new MicaSystemBackdrop(),
            1 => new MicaSystemBackdrop(MicaKind.BaseAlt),
            2 => new AcrylicSystemBackdrop(),
            3 => new AcrylicSystemBackdrop(DesktopAcrylicKind.Thin),
            4 => null,
            _ => new MicaSystemBackdrop()
        };

        int theme = AppSettings.Current.Theme;
        RootGrid.RequestedTheme = theme switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };


        if (string.IsNullOrEmpty(AppSettings.Current.ThemePicture)) return;
        var themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Themes");
        var files = Directory.GetFiles(themesPath, "*.jpg", SearchOption.AllDirectories);
        var file = files[0];
        //加载到FlipView
        AppSettings.Current.IsVpnEnabled = false;
    }

    private async void PrepareContent()
    {
        LoadMenuItem();
        await LoadIndex();
        await GetFocusBoards();
        await RefreshMessage();
        await LoadPortrait();
        await GetFavorites();
        InitializeTimer();
    }

    private async Task LoadPortrait()
    {
        var portraitUrl = AppSettings.Current.Portrait;
        var userId = AppSettings.Current.UserId;

        if (string.IsNullOrEmpty(portraitUrl) || userId == 0)
        {
            var profileUrl = ApiEndpoints.User.UserProfile(true);
            var profileResult = await ApiService.Fetch<UserInfo>(profileUrl);
            if (!profileResult.IsSuccess) return;
            var data = profileResult.Data;
            AppSettings.Current.Portrait = data.PortraitUrl;
            AppSettings.Current.UserId = data.Id;
        }
    }
   
    private void OnNavigationItemAdded(NavigationItem item)
    {
        //检查导航栏中是否已经存在相同Tag的项，如果存在则不添加
        if (MenuItems.OfType<NavigationItem>().Select(i => i.Tag).Contains(item.Tag))return;
        MenuItems.Add(item);
    }

    private void LoadMenuItem()
    {
        var favorite = new NavigationGroup { Name = "集锦", IsEditable = false };
        var pinnedGroup = new NavigationGroup { Name = "关注", IsEditable = true };
        MenuItems.Add(new NavigationItem
            { Name = "今日话题", IconSymbol = Symbol.Grid, Tag = "Index", IsEditable = false });
        MenuItems.Add(new NavigationItem
            { Name = "全部版面", IconSymbol = Symbol.Board, Tag = "Section", IsEditable = false });
        MenuItems.Add(new NavigationItem
            { Name = "新帖", IconSymbol = Symbol.DesignIdeas, Tag = "Discover", IsEditable = false });
        MenuItems.Add(favorite);
        MenuItems.Add(new NavigationItem { Name = "动态", IconSymbol = Symbol.Home, Tag = "Focus", IsEditable = false });
        MenuItems.Add(new NavigationItem
            { Name = "收藏集", IconSymbol = Symbol.StarLineHorizontal3, Tag = "Favorite", IsEditable = false });
        MenuItems.Add(pinnedGroup);
        FooterMenuItems.Add(new NavigationItem
            { Name = "消息", IconSymbol = Symbol.MailRead, Tag = "Message", IsEditable = false });
        FooterMenuItems.Add(new NavigationItem
            { Name = "设置", IconSymbol = Symbol.StarSettings, Tag = "Setting", IsEditable = false });
    }

    private async void PinOff_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as MenuFlyoutItem)?.Tag is not int boardId) return;
        var url = ApiEndpoints.Board.EditFocusBoards(boardId);
        var result = await ApiService.Delete(url);
        if (!result.IsSuccess)
        {
            //
            return;
        }
        var customBoards = AppSettings.Current.CustomBoards;
        if (customBoards != "")
        {
            var boardInfo = SerializationHelper.TryDeserialize<Dictionary<int, string>>(customBoards);
            boardInfo?.Remove(boardId);
        }

        var item = MenuItems.OfType<NavigationItem>().First(g => g.Tag == boardId.ToString());
        MenuItems.Remove(item);
    }

    private async Task GetFocusBoards() //同步客户端和在线关注版块的信息
    {
        var customBoards = AppSettings.Current.CustomBoards;
        if (string.IsNullOrEmpty(customBoards))
        {
            Memory = [];
        }
        else
        {
            Memory = SerializationHelper.TryDeserialize<Dictionary<int, string>>(customBoards) ?? [];
        }
        //初始化本地缓存
        var profileUrl = ApiEndpoints.User.UserProfile(true, 0);
        var profileResult = await ApiService.Fetch<UserInfo>(profileUrl);
        if (!profileResult.IsSuccess || profileResult.Data == null)
        {
            MenuItems.AddRange(Memory.Select(board => new NavigationItem
            {
                Name = board.Value,
                IconSymbol = BoardIconHelper.GetSymbol(board.Key, board.Value),
                Tag = board.Key.ToString(),
                IsEditable = true
            }));
            
            return;
        }

        var data = profileResult.Data;
        var boards = data.CustomBoards;
        foreach (var board in boards) await AddBoards(board);
    }

    private async Task AddBoards(int boardId)
    {
        //此方法将检测本地是否已存储板块，没有则添加。无论本地是否已经存在，都会加载到导航栏。
        //先判断本地存储是否有此板块
        if (!Memory.ContainsKey(boardId))
        {
            var boardDataUrl = ApiEndpoints.Board.BoardInfo(boardId);
            var boardDataResult = await ApiService.Fetch<BoardData>(boardDataUrl);
            if (!boardDataResult.IsSuccess || boardDataResult.Data == null) return;
            var data = boardDataResult.Data;
            Memory.Add(boardId, data.Name);
            MenuItems.Add(new NavigationItem
            {
                Name = data.Name, IconSymbol = BoardIconHelper.GetSymbol(boardId, data.Name), Tag = boardId.ToString(),
                IsEditable = true
            });
            var boardjsontext = SerializationHelper.TrySerialize(Memory);
            AppSettings.Current.CustomBoards = boardjsontext;
        }
        else
        {
            //如果本地存储有此板块，则直接添加
            MenuItems.Add(new NavigationItem
            {
                Name = Memory[boardId], IconSymbol = BoardIconHelper.GetSymbol(boardId, Memory[boardId]),
                Tag = boardId.ToString(), IsEditable = true
            });
        }
    }

    private async Task LoadIndex()
    {
        var index = AppSettings.Current.TitlePage;
        switch (index)
        {
            case 1:
                var param = new ProfileNavigationInfo { IsMe = true };
                ContentFrame.Navigate(typeof(ProfilePage), param);
                break;
            case 2:
                ContentFrame.Navigate(typeof(DiscoverPage));
                break;
            default:
                ContentFrame.Navigate(typeof(IndexPage));
                await FetchIndex();
                break;
        }
      
    }

    private void InitializeTimer()
    {
        SyncTimer = new()
        {
            Interval = TimeSpan.FromSeconds(240)
        };
        SyncTimer.Tick += async (s, e) => 
        {
            await FetchIndex();
            await RefreshMessage();
        };
        SyncTimer.Start();
    }


    private static async Task<bool> FetchIndex()
    {
        var url = ApiEndpoints.Forum.Index;
        return await IndexDataService.Instance.RefreshFromApiAsync(url);
    }

    

    private void OnAppThemeChanged(ElementTheme theme)
    {
        // 更新 RootGrid 的主题
        RootGrid.RequestedTheme = theme;
    }



    private async Task RefreshMessage()
    {
        var url = ApiEndpoints.User.UnreadMessage();
        var result = await ApiService.Fetch<UnreadMessageInfo>(url);
        if (!result.IsSuccess || result.Data == null)
        {
            //await App.Logger.WriteAsync("MainWindow", "刷新未读消息失败", $"{result.StatusCode}:{result.Message}");
            return;
        }

        var data = result.Data;
        GlobalService.AtCount = data.AtCount;
        GlobalService.ReplyCount = data.ReplyCount;
        GlobalService.SystemCount = data.SystemCount;
        GlobalService.MessageCount = data.MessageCount;
        UnreadCount = data.MessageCount + data.AtCount + data.ReplyCount + data.SystemCount;
        BadgeNotificationManager.Current.SetBadgeAsCount((uint)UnreadCount);
    }


    private async Task<bool> GetFavorites()
    {
        var favoritesInfoUrl = ApiEndpoints.User.FavoritesList();
        var favoritesInfoResult = await ApiService.Fetch<FavoritesInfo>(favoritesInfoUrl);
        if (!favoritesInfoResult.IsSuccess || favoritesInfoResult.Data == null)
            //
            return false;
        var data = favoritesInfoResult.Data;
        var groups = data.FavoriteTopicGroups;
        if (groups.IsNonEmpty())
        {
            //存储收藏夹列表
            var favoJson = SerializationHelper.TrySerialize(groups);
            AppSettings.Current.FavoriteGroups = favoJson ?? "";
            return true;
        }

        Flower.Play(FlowStatus.Info, "暂无收藏夹");
        return false;
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
        if (ContentFrame.CanGoBack) ContentFrame.GoBack();
    }

    private void Navi_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is not string tag) return;
        var item = args.InvokedItem as NavigationViewItem;
        switch (tag)
        {
            case "Index":
                ContentFrame.Navigate(typeof(IndexPage));
                break;
            case "Section":
                ContentFrame.Navigate(typeof(SectionPage));
                break;
            case "Discover":
                ContentFrame.Navigate(typeof(DiscoverPage));
                break;
            case "Favorite":
                ContentFrame.Navigate(typeof(FavoritePage));
                break;
            case "Setting":
                ContentFrame.Navigate(typeof(SettingPage));
                break;
            case "Message":
                var param = new ChatNavigationInfo { HasTarget = false };
                ContentFrame.Navigate(typeof(MessagePage), param);
                break;
            case "Focus":
                ContentFrame.Navigate(typeof(FocusPage));
                break;
            default:
                if (tag.All(char.IsDigit))
                {
                    try
                    {
                        ContentFrame.Navigate(typeof(BoardPage), int.Parse(tag));
                    }
                    catch
                    {
                        Flower.Play(FlowStatus.Fail, "无效的版块ID");
                    }
                }
                break;
        }
    }

    private void PaneExpand_Click(object sender, RoutedEventArgs e)
    {
        Navi.IsPaneOpen = !Navi.IsPaneOpen;
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

    private void AnimateButton(ScaleTransform transform, double x, double y)
    {
        var storyboard = new Storyboard();

        var animationX = new DoubleAnimation
        {
            To = x,
            Duration = TimeSpan.FromSeconds(0.2),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animationX, transform);
        Storyboard.SetTargetProperty(animationX, "ScaleX");

        var animationY = new DoubleAnimation
        {
            To = y,
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
        var param = new ProfileNavigationInfo { IsMe = true };
        ContentFrame.Navigate(typeof(ProfilePage), param);
    }
    

    //本方法只控制全局状态记忆类的参量，而不更改导航栈本身的导航参数
    private void ContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (ContentFrame.Content is not Page currentPage) return;
        var fromPage = currentPage.GetType();
        var toPage = e.SourcePageType;
        GlobalService.ShouldReplaceNavigationArgs = e.NavigationMode == NavigationMode.Back && _rules.Contains((fromPage, toPage));
    }

    private async void VpnButton_Click(object sender, RoutedEventArgs e)
    {
        if (VpnButton.IsChecked==false)
        {
            Flower.Play(FlowStatus.Info, "VPN已断开");
        }
        else
        {
            VpnButton.IsChecked = false;
            await CheckVpnStatus();
        }

    }
    private async void VPNConfigSave_Click(object sender, RoutedEventArgs e)
    {
        var userName = VPNUsername.Text;
        var password = VPNPassword.Password;
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            ErrorBox.Text= "请输入完整的VPN凭据";
            return;
        }
        var result= await LoginAsync(userName, password);
        if(result)
        {
            PasswordManager.SavePassword(userName, "VpnUserName");
            PasswordManager.SavePassword(password, "VpnPassWord");
            VpnButton.IsChecked=true;
            AppSettings.Current.IsVpnEnabled=true;
            VPNConfigDialog.Hide();
            Flower.Play(FlowStatus.Success, "已保存凭据并启用VPN");
        }
        else
        {
            ErrorBox.Text = "登录失败";
        }
    }
    private void VPNConfigCancel_Click(object sender, RoutedEventArgs e)
    {
        AppSettings.Current.IsVpnEnabled = false;
        VPNConfigDialog.Hide();
    }
    private void VPNConfigDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        ErrorBox.Text = "";
    }
    /// <summary>
    /// 只负责在VPN登录时调用VPN服务的登录方法，并处理返回结果。不会直接更改UI状态。
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    private async Task<bool> LoginAsync(string userName, string password)
    {
        try
        {
            Debug.WriteLine("开始登录");
            var vpnService = App.Current.GetService<IVpnService>();
            var res = await vpnService.LoginAsync(userName, password);
            if (res == null)
            {
                Debug.WriteLine("返回空");
                return false;
            }
            if (res.Success)
            {
                Debug.WriteLine("登录成功");
                return true;
            }
            else
            {
                if (res.Status == VpnLoginStatus.NeedCaptcha)
                {
                    //验证码
                    Debug.WriteLine("需要验证码");
                    return false;
                }
                else if (res.Status == VpnLoginStatus.NeedConfirm)
                {
                    //确认
                    Debug.WriteLine("正在进行确认");
                    return await VpnConfirmAsync();
                }
                Debug.WriteLine($"状态：{res.Status}");
                return false;
            }
          
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
            //记录异常
        }
    }
    private async Task<bool> VpnConfirmAsync()
    {
        var vpnService = App.Current.GetService<IVpnService>();
        var confirmResult = await vpnService.ConfirmAsync();
        if (confirmResult == null || !confirmResult.Success)
        {
            //可以肯定此时账户密码均正确
            //需要重试
            Flower.Play(FlowStatus.Fail, "VPN确认顶号失败，请重试");
            AppSettings.Current.IsVpnEnabled = false;
            return false;
        }
        else
        {
            VpnButton.IsChecked = true;
            Flower.Play(FlowStatus.Success, "VPN连接成功");
            return true;
        }
    }
    private async Task CheckVpnStatus()
    {
        //没有配置过VPN，或者配置过但是凭据不完整，则需要登录
        var isVpnUsable = PasswordManager.PasswordExists("VpnUserName") && PasswordManager.PasswordExists("VpnPassWord");
        if (!isVpnUsable)
        {
            VPNConfigDialog.XamlRoot = RootGrid.XamlRoot;
            try
            {
                await VPNConfigDialog.ShowAsync();
            }
            catch(Exception ex)
            {
                Flower.Play(FlowStatus.Warning, ex.Message);
            }
            //等待用户登录VPN
        }
        else
        {
            //尝试使用Cookie
            AppSettings.Current.IsVpnEnabled = true;
            var mirrorService = App.Current.GetService<MirrorService>();
            var networkStatus = await mirrorService.CheckNetworkAsync();
            //如果有效，通知连接成功
            if (networkStatus == NetworkStatus.InCampus)
            {
                //
                VpnButton.IsChecked= true;
                Flower.Play(FlowStatus.Success, "VPN连接成功");
            }
            else if (networkStatus == NetworkStatus.NotInCampus)
            {
                await ReloginVpn();
            }
            else
            {
                //其他错误，提示用户
                AppSettings.Current.IsVpnEnabled = false;
                Flower.Play(FlowStatus.Fail, $"VPN连接失败{networkStatus}");
            }
        }
        
    }
    //VPN的登录分成两种情况，一种是首次登录，另一种是凭据过期后的重新登录。此方法处理凭据过期后的重新登录。
    //重新登录是静默的，一旦弹出需要验证码，就认为凭据过期，清除凭据，要求用户重新登录。
    private async Task ReloginVpn()
    {
        var userName = PasswordManager.RetrievePassword("VpnUserName");
        var password = PasswordManager.RetrievePassword("VpnPassWord");
        if(string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            throw new ArgumentNullException("VPNUserCredentials", "VPN用户名或密码为空，无法重新登录。");
        }
        var vpnService = App.Current.GetService<IVpnService>();
        var res = await vpnService.LoginAsync(userName, password);
        if (res == null)
        {
            //请用户重试
            AppSettings.Current.IsVpnEnabled=false;
            Flower.Play(FlowStatus.Fail, "VPN连接失败，请检查网络后重试");
            return;
        }
        
        if (res.Status == VpnLoginStatus.Success)
        {
            //通知连接成功
            VpnButton.IsChecked = true;
            Flower.Play(FlowStatus.Success, "VPN连接成功");
            return;
        }
        if (res.Status == VpnLoginStatus.NeedConfirm)
        {
            await VpnConfirmAsync();
        }
        if (res.Status == VpnLoginStatus.NeedCaptcha || res.Status == VpnLoginStatus.Fail)
        {
            //密码有问题，清理旧密码，要求重新登录
            PasswordManager.RemovePassword("VpnUserName");
            PasswordManager.RemovePassword("VpnPassWord");
            AppSettings.Current.IsVpnEnabled=false;
            Flower.Play(FlowStatus.Fail, "VPN套餐过期或密码已错误，请重新登录");
        }
    }

    
}