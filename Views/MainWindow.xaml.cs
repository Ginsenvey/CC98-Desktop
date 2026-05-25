using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using DevWinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.BadgeNotifications;
using CC98.Services.Extensions;
using CC98.Services.Helpers;
using CC98.Kernel.Authorize;
using Microsoft.UI.Xaml.Controls;
using Symbol = FluentIcons.Common.Symbol;
using CC98.Kernel.Network;

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
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;

    public ObservableCollection<CategoryBase> MenuItems { get; } = [];
    public ObservableCollection<CategoryBase> FooterMenuItems { get; } = [];
    public Frame RootFrame => ContentFrame; //用于在嵌套的Frame中导航
    public NavigationView NavigationView => Navi;
    public int UnreadCount { get; set; }

    private DispatcherTimer? SyncTimer { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Changed += AppWindow_Changed;
        App.ThemeChanged += OnAppThemeChanged;
    }
    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        //在最小化时隐藏到托盘
        if (args.DidPresenterChange && AppWindow.Presenter is OverlappedPresenter presenter && presenter.State == OverlappedPresenterState.Minimized)
            AppWindow.Hide();
    }
    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        //设置窗口状态
        SetWindowState();
        //加载自定义设置
        LoadSettings();
        //加载内容
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
    }

    private void PrepareContent()
    {
        CheckLoginStatus();
        LoadPortrait();
        LoadMenuItem();
        InitializeTimer();
    }

    private async void LoadPortrait()
    {
        var portraitUrl = AppSettings.Current.Portrait;
        if (string.IsNullOrEmpty(portraitUrl)) return;

        var profileUrl = ApiEndpoints.User.UserProfile(true, 0);
        var profileResult = await RequestSender.Fetch<UserInfo>(profileUrl);
        if (!profileResult.IsSuccess) return;
        var data = profileResult.Data;
        portraitUrl = data.PortraitUrl;
        AppSettings.Current.Portrait = data.PortraitUrl;

        MyPicture.Src = portraitUrl;
    }

    private void OnNavigationItemAdded(NavigationItem item)
    {
        var flag = true;
        foreach (var menu in MenuItems)
            if (menu is NavigationItem i)
                if (i.Tag == item.Tag)
                    flag = false;

        if (flag) MenuItems.Add(item);
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
        var result = await RequestSender.Delete(url);
        if (!result.IsSuccess)
        {
            //
            return;
        }
        var customBoards = ValidationHelper.GetValue(Set, "CustomBoards");
        if (customBoards != "0")
        {
            var boardinfo = SerializationHelper.TryDeserialize<Dictionary<int, string>>(customBoards);
            boardinfo?.Remove(boardId);
        }

        var item = MenuItems.OfType<NavigationItem>().First(g => g.Tag == boardId.ToString());
        MenuItems.Remove(item);
    }

    private async void GetFocusBoards() //同步客户端和在线关注版块的信息
    {
        var customBoards = ValidationHelper.GetValue(Set, "CustomBoards");
        if (customBoards != "0")
            Memory = SerializationHelper.TryDeserialize<Dictionary<int, string>>(customBoards) ?? [];
        else
            Set.Values["CustomBoards"] = "0";
        //初始化本地缓存
        var profileUrl = ApiEndpoints.User.UserProfile(true, 0);
        var profileResult = await RequestSender.Fetch<UserInfo>(profileUrl);
        if (!profileResult.IsSuccess || profileResult.Data == null)
        {
            if (Memory != null)
                foreach (var b in Memory)
                    MenuItems.Add(new NavigationItem
                    {
                        Name = b.Value, IconSymbol = BoardIconHelper.GetSymbol(b.Key, b.Value), Tag = b.Key.ToString(),
                        IsEditable = true
                    });

            return;
        }

        var data = profileResult.Data;
        var boards = data.CustomBoards;
        foreach (var board in boards) AddBoards(board);
    }

    private async void AddBoards(int boardId)
    {
        //此方法将检测本地是否已存储板块，没有则添加。无论本地是否已经存在，都会加载到导航栏。
        //先判断本地存储是否有此板块
        if (!Memory.ContainsKey(boardId))
        {
            var boardDataUrl = ApiEndpoints.Board.BoardInfo(boardId);
            var boardDataResult = await RequestSender.Fetch<BoardData>(boardDataUrl);
            if (!boardDataResult.IsSuccess || boardDataResult.Data == null) return;
            var data = boardDataResult.Data;
            Memory.Add(boardId, data.Name);
            MenuItems.Add(new NavigationItem
            {
                Name = data.Name, IconSymbol = BoardIconHelper.GetSymbol(boardId, data.Name), Tag = boardId.ToString(),
                IsEditable = true
            });
            var boardjsontext = SerializationHelper.TrySerialize(Memory);
            Set.Values["CustomBoards"] = boardjsontext;
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

    private async void LoadIndex()
    {
        var tag = ValidationHelper.GetValue(Set, "TitlePage");
        if (tag != "0")
        {
            switch (tag)
            {
                case "1":

                    var index = await FetchIndex();
                    if (!index) Flower.Play(FlowStatus.Fail, "刷新首页失败");
                    ContentFrame.Navigate(typeof(IndexPage));
                    break;

                case "2":
                    var param = new ProfileNavigationInfo { IsMe = true };
                    ContentFrame.Navigate(typeof(ProfilePage), param);
                    break;
                case "3":
                    ContentFrame.Navigate(typeof(DiscoverPage));
                    break;
            }
        }
        else
        {
            Set.Values["TitlePage"] = "1";
            var index = await FetchIndex();
            if (index) ContentFrame.Navigate(typeof(IndexPage));
        }
    }

    private void InitializeTimer()
    {
        SyncTimer = new()
        {
            Interval = TimeSpan.FromSeconds(240)
        };
        SyncTimer.Tick += DispatcherTimer_Tick;
        SyncTimer.Start();
    }


    private async void DispatcherTimer_Tick(object? sender, object e)
    {
        await FetchIndex();
        RefreshMessage();
    }

    private async Task<bool> FetchIndex()
    {
        var url = ApiEndpoints.Forum.Index;
        return await IndexDataService.RefreshFromApiAsync(url);
    }

    

    private void OnAppThemeChanged(ElementTheme theme)
    {
        // 更新 RootGrid 的主题
        RootGrid.RequestedTheme = theme;
    }


    private async void CheckLoginStatus()
    {
        var access = PasswordManager.RetrievePassword("Access");
        if (string.IsNullOrEmpty(access))
        {
            ShowTips("登录凭据未保存", "请重新登录");
            //应该只清除CC98相关的凭据
            Logout();
            return;
        }

        //VpnService.HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", access);
        var url = ApiEndpoints.User.UnreadMessage();
        var result = await RequestSender.Fetch<UnreadMessageInfo>(url);
        if (!result.IsSuccess || result.Data == null)
        {
            //
            if (result.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                //登录失效
                ShowTips("登录状态已过期", "请重新登录");
                Logout();
            }

            return;
        }

        var data = result.Data;
        GlobalService.AtCount = data.AtCount;
        GlobalService.ReplyCount = data.ReplyCount;
        GlobalService.SystemCount = data.SystemCount;
        GlobalService.MessageCount = data.MessageCount;
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
            await App.Logger.WriteAsync("MainWindow", "刷新未读消息失败", $"{result.StatusCode}:{result.Message}");
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
        var favoritesInfoResult = await RequestSender.Fetch<FavoritesInfo>(favoritesInfoUrl);
        if (!favoritesInfoResult.IsSuccess || favoritesInfoResult.Data == null)
            //
            return false;
        var data = favoritesInfoResult.Data;
        var groups = data.FavoriteTopicGroups;
        if (groups.Count > 0)
        {
            //临时存储收藏夹列表
            var favoJson = SerializationHelper.TrySerialize(groups);
            Set.Values["Favorites"] = favoJson;
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
        if (args.InvokedItemContainer?.Tag is string tag)
        {
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
                    var param = new MessageNavigationInfo { HasTarget = false };
                    ContentFrame.Navigate(typeof(MessagePage), param);
                    break;
                case "Focus":
                    ContentFrame.Navigate(typeof(FocusPage));
                    break;
                default:
                    if (tag.All(char.IsDigit))
                        try
                        {
                            ContentFrame.Navigate(typeof(BoardPage), int.Parse(tag));
                        }
                        catch
                        {
                        }

                    break;
            }
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
        GlobalService.ShouldReplaceNavigationArgs =
            e.NavigationMode == NavigationMode.Back && _rules.Contains((fromPage, toPage));
    }


    #region 辅助方法

    private void Logout()
    {
        //清除凭据
        PasswordManager.ClearAllPasswords("Access");
        PasswordManager.ClearAllPasswords("Refresh");
        Set.Values["Portrait"] = "0";
        Set.Values["IsActive"] = "0";
        //退出，重启
        AppInstance.Restart("");
    }

    private void ShowTips(string title, string description)
    {
        var notification = new AppNotificationBuilder()
            .AddText(title)
            .AddText(description)
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    #endregion

    
}