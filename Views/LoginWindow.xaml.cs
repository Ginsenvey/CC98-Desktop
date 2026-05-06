using System;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using CC98.Kernel;
using CC98.Kernel.Network;
using CC98.Objects;
using CC98.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using CC98.Kernel.Authorize;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     登录窗口。
/// </summary>
public sealed partial class LoginWindow : Window
{
    public int Mode;
    public ApplicationDataContainer Set;

    public LoginWindow(int mode)
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(GridTitleBar);
        RootGrid.RequestedTheme = ElementTheme.Light;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.SetBorderAndTitleBar(true, true);
        AppWindow.SetPresenter(presenter);
        CenterWindow();
        tip.Text =
            "如果尚未连接浙江大学内网，请在此处登录WebVPN,或者使用[ZJU Connect](https://github.com/Mythologyli/ZJU-Connect-for-Windows/releases).";
        Set = ApplicationData.Current.LocalSettings;
        LoadParams(mode);
    }

    private void LoadParams(int mode)
    {
        Mode = mode;
        if (mode == 0)
        {
        } //普通登录
        else if (mode == 1) //用户已经登录过，只需要添加VPN凭据
        {
            LoginPane.Visibility = Visibility.Collapsed;
            VpnPane.Visibility = Visibility.Visible;
        }
        else //由于密码更改或者套餐到期，尝试使用原凭据VPN登录失败，需要验证码
        {
            LoginPane.Visibility = Visibility.Collapsed;
            VpnPane.Visibility = Visibility.Visible;
            var captchaId = LoginService.Vpn.LastCaptchaId;
            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var captchaUrl = $"{VpnService.BaseUrl}/captcha/{captchaId}.png?reload={timeStamp}";
            captcha.Source = new BitmapImage(new(captchaUrl));
            captchabox.Visibility = Visibility.Visible;
        }
    }

    private void CenterWindow()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
        if (area == null) return;
        AppWindow.Move(new((area.Value.Width - AppWindow.Size.Width) / 2,
            (area.Value.Height - AppWindow.Size.Height) / 2));
    }


    private async Task InitializeNetwork()
    {
        var networkStatus = await LoginService.Vpn.CheckNetworkAsync(false);
        if (networkStatus == NetworkStatus.InCampus)
        {
            //启动
            OpenPasswordLoginPane();
            return;
        }

        if (networkStatus == NetworkStatus.NotInCampus) //在校外
        {
            if (ValidationHelper.GetValue(Set, "IsVpnUsable") != "1")
            {
                //打开VPN配置设置
                LoginPane.Visibility = Visibility.Collapsed;
                VpnPane.Visibility = Visibility.Visible;
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

            var newStatus = await LoginService.Vpn.CheckNetworkAsync(true);
            if (newStatus == NetworkStatus.ByVpn)
            {
                LoginService.Vpn.IsLoggedIn = true;
                LoginService.Vpn.IsVpnEnabled = true;
                OpenPasswordLoginPane();
                //启动
                return;
            }

            var success = await ReloginVpn();
            if (success)
            {
                LoginService.Vpn.IsVpnEnabled = true;
                SaveVpnToken();
                //此时vpn应该可用
                OpenPasswordLoginPane();
            }
        }

        if (networkStatus == NetworkStatus.MirrorError) Flower.Play(FlowStatus.Fail, "连接镜像站失败");
        if (networkStatus == NetworkStatus.UnknownError) Flower.Play(FlowStatus.Fail, "IP被镜像站拦截");
        if (networkStatus == NetworkStatus.NoConnection) Flower.Play(FlowStatus.Fail, "无互联网连接");
    }

    private bool SaveVpnToken()
    {
        var newTicket = LoginService.Vpn.Ticket;
        var newRoute = LoginService.Vpn.Route;
        var ticket = newTicket.Value;
        var route = newRoute.Value;
        if (string.IsNullOrEmpty(ticket) || string.IsNullOrEmpty(route))
        {
            //报错
            Flower.Play(FlowStatus.Fail, "未获取到完整VPN凭据");
            return false;
        }

        //更新环节
        PasswordManager.SavePassword(ticket, "Ticket");
        PasswordManager.SavePassword(route, "Route");
        return true;
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
        LoginService.Vpn.CookieContainer.Add(ticket);
        LoginService.Vpn.CookieContainer.Add(route);
        return true;
    }


    private async Task<bool> ReloginVpn()
    {
        if (!PasswordManager.PasswordExists("VpnUserName") || !PasswordManager.PasswordExists("VpnPassWord"))
            return false;
        var id = PasswordManager.RetrievePassword("VpnUserName");
        var pass = PasswordManager.RetrievePassword("VpnPassWord");
        var res = await LoginService.Vpn.LoginAsync(id, pass);
        if (res.Status == VpnLoginStatus.Success) return true;
        if (res.Status == VpnLoginStatus.NeedConfirm)
        {
            var confirmRes = await LoginService.Vpn.ConfirmAsync();
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

    private void Guide_Click(object sender, RoutedEventArgs e)
    {
        LoginPane.Visibility = Visibility.Collapsed;
        GuidePane.Visibility = Visibility.Visible;
        VpnPane.Visibility = Visibility.Collapsed;
        var guidance =
            "> 在校外连接需要配置应用的内建WebVPN,或者使用[ZJU Connect](https://github.com/Mythologyli/ZJU-Connect-for-Windows/releases)，打开RVPN，并设置系统代理。\r\n\r\n  **忘记密码/无账号？**\r\n\r\n进入[CC98](https://www.cc98.org/logon)官网操作。\r\n\r\n**遇到问题/想要新功能?**\r\n\r\n你可以在微软商店或[开发进度记录楼](https://www.cc98.org/topic/6173309)反馈此问题。\r\n\r\n你也可以克隆本应用仓库，自由修改和编译新的分支。不过，在分发时，应当告知所有的改动。\r\n\r\n**成为开发者**\r\n\r\n本应用使用`Windows App SDK`,`C#`,`XAML`构建。欢迎所有对.NET生态感兴趣的uu加入本应用的开发，欢迎所有使用者对本应用UI、功能和代码提供建议。";
        GuidePresenter.Text = guidance;
    }

    private async void OIDC_Click(object sender, RoutedEventArgs e)
    {
        var status = await LoginService.Vpn.CheckNetworkAsync(false);
        if (status == NetworkStatus.InCampus)
        {
            var oidcService = new OpenId();
            var loop = oidcService.GenerateAuthLoop();
            PasswordManager.SavePassword(loop.veri, "Verifier");
            PasswordManager.SavePassword(loop.state, "State");
            await Launcher.LaunchUriAsync(new(loop.url));
            await Task.Delay(2000);
            Application.Current.Exit();
        }
        else
        {
            Flower.Play(FlowStatus.Fail, "此登录方式需要校园网");
        }
    }


    private void GuideBack_Click(object sender, RoutedEventArgs e)
    {
        GuidePresenter.Text = "";
        LoginPane.Visibility = Visibility.Visible;
        VpnPane.Visibility = Visibility.Collapsed;
        GuidePane.Visibility = Visibility.Collapsed;
    }

    private void LinkToVpn_Click(object sender, RoutedEventArgs e)
    {
        if (ValidationHelper.GetValue(Set, "IsVpnUsable") == "1")
        {
            Flower.Play(FlowStatus.Info, "已配置VPN，无需其他操作");
            return;
        }

        GuidePane.Visibility = Visibility.Collapsed;
        VpnPane.Visibility = Visibility.Visible;
        LoginPane.Visibility = Visibility.Collapsed;
    }

    private void VpnBack_Click(object sender, RoutedEventArgs e)
    {
        GuidePane.Visibility = Visibility.Collapsed;
        VpnPane.Visibility = Visibility.Collapsed;
        LoginPane.Visibility = Visibility.Visible;
    }

    private async void Link_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(idbox.Text) || string.IsNullOrEmpty(passbox.Password))
        {
            //
            Flower.Play(FlowStatus.Info, "请输入完整凭据");
            return;
        }

        Link.IsChecked = true;
        try
        {
            await SetupVpn(idbox.Text, passbox.Password);
        }
        catch (Exception ex)
        {
            Link.IsChecked = false;
            Link.ShowError = true;
            Flower.Play(FlowStatus.Fail, ex.Message);
        }
    }

    private async Task SetupVpn(string id, string pass)
    {
        var res = await LoginService.Vpn.LoginAsync(id, pass);
        switch (res.Status)
        {
            case VpnLoginStatus.Success:
                SaveToken(id, pass);
                Link.IsChecked = false;
                LoginService.Vpn.IsVpnEnabled = true;
                GoBackOrLaunchApp();
                break;
            case VpnLoginStatus.Error:
                Link.IsChecked = false;
                Link.ShowError = true;
                Flower.Play(FlowStatus.Fail, res.Description);
                break;
            case VpnLoginStatus.NeedConfirm:
                var confirmRes = await LoginService.Vpn.ConfirmAsync();
                if (confirmRes.Status == VpnLoginStatus.Success)
                {
                    SaveToken(id, pass);
                    Link.IsChecked = false;
                    LoginService.Vpn.IsVpnEnabled = true;
                    GoBackOrLaunchApp();
                }
                else
                {
                    Link.IsChecked = false;
                    Link.ShowError = true;
                    Flower.Play(FlowStatus.Fail, res.Description);
                }

                break;
            case VpnLoginStatus.NeedCaptcha:
                Link.IsChecked = false;
                de.Visibility = Visibility.Collapsed;
                //从VpnService处直接调用。
                var captchaId = LoginService.Vpn.LastCaptchaId;
                var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var captchaUrl = $"{VpnService.BaseUrl}/captcha/{captchaId}.png?reload={timeStamp}";
                captcha.Source = new BitmapImage(new(captchaUrl));
                captchabox.Visibility = Visibility.Visible;
                Flower.Play(FlowStatus.Fail, res.Message ?? "需要验证码");
                break;
            default:
                Link.IsChecked = false;
                Link.ShowError = true;
                Flower.Play(FlowStatus.Fail, res.Description);
                break;
        }
    }

    private void SaveToken(string id, string pass)
    {
        Set.Values["IsVpnUsable"] = "1";
        var ticket = LoginService.Vpn.Ticket;
        var route = LoginService.Vpn.Route;
        var ticketValue = ticket.Value;
        var routeValue = route.Value;

        PasswordManager.SavePassword(id, "VpnUserName");
        PasswordManager.SavePassword(pass, "VpnPassWord");
        if (!string.IsNullOrEmpty(ticketValue) && !string.IsNullOrEmpty(routeValue))
        {
            PasswordManager.SavePassword(ticketValue, "Ticket");
            PasswordManager.SavePassword(routeValue, "Route");
            Flower.Play(FlowStatus.Success, "已保存VPN凭据");
        } //保存失败或者token为空时，会出现VPN启用但找不到令牌的情况。
        else
        {
            Flower.Play(FlowStatus.Fail, "未保存VPN凭据");
        }
    }

    private void GoBackOrLaunchApp()
    {
        if (Mode == 0)
        {
            VpnPane.Visibility = Visibility.Collapsed;
            GuidePane.Visibility = Visibility.Collapsed;
            LoginPane.Visibility = Visibility.Visible;
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
            await InitializeNetwork();
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("Login", "网络初始化出错", ex.Message);
            Debug("日志已记录。");
        }
    }

    private void OpenPasswordLoginPane()
    {
        GuidePane.Visibility = Visibility.Collapsed;
        VpnPane.Visibility = Visibility.Collapsed;
        LoginPane.Visibility = Visibility.Collapsed;
        PasswordLoginPane.Visibility = Visibility.Visible;
    }

    private void Debug(string text)
    {
        var notification = new AppNotificationBuilder()
            .AddText("出错")
            .AddText(text)
            .BuildNotification();
        AppNotificationManager.Default.Show(notification);
    }

    private async void LoginWithPassword_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ccidbox.Text) && !string.IsNullOrEmpty(ccpassbox.Password))
        {
            LoginWithPassword.IsChecked = true;
            var result = await LoginService.LoginWithPasswordAsync(ccidbox.Text, ccpassbox.Password);
            LoginWithPassword.IsChecked = false;
            if (result.IsError)
            {
                //
                await App.Logger.WriteAsync("Login", "登录失败", result.ErrorDescription);
                LoginWithPassword.ShowError = true;
                Flower.Play(FlowStatus.Fail, result.ErrorDescription);
                return;
            }
            return;//
            var token = result;
            if (token == null)
            {
                //
                await App.Logger.WriteAsync("Login", "登录失败,令牌为空", result.ErrorDescription);
                LoginWithPassword.ShowError = true;
                Flower.Play(FlowStatus.Fail, "发生错误。请报告开发者");
                return;
            }

            if (!token.IsError)
            {
                //InjectToken(token);
            } 
            else
            {
                //await App.Logger.WriteAsync("Login", "登录失败", token.Message);
                LoginWithPassword.ShowError = true;
                //Flower.Play(FlowStatus.Fail, token.Message);
                Set.Values["IsActive"] = "0";
            }
        }
        else
        {
            Flower.Play(FlowStatus.Info, "凭据不完整");
        }
    }

    private void InjectToken(AuthorizeResult result)
    {
        //在注入之前，必须确认IsValid==true
        PasswordManager.SavePassword(result.AccessToken, "Access");
        PasswordManager.SavePassword(result.RefreshToken, "Refresh");
        LoginWithPassword.IsChecked = false;
        Set.Values["IsActive"] = "2";
        App.Current.AppMainWindow = new MainWindow();
        App.Current.AppMainWindow.Activate();
        DispatcherQueue.TryEnqueue(() => { Close(); });
    }

    private void PasswordLoginBack_Click(object sender, RoutedEventArgs e)
    {
        PasswordLoginPane.Visibility = Visibility.Collapsed;
        VpnPane.Visibility = Visibility.Collapsed;
        GuidePane.Visibility = Visibility.Collapsed;
        LoginPane.Visibility = Visibility.Visible;
    }

    private void captchabox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LoginService.Vpn.CaptchaValue = captchabox.Text;
    }
}