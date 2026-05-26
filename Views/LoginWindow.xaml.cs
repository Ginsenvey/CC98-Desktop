using CC98.Kernel;
using CC98.Kernel.Authorize;
using CC98.Kernel.Network;
using CC98.Objects;
using CC98.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;
using Windows.UI.WindowManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     登录窗口。
/// </summary>
public sealed partial class LoginWindow : Window
{
    public int Mode;
    public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
    //TODO：改xaml
    private const string TipText= "如果尚未连接浙江大学内网，请在此处登录WebVPN,或者使用[ZJU Connect](https://github.com/Mythologyli/ZJU-Connect-for-Windows/releases).";
    private const string GuideText= "**在校外登录** \r\n\r\n先配置应用的内建WebVPN,再使用密码登录。\r\n\r\n**忘记密码/无账号？**\r\n\r\n进入[CC98](https://www.cc98.org/logon)官网操作。\r\n\r\n**遇到问题/想要新功能?**\r\n\r\n你可以在微软商店或[开发进度记录楼](https://www.cc98.org/topic/6173309)反馈此问题。\r\n\r\n你也可以克隆本应用仓库，自由修改和编译新的分支。不过，在分发时，应当告知所有的改动。\r\n\r\n**成为开发者**\r\n\r\n本应用使用`WinUI3`,`C#`,`XAML`构建。欢迎所有对.NET生态感兴趣的uu加入本应用的开发，欢迎所有使用者对本应用UI、功能和代码提供建议。";
    
    public LoginService LoginService=App.Current.GetService<LoginService>();
    public IVpnService VpnService => App.Current.GetService<IVpnService>();
    public LoginWindow(int mode)
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(GridTitleBar);
        RootGrid.RequestedTheme = ElementTheme.Light;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cc98.ico");
        AppWindow.SetIcon(iconPath);
        AppWindow.SetTaskbarIcon(iconPath);
        SetWindowState();
        LoadParams(mode);
    }
    /// <summary>
    /// 适应高分屏，设置窗口大小为基于2560分辨率的缩放值，并居中显示。
    /// </summary>
    private void SetWindowState()
    {
        const int baseWidth = 1440;
        const int baseHeight = 920;

        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.SetBorderAndTitleBar(true, true);
        AppWindow.SetPresenter(presenter);

        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        int screenWidth = displayArea.WorkArea.Width;  // 物理分辨率宽度

        // 计算 scale（基于2560基准）
        double scale = screenWidth / 2560.0;
        // 可选：限制范围，避免窗口过小或过大
        scale = Math.Clamp(scale, 0.6, 1.5);
        // 计算最终想要的内容区大小（逻辑像素）
        int desiredClientWidth = (int)(baseWidth * scale);
        int desiredClientHeight = (int)(baseHeight * scale);
        AppWindow.ResizeClient(new SizeInt32(desiredClientWidth, desiredClientHeight));

        //居中
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
        if (area == null) return;
        AppWindow.Move(new((area.Value.Width - AppWindow.Size.Width) / 2,(area.Value.Height - AppWindow.Size.Height) / 2));
    }
    private void LoadParams(int mode)
    {
        Mode = mode;
        if (mode == 0)
        {
        } //普通登录
        else if (mode == 1) //用户已经登录过，只需要添加VPN凭据
        {
           
        }
        else //由于密码更改或者套餐到期，尝试使用原凭据VPN登录失败，需要验证码
        {
            
            var captchaId = ""; //从VpnService处直接调用。
            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var captchaUrl = $"{""}/captcha/{captchaId}.png?reload={timeStamp}";
            
        }
    }

   

    private async Task InitializeNetworkAsync(CancellationToken cancellation=default)
    {
        //检测网络状态，不注入Cookie
        var networkStatus = NetworkStatus.InCampus;
        if (networkStatus == NetworkStatus.InCampus)
        {
            //启动
            //OpenPasswordLoginPane();
            return;
        }

        if (networkStatus == NetworkStatus.NotInCampus) //在校外
        {
            if (ValidationHelper.GetValue(Set, "IsVpnUsable") != "1")
            {
                //打开VPN配置设置
              
                return;
            }

            //检测是否已初始化Ticket。若已初始化，使用并检查有效性。无效则重连。未初始化是出错的情况。
            if (!PasswordManager.PasswordExists("Ticket") || !PasswordManager.PasswordExists("Route"))
            {
                //报错
                Flower.Play(FlowStatus.Fail, "VPN凭据不完整");
                return;
            }

            if (!InjectTokenFromVault())
            {
                //报错
                Flower.Play(FlowStatus.Fail, "VPN凭据不完整");
                return;
            }

            var newStatus = NetworkStatus.InCampus;
            if (newStatus == NetworkStatus.ByVpn)
            {
                
                //OpenPasswordLoginPane();
                //启动
                return;
            }

            var success = await ReloginVpn();
            if (success)
            {
                //VpnService.IsEnabled = true;
                SaveVpnToken();
                //此时vpn应该可用
                //OpenPasswordLoginPane();
            }
        }

        if (networkStatus == NetworkStatus.MirrorError) Flower.Play(FlowStatus.Fail, "连接镜像站失败");
        if (networkStatus == NetworkStatus.UnknownError) Flower.Play(FlowStatus.Fail, "IP被镜像站拦截");
        if (networkStatus == NetworkStatus.NoConnection) Flower.Play(FlowStatus.Fail, "无互联网连接");
    }
    //TODO：rewrite
    private bool SaveVpnToken()
    {
        throw new NotImplementedException();
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
        if (string.IsNullOrEmpty(ticketValue) || string.IsNullOrEmpty(routeValue)) return false;
        
        return true;
    }


    private async Task<bool> ReloginVpn()
    {
        if (!PasswordManager.PasswordExists("VpnUserName") || !PasswordManager.PasswordExists("VpnPassWord"))
            return false;
        var id = PasswordManager.RetrievePassword("VpnUserName");
        var pass = PasswordManager.RetrievePassword("VpnPassWord");
        var res = await VpnService.LoginAsync(id, pass);
        if (res.Status == VpnLoginStatus.Success) return true;
        if (res.Status == VpnLoginStatus.NeedConfirm)
        {
            var confirmRes = await VpnService.ConfirmAsync();
            if (confirmRes.Status == VpnLoginStatus.Success) return true;

            //报错
            Flower.Play(FlowStatus.Fail, "VPN登录失败，报告开发者");
            return false;
        }

        if (res.Status == VpnLoginStatus.NeedCaptcha || res.Status == VpnLoginStatus.Error)
            //报错,VPN需要重新登录
            return false;
        return false;
    }

   

    private async void OIDC_Click(object sender, RoutedEventArgs e)
    {
        var status = await VpnService.CheckNetworkAsync(false);
        if (status == NetworkStatus.InCampus)
        {
            var oidcService = new OpenId();
            var (url, veri, state) = oidcService.GenerateAuthLoop();
            PasswordManager.SavePassword(veri, "Verifier");
            PasswordManager.SavePassword(state, "State");
            await Launcher.LaunchUriAsync(new(url));
            await Task.Delay(2000);
            Application.Current.Exit();
        }
        else
        {
            Flower.Play(FlowStatus.Fail, "此登录方式需要校园网");
        }
    }



   
    private async Task SetupVpn(string id, string pass)
    {
        var res = await VpnService.LoginAsync(id, pass);
        switch (res.Status)
        {
            case VpnLoginStatus.Success:
                SaveToken(id, pass);
                //Link.IsChecked = false;
                VpnService.IsEnabled = true;
                GoBackOrLaunchApp();
                break;
            case VpnLoginStatus.Error:
                //Link.IsChecked = false;
                //Link.ShowError = true;
                Flower.Play(FlowStatus.Fail, res.Description);
                break;
            case VpnLoginStatus.NeedConfirm:
                var confirmRes = await VpnService.ConfirmAsync();
                if (confirmRes.Status == VpnLoginStatus.Success)
                {
                    SaveToken(id, pass);
                    //Link.IsChecked = false;
                    VpnService.IsEnabled = true;
                    GoBackOrLaunchApp();
                }
                else
                {
                    //Link.IsChecked = false;
                    //Link.ShowError = true;
                    Flower.Play(FlowStatus.Fail, res.Description);
                }

                break;
            case VpnLoginStatus.NeedCaptcha:
                //Link.IsChecked = false;
                //de.Visibility = Visibility.Collapsed;
                //从VpnService处直接调用。
                var captchaId = "";
                var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var captchaUrl = $"{""}/captcha/{captchaId}.png?reload={timeStamp}";
                //captcha.Source = new BitmapImage(new(captchaUrl));
                //captchabox.Visibility = Visibility.Visible;
                Flower.Play(FlowStatus.Fail, res.Message ?? "需要验证码");
                break;
            default:
                
                Flower.Play(FlowStatus.Fail, res.Description);
                break;
        }
    }
    //TODO:rewrite
    private void SaveToken(string id, string pass)
    {
        
        throw new NotImplementedException();
    }

    private void GoBackOrLaunchApp()
    {
        if (Mode == 0)
        {
            
        }
        else
        {
            App.Current.AppMainWindow = new MainWindow();
            App.Current.AppMainWindow.Activate();
            DispatcherQueue.TryEnqueue(() => { Close(); }); //尝试修复竞争条件
        }
    }

    private async void LinkToForum_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await InitializeNetworkAsync();
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("Login", "网络初始化出错", ex.Message);
            Debug("日志已记录。");
        }
    }

  

    private void Debug(string text)
    {
        var notification = new AppNotificationBuilder()
            .AddText("出错")
            .AddText(text)
            .BuildNotification();
        AppNotificationManager.Default.Show(notification);
    }

    

  

  

   
}