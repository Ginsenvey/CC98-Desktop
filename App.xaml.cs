using CC98.Kernel;
using CC98.Kernel.Authorize;
using CC98.Kernel.Network;
using CC98.Services;
using DevWinUI;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using CC98.Services.Helpers;
using Microsoft.Extensions.Caching.Memory;
using NativeMethods = CC98.Services.Helpers.NativeMethods;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Duende.AccessTokenManagement;
using CC98.Objects;
using CC98.Views;
using Duende.IdentityModel.OidcClient;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 获取应用程序的当前实例。
    /// </summary>
    public new static App Current => (App)Application.Current;
    public Window AppMainWindow { get; set; }
    public Window LoginWindow { get; private set; }

    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;

    public LoginService LoginService =>Current.GetService<LoginService>();
    public IVpnService Vpn =>Current.GetService<IVpnService>();
    
    /// <summary>
    /// 内存缓存服务。
    /// </summary>
    public IMemoryCache MemoryCache { get; } = new MemoryCache(new MemoryCacheOptions());


    private static AppLog? _logger;
    public static AppLog Logger => _logger ?? throw new InvalidOperationException("Logger未初始化");
    private SystemTrayIcon? _trayIcon;

    public static event Action<ElementTheme>? ThemeChanged;

    public static void RaiseThemeChanged(ElementTheme theme)
    {
        ThemeChanged?.Invoke(theme);
    }
    public App()
    {
        InitializeComponent();
        RegisterServices();
    }


    #region 应用启动
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        var m = new LoginWindow();
        m.Activate();
        await InitializeAppLog();
        var e = AppInstance.GetActivatedEventArgs();
        if (e.Kind == ActivationKind.Protocol)
        {
            AuthFromOpenId(e);
            return;
        }
    }

    private async Task InitializeAppLog()
    {
        try
        {
            //初始化日志
            _logger = new("CC98");
            await Logger.InitializeAsync();
        }
        catch (Exception ex)
        {
            //弹出
            Debug.WriteLine(ex.Message);
            throw;
        }
        AppDomain.CurrentDomain.UnhandledException += async (s, e) =>
        {
            //await Logger.WriteAsync("App", "发生未处理的异常", e.ExceptionObject.ToString());
        };


    }
    private async Task StartUp()
    {
        try
        {
            //必须在构造函数前加上异常处理
            AppMainWindow = new Views.MainWindow();
            AppMainWindow.Closed += Window_Closed;
            AppMainWindow.Activate();
            DisplayTrayIcon();
        }
        catch (Exception ex)
        {
            await Logger.WriteAsync("App", "主窗口启动出错", ex.Message);
        }

    }

  

    #endregion

    #region 依赖注入
    private const string WebClientId = "9a1fd200-8687-44b1-4c20-08d50a96e5cd";
    private const string DesktopClientId = "d47a2448-779f-42f3-164f-08dd8896bbe5";
    private const string WebClientSecret = "8b53f727-08e2-4509-8857-e34bf92b27f2";
    //必须是实例成员。
    private IHost Host;
    private void RegisterServices()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                //配置文件
                //services.Configure<>;
                services.AddSingleton<AppConfig>();
                //VPN服务和委托处理器
                services.AddSingleton<IVpnService, VpnService>();
                services.AddTransient<VpnMessageHandler>();
                //Token服务
                services.AddSingleton<ITokenService, TokenService>();
                //HTTP
                services.AddHttpClient("VpnClient");
                services.AddHttpClient("ForumClient").AddHttpMessageHandler<VpnMessageHandler>();
                services.AddHttpClient("IdentityServer", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
                //论坛登录服务
                services.AddSingleton<LoginService>();
                //主业务服务
                services.AddSingleton<ApiService>();
            })
            .Build();
    }

    /// <summary>
    /// 公开方法。允许外部获取服务实例。在发布前确保所有服务都已注册。该方法必须通过App.Current.GetService<T>()的形式调用。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetService<T>() where T : notnull
    {
        return Host.Services.GetRequiredService<T>();
    }

    #endregion

    #region 认证
    private async void AuthFromOpenId(IActivatedEventArgs e)
    {
        var protocol = (ProtocolActivatedEventArgs)e;
        var query = System.Web.HttpUtility.ParseQueryString(protocol.Uri.Query);
        var code = ValidationHelper.GetValue(query, "code");
        var iss = ValidationHelper.GetValue(query, "iss");
        var state = ValidationHelper.GetValue(query, "state");
        var sessionState = ValidationHelper.GetValue(query, "session_state");
        if (code == "0" || iss == "0" || state == "0" || sessionState == "0")
        {
            ShowError("登录失败", "回调参数不完整", "请报告开发者");
            
            return;
        }
        var stateToVerify = PasswordManager.RetrievePassword("State");
        PasswordManager.RemovePassword("State");
        if (stateToVerify != state)
        {
            ShowError("警告", "返回验证参数不正确", "你可能重复点击了登录按钮，或当前网络环境有风险。");
        }
        var veri = PasswordManager.RetrievePassword("Verifier");
        PasswordManager.ClearAllPasswords("Verifier");
        if (veri != null)
        {

        }
    }


    
    #endregion

    #region 网络
    private async void InitializeNetwork()
    {
        var networkStatus = await Vpn.CheckNetworkAsync(false);
        if (networkStatus == NetworkStatus.InCampus)
        {
            //启动
            await StartUp();
            return;
        }
        if (networkStatus == NetworkStatus.NotInCampus)//在校外
        {
            await Logger.WriteAsync("App", "初始化网络", "检测VPN可用性");
            if (ValidationHelper.GetValue(Set, "IsVpnUsable") != "1")
            {
                //打开VPN配置设置
                await Logger.WriteAsync("App", "初始化网络", "未配置VPN,跳转登录");
                
                return;
            }
            //检测是否已初始化Ticket。若已初始化，使用并检查有效性。无效则重连。未初始化是出错的情况。
            if (!PasswordManager.PasswordExists("Ticket") || !PasswordManager.PasswordExists("Route"))
            {
                //报错
                await Logger.WriteAsync("App", "初始化网络", "VPN凭据中，有至少一个没有保存");
                ShowError("VPN凭据不完整");
                return;
            }
            if (!InjectTokenFromVault())
            {
                //报错
                await Logger.WriteAsync("App", "初始化网络", "已保存的凭据中，有至少一个内容是空文本");
                ShowError("VPN凭据不完整");
                return;
            }
            await Logger.WriteAsync("App", "初始化网络", "注入已有Cookie成功,启用VPN模式检查网络");
            var newStatus = await Vpn.CheckNetworkAsync(true);
            await Logger.WriteAsync("App", "初始化网络", $"新的网络状态为：{newStatus}");
            if (newStatus == NetworkStatus.ByVpn)
            {
                Vpn.IsLoggedIn = true;
                Vpn.IsEnabled = true;
                await StartUp();
                //启动
                return;
            }
            var success = await ReloginVpn();
            await Logger.WriteAsync("App", "初始化网络", success ? "重连成功" : "重连失败");
            if (success)
            {
                Vpn.IsLoggedIn = true;
                Vpn.IsEnabled = true;
                SaveToken();
                //此时vpn应该可用
                await StartUp();
            }
            else
            {
                //此处存在bug，经此入口重新登录VPN，重启应用，显示VPN未配置
                Set.Values["IsVpnUsable"] = "0";
                ShowError("无法连接WebVPN", "账户欠费或者密码不正确", "请重新配置凭据");
            }
        }
        if (networkStatus == NetworkStatus.MirrorError)
        {
            ShowError("出错", "连接镜像站失败", "日志已记录");
        }
        if (networkStatus == NetworkStatus.UnknownError)
        {
            ShowError("出错", "IP可能被镜像站拦截", "日志已记录");
        }
        if (networkStatus == NetworkStatus.NoConnection)
        {
            ShowError("出错", "无互联网连接", "日志已记录");
        }
        return;
    }

    private bool InjectTokenFromVault()
    {
        //提取环节
        var ticketValue = PasswordManager.RetrievePassword("Ticket");
        var routeValue = PasswordManager.RetrievePassword("Route");
        var ticket = new Cookie("wengine_vpn_ticketwebvpn_zju_edu_cn", ticketValue, "/", "webvpn.zju.edu.cn");
        var route = new Cookie("route", routeValue, "/", "webvpn.zju.edu.cn");
        ticket.HttpOnly = true;
        //注入环节
        if (string.IsNullOrEmpty(ticketValue) || string.IsNullOrEmpty(routeValue))
        {
            return false;
        }
        Vpn.SetCookies(ticket.ToString(), route.ToString());
        return true;
    }

    private async Task<bool> ReloginVpn()
    {
        if (!PasswordManager.PasswordExists("VpnUserName") || !PasswordManager.PasswordExists("VpnPassWord"))
        {
            return false;
        }
        var id = PasswordManager.RetrievePassword("VpnUserName");
        var pass = PasswordManager.RetrievePassword("VpnPassWord");
        var res = await Vpn.LoginAsync(id, pass);   
        if (res.Status == VpnLoginStatus.Success) return true;
        if (res.Status == VpnLoginStatus.NeedConfirm)
        {
            var confirmRes = await Vpn.ConfirmAsync();
            if (confirmRes.Status == VpnLoginStatus.Success)
            {
                //
                return true;
            }
            else
            {
                //报错
                ShowError("顶号失败");
                return false;
            }
        }
        if (res.Status == VpnLoginStatus.NeedCaptcha || res.Status == VpnLoginStatus.Error)
        {
            //报错
            return false;
        }
        return false;
    }
    //Todo:改进cookie的保存形式
    private bool SaveToken()
    {
        throw new NotImplementedException("此方法尚未实现");
    }
    #endregion

    #region 其他


    private void Window_Closed(object sender, WindowEventArgs args)
    {
        if (_trayIcon != null)
        {
            _trayIcon.IsVisible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }



    private void DisplayTrayIcon()
    {
        var icon = WindowHelper.GetWindowIcon(AppMainWindow);
        uint iconId = 9898;

        // 保留引用，避免被 GC 回收
        _trayIcon = new(iconId, icon, "CC98论坛");
        _trayIcon.LeftClick += (s, e) =>
        {
            AppMainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                if (!AppMainWindow.Visible) AppMainWindow.AppWindow.Show();
                AppMainWindow.Activate();
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(AppMainWindow.AppWindow.Id);
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter)
                {
                    if (overlappedPresenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)
                    {
                        overlappedPresenter.Restore();
                    }
                }
            });
        };
        _trayIcon.RightClick += (s, e) =>
        {
            // 使用 WinUI 的 MenuFlyout 并将其赋值给事件参数的 Flyout 属性
            var flyout = new MenuFlyout();
            var titleItem = new MenuFlyoutItem { Text = "CC98", IsEnabled = false, Width = 180 };
            var openItem = new MenuFlyoutItem { Text = "进入论坛", Width = 180, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.Home } };
            openItem.Click += (_, __) =>
            {
                AppMainWindow.DispatcherQueue.TryEnqueue(() => AppMainWindow.Activate());
            };

            var exitItem = new MenuFlyoutItem { Text = "退出", Width = 180, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.ArrowExit } };
            exitItem.Click += (_, __) =>
            {
                AppMainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    AppMainWindow.Close();
                    LoginWindow?.Close();
                });

                // 隐藏并释放托盘对象
                if (_trayIcon != null)
                {
                    _trayIcon.IsVisible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }

                Application.Current.Exit();
            };
            flyout.Items.Add(titleItem);
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(openItem);
            flyout.Items.Add(exitItem);

            // 将 Flyout 交给 DevWinUI 的 SystemTrayIcon 处理显示
            e.Flyout = flyout;
        };

        _trayIcon.IsVisible = true;




    }
    private void ShowError(string title, string subtitle = "", string message = "")
    {
        var notification = new AppNotificationBuilder()
            .AddText(title)
            .AddText(subtitle)
            .AddText(message)
            .BuildNotification();
        AppNotificationManager.Default.Show(notification);
    }

    #endregion
}