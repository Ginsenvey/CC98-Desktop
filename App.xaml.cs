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

    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;

    public LoginService LoginService =>Current.GetService<LoginService>();
    public IVpnService Vpn =>Current.GetService<IVpnService>();
    
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
            AppMainWindow = new MainWindow();
            AppMainWindow.Activate();
        }
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

                //论坛登录服务
                services.AddSingleton<LoginService>();
                //主业务服务
                services.AddSingleton<ApiService>();
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
            
            if (false)
            {
                //打开VPN配置设置
              
                
                return;
            }
            //检测是否已初始化Ticket。若已初始化，使用并检查有效性。无效则重连。未初始化是出错的情况。
            if (!PasswordManager.PasswordExists("Ticket") || !PasswordManager.PasswordExists("Route"))
            {
                //报错
               
                ShowAppNotification("VPN凭据不完整");
                return;
            }
            //Todo
            if (false)
            {
                //报错
              
                ShowAppNotification("VPN凭据不完整");
                return;
            }
    
            var newStatus = await Vpn.CheckNetworkAsync(true);
        
            if (newStatus == NetworkStatus.ByVpn)
            {
                Vpn.IsLoggedIn = true;
                Vpn.IsEnabled = true;
                await StartUp();
                //启动
                return;
            }
            var success = await ReloginVpn();

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
                ShowAppNotification("无法连接WebVPN", "账户欠费或者密码不正确", "请重新配置凭据");
            }
        }
        if (networkStatus == NetworkStatus.MirrorError)
        {
            ShowAppNotification("出错", "连接镜像站失败", "日志已记录");
        }
        if (networkStatus == NetworkStatus.UnknownError)
        {
            ShowAppNotification("出错", "IP可能被镜像站拦截", "日志已记录");
        }
        if (networkStatus == NetworkStatus.NoConnection)
        {
            ShowAppNotification("出错", "无互联网连接", "日志已记录");
        }
        return;
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
                ShowAppNotification("顶号失败");
                return false;
            }
        }
        if (res.Status == VpnLoginStatus.NeedCaptcha || res.Status == VpnLoginStatus.Fail)
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