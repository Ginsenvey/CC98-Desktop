using CC98.Kernel;
using CC98.Kernel.Authorize;
using CC98.Kernel.Network;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Helpers;
using CC98.Views;
using DevWinUI;
using Duende.IdentityModel.OidcClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

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
    public Window LoginWindow { get; set; }

    public LoginService LoginService =>Current.GetService<LoginService>();
    public IVpnService Vpn =>Current.GetService<IVpnService>();
    public MirrorService mirrorService => Current.GetService<MirrorService>();

    /// <summary>
    /// 内存缓存服务。
    /// </summary>
    public IMemoryCache MemoryCache { get; } = new MemoryCache(new MemoryCacheOptions());

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

        var e = AppInstance.GetActivatedEventArgs();
        if (e is ProtocolActivatedEventArgs protocol)
        {
            var redirectUrl = protocol.Uri.ToString();
            bool success = await AuthFromProtocol(redirectUrl);
            if (success)
            {
                AppSettings.Current.IsActive = true;
                AppSettings.Current.ActiveMode = (int)ActiveMode.OpenId;
                AppMainWindow = new MainWindow();
                AppMainWindow.Activate();
            }
            return;
        }

        bool isActive = AppSettings.Current.IsActive;
        if (!isActive)
        {
            LoginWindow = new LoginWindow();
            LoginWindow.Activate();
        }
        else
        {
            InitializeAndLaunch();
        }
    }
    private void LaunchAppMainWindow()
    {
        AppMainWindow = new MainWindow();
        AppMainWindow.Activate();
    }

    private void TokenService_AuthenticationFailed(object? sender, AuthenticationFailedEventArgs e)
    {
        AppSettings.Current.IsActive = false;
        ShowAppNotification("需要重新登录", UserFriendlyExceptionMessage(e.Reason), e.Exception?.Message ?? "");
        try
        {
            AppMainWindow.Close();
            LoginWindow = new LoginWindow();
            LoginWindow.Activate();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    private static string UserFriendlyExceptionMessage(string reason)
    {
        return reason switch
        {
            "invalid_grant" => "令牌已过期",
            "invalid_client" => "客户端凭证错误,客户端配置可能被98官方改动",
            _ => reason
        };
    }

    #endregion

    #region 依赖注入
   
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
                //Cookie容器
                var cookieContainer = new CookieContainer();
                services.AddSingleton(cookieContainer);
                //VPN服务和委托处理器
                services.AddSingleton<ICookieService, CookieService>();
                services.AddSingleton<IVpnService, VpnService>();
                services.AddTransient<VpnMessageHandler>();
                //Token服务
                services.AddSingleton<ITokenService, TokenService>();
                //HTTP
                services.AddHttpClient("VpnClient",client=>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.BaseAddress = new Uri("https://webvpn.zju.edu.cn");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
                })
                .ConfigurePrimaryHttpMessageHandler(() => 
                {
                    return new HttpClientHandler
                    {
                        Proxy=new WebProxy("127.0.0.1:9000"),
                        CookieContainer = cookieContainer,
                    };
                })
                .AddStandardResilienceHandler();

                services.AddHttpClient("ForumClient", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .AddHttpMessageHandler<VpnMessageHandler>()
                .AddStandardResilienceHandler();

                services.AddHttpClient("IdentityServer", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .AddStandardResilienceHandler();

                services.AddHttpClient("MirrorClient", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .AddHttpMessageHandler<VpnMessageHandler>()
                .AddStandardResilienceHandler();

                //论坛登录服务
                services.AddSingleton<LoginService>();
                //主业务服务
                services.AddSingleton<ApiService>();
                //镜像站服务
                services.AddSingleton<MirrorService>();
            })
            .Build();
        var tokenService = GetService<ITokenService>();
        tokenService.AuthenticationFailed += TokenService_AuthenticationFailed;
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

    /// <summary>
    /// 完成回调过程
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private async Task<bool> AuthFromProtocol(string callback)
    {
        var clientState = PasswordManager.RetrievePassword("OpenIdState");
        PasswordManager.RemovePassword("OpenIdState");
    
        var verifier = PasswordManager.RetrievePassword("OpenIdCodeVerifier");
        PasswordManager.RemovePassword("OpenIdCodeVerifier");

        if(string.IsNullOrEmpty(clientState)||string.IsNullOrEmpty(verifier))
        {
            throw new Exception("缺少必要的认证参数");
        }

        try
        {
            var res = await LoginService.LoginWithCodeAsync(callback, verifier, clientState);
            if (res == null)
            {
                ShowAppNotification("登录失败", "请检查网络后重试");
                return false;
            }
            if (res.IsError)
            {
                ShowAppNotification("登录失败", res.ErrorDescription ?? res.Error?? "未知错误");
                return false;
            }
            ShowAppNotification("登录成功", $"欢迎回家~{res.User.Identity?.Name}前辈");
            return true;
        }
        catch (Exception ex)
        {
            ShowAppNotification("登录失败", "发生异常", ex.Message); 
            return false;
        }
    }



    #endregion

    #region 网络
    /// <summary>
    /// 必须有如下结果中的一种：启动某个窗口，或者显示通知并退出应用。
    /// </summary>
    private async void InitializeAndLaunch()
    {
        var networkStatus = await mirrorService.CheckNetworkAsync();
        switch (networkStatus) 
        { 
            case NetworkStatus.InCampus:
                LaunchAppMainWindow();
                break;
            case NetworkStatus.NotInCampus:
                var isVpnUsable = PasswordManager.PasswordExists("VpnUserName") && PasswordManager.PasswordExists("VpnPassWord");
                if (isVpnUsable)
                {
                    //尝试检验有效性
                    Vpn.IsEnabled = true;
                    var newStatus = await mirrorService.CheckNetworkAsync();
                    //这时候启用了VPN，如果无法连接内网，有可能是套餐到期，也有可能是凭据过期
                    if (newStatus == NetworkStatus.InCampus)
                    {
                        LaunchAppMainWindow();
                    }
                    //处理cookie过期的情况
                    else if (newStatus == NetworkStatus.VpnExpired)
                    {
                        await ReloginVpn();
                    }
                    else
                    {
                        ShowAppNotification("出错", newStatus.ToString());
                        Exit();
                    }
                }
                else
                {
                    ShowAppNotification("出错", "VPN凭据不完整", "请重新登录");
                    LoginWindow = new LoginWindow(needLoginVpn: true);
                    LoginWindow.Activate();
                }
                break;
            case NetworkStatus.MirrorError:
                ShowAppNotification("出错", "连接镜像站失败");
                Exit();
                break;
            case NetworkStatus.UnknownError:
                ShowAppNotification("出错", "IP可能被镜像站拦截");
                Exit();
                break;
            case NetworkStatus.NoConnection:
                ShowAppNotification("出错", "无互联网连接");
                Exit();
                break;
        }
        
    }

    

    private async Task ReloginVpn()
    {
        var userName = PasswordManager.RetrievePassword("VpnUserName");
        var password = PasswordManager.RetrievePassword("VpnPassWord");
        var res = await Vpn.LoginAsync(userName, password);   
        if(res==null)
        {
            ShowAppNotification("VPN登录失败", "请检查网络后重启应用");
            return;
        }
        if (res.Status == VpnLoginStatus.Success) return;
        if (res.Status == VpnLoginStatus.NeedConfirm)
        {
            await VpnConfirmAsync();
        }
        if (res.Status == VpnLoginStatus.NeedCaptcha || res.Status == VpnLoginStatus.Fail)
        {
            //密码有问题，清理旧密码，要求重新登录
            PasswordManager.RemovePassword("VpnUserName");
            PasswordManager.RemovePassword("VpnPassWord");
            ShowAppNotification("VPN登录失败", "需要重新登录","VPN凭据错误或者套餐到期");
            LoginWindow = new LoginWindow(needLoginVpn: true);
            LoginWindow.Activate();
            return;
        }
    }
    private async Task VpnConfirmAsync()
    {
        var confirmResult = await Vpn.ConfirmAsync();
        if (confirmResult == null || !confirmResult.Success)
        {
            //关闭应用，让用户重启
            ShowAppNotification("VPN顶号失败", "请重新启动应用");
            Current.Exit();
            return;
        }
        else
        {
            LaunchAppMainWindow();
        }
    }

    #endregion

    #region 其他


    public static string GetValue(NameValueCollection collection, string key)
    {
        if (collection.AllKeys.Contains(key))
        {
            var value = collection[key];
            if (value is string str) return str;
        }

        return "0";
    }


    private static void ShowAppNotification(string title, string subtitle = "", string message = "")
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