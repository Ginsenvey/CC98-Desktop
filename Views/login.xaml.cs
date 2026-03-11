using CC98.Kernel;
using CC98.Kernel.OpenID;
using CC98.Services;
using DevWinUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Net;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;
using System.Text.Json;
using CC98.Objects;
using CC98.Kernel.Network;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class Login : Window
    {
        public ApplicationDataContainer Set;
        public Login(int mode)
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar= true;
            this.SetTitleBar(GridTitleBar);
            this.RootGrid.RequestedTheme = ElementTheme.Light;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;
            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, true);
            AppWindow.SetPresenter(presenter);
            CenterWindow();
            tip.Text = "如果尚未连接浙江大学内网，请在此处登录WebVPN,或者使用[ZJU Connect](https://github.com/Mythologyli/ZJU-Connect-for-Windows/releases).";
            Set = ApplicationData.Current.LocalSettings;
            LoadParams(mode);
        }
        public int mode = 0;
        private void LoadParams(int mode)
        {
            this.mode = mode;
            if (mode == 0) { }//普通登录
            else if(mode==1)//用户已经登录过，只需要添加VPN凭据
            {
                LoginPane.Visibility = Visibility.Collapsed;
                VpnPane.Visibility = Visibility.Visible;
            }
        }
        private void CenterWindow()
        {
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
            if (area == null) return;
            AppWindow.Move(new PointInt32((area.Value.Width - AppWindow.Size.Width) / 2, (area.Value.Height - AppWindow.Size.Height) / 2));
        }


        private async Task InitializeNetwork()
        {
            var network_status = await LoginService.vpn.CheckNetwork(false);
            if (network_status == NetworkStatus.InCampus)
            {
                //启动
                OpenPasswordLoginPane();
                return;
            }
            if (network_status == NetworkStatus.NotInCampus)//在校外
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
                var new_status = await LoginService.vpn.CheckNetwork(true);
                if (new_status == NetworkStatus.ByVPN)
                {
                    LoginService.vpn.Logined = true;
                    LoginService.vpn.IsVpnEnabled = true;
                    OpenPasswordLoginPane();
                    //启动
                    return;
                }
                bool success = await ReloginVPN();
                if (success)
                {
                    LoginService.vpn.IsVpnEnabled = true;
                    SaveVpnToken();
                    //此时vpn应该可用
                    OpenPasswordLoginPane();
                }
            }
            if (network_status == NetworkStatus.MirrorError)
            {
                Flower.Play(FlowStatus.Fail, "连接镜像站失败");
            }
            if (network_status == NetworkStatus.UnknownError)
            {
                Flower.Play(FlowStatus.Fail, "IP被镜像站拦截");
            }
            if (network_status == NetworkStatus.NoConnection)
            {
                Flower.Play(FlowStatus.Fail, "无互联网连接");
            }
        }
        private bool SaveVpnToken()
        {
            var new_ticket = LoginService.vpn.Ticket;
            var new_route = LoginService.vpn.Route;
            string _ticket = new_ticket.Value;
            string _route = new_route.Value;
            if (string.IsNullOrEmpty(_ticket) || (string.IsNullOrEmpty(_route)))
            {
                //报错
                Flower.Play(FlowStatus.Fail, "未获取到完整VPN凭据");
                return false;
            }

            //更新环节
            PasswordManager.SavePassword(_ticket, "Ticket");
            PasswordManager.SavePassword(_route, "Route");
            return true;

        }
        private bool InjectTokenFromVault()
        {
            //提取环节
            var ticket_value = PasswordManager.RetrievePassword("Ticket");
            var route_value = PasswordManager.RetrievePassword("Route");
            var ticket = new Cookie("wengine_vpn_ticketwebvpn_zju_edu_cn", ticket_value, "/", "webvpn.zju.edu.cn");
            var route = new Cookie("route", route_value, "/", "webvpn.zju.edu.cn");
            ticket.HttpOnly = true;
            //注入环节
            if (string.IsNullOrEmpty(ticket_value) || string.IsNullOrEmpty(route_value))
            {
                return false;
            }
            LoginService.vpn.Jar.Add(ticket);
            LoginService.vpn.Jar.Add(route);
            return true;
        }


        private async Task<bool> ReloginVPN()
        {
            if (!PasswordManager.PasswordExists("VpnUserName") || !PasswordManager.PasswordExists("VpnPassWord"))
            {
                return false;
            }
            string id = PasswordManager.RetrievePassword("VpnUserName");
            string pass = PasswordManager.RetrievePassword("VpnPassWord");
            var res = await LoginService.vpn.LoginAsync(id, pass);
            if (res.Status == VPNLoginStatus.Success) return true;
            if (res.Status == VPNLoginStatus.NeedConfirm)
            {
                var confirm_res = await LoginService.vpn.Confirm();
                if (confirm_res.Status == VPNLoginStatus.Success)
                {
                    return true;
                }
                else
                {
                    //报错
                    Flower.Play(FlowStatus.Fail, "VPN登录失败，报告开发者");
                    return false;
                }
            }
            if (res.Status == VPNLoginStatus.NeedCaptcha || res.Status == VPNLoginStatus.Error)
            {
                //报错,VPN需要重新登录
                
                return false;
            }
            return false;
        }

        private void Guide_Click(object sender, RoutedEventArgs e)
        {
            LoginPane.Visibility = Visibility.Collapsed;
            GuidePane.Visibility = Visibility.Visible;
            VpnPane.Visibility = Visibility.Collapsed;
            string guidance = "> 在校外连接需要配置应用的内建WebVPN,或者使用[ZJU Connect](https://github.com/Mythologyli/ZJU-Connect-for-Windows/releases)，打开RVPN，并设置系统代理。\r\n\r\n  **忘记密码/无账号？**\r\n\r\n进入[CC98](https://www.cc98.org/logon)官网操作。\r\n\r\n**遇到问题/想要新功能?**\r\n\r\n你可以在微软商店或[开发进度记录楼](https://www.cc98.org/topic/6173309)反馈此问题。\r\n\r\n你也可以克隆本应用仓库，自由修改和编译新的分支。不过，在分发时，应当告知所有的改动。\r\n\r\n**成为开发者**\r\n\r\n本应用使用`Windows App SDK`,`C#`,`XAML`构建。欢迎所有对.NET生态感兴趣的uu加入本应用的开发，欢迎所有使用者对本应用UI、功能和代码提供建议。";
            GuidePresenter.Text= guidance;
        }

        private async void OIDC_Click(object sender, RoutedEventArgs e)
        {
            var status = await LoginService.vpn.CheckNetwork(false);
            if (status == NetworkStatus.InCampus)
            {
                var oidc_service = new OpenID();
                var Loop = oidc_service.GenerateAuthLoop();
                PasswordManager.SavePassword(Loop.veri, "Verifier");
                PasswordManager.SavePassword(Loop.state, "State");
                await Launcher.LaunchUriAsync(new Uri(Loop.url));
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
                Flower.Play("\uE930", "已配置VPN，无需其他操作");
                return;
            }
            GuidePane.Visibility= Visibility.Collapsed;
            VpnPane.Visibility = Visibility.Visible;
            LoginPane.Visibility = Visibility.Collapsed;
        }

        private void VpnBack_Click(object sender, RoutedEventArgs e)
        {
            GuidePane.Visibility = Visibility.Collapsed;
            VpnPane.Visibility = Visibility.Collapsed;
            LoginPane.Visibility = Visibility.Visible;
        }
      
        private  async void Link_Click(object sender, RoutedEventArgs e)
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
                await SetupVPN(idbox.Text, passbox.Password);
            }
            catch (Exception ex)
            {
                Link.IsChecked = false;
                Link.ShowError = true;
                Flower.Play(FlowStatus.Fail, ex.Message);
            }
            
        }
        private async Task SetupVPN(string id,string pass)
        {
            var res = await LoginService.vpn.LoginAsync(id, pass);
            switch (res.Status)
            {
                case VPNLoginStatus.Success:
                    SaveToken(id, pass);
                    Link.IsChecked = false;
                    LoginService.vpn.IsVpnEnabled = true;
                    GoBackOrLaunchApp();
                    break;
                case VPNLoginStatus.Error:
                    Link.IsChecked = false;
                    Link.ShowError = true;
                    Flower.Play(FlowStatus.Fail, res.Description);
                    break;
                case VPNLoginStatus.NeedConfirm:
                    var confirm_res = await LoginService.vpn.Confirm();
                    if (confirm_res.Status == VPNLoginStatus.Success)
                    {
                        SaveToken(id, pass);
                        Link.IsChecked = false;
                        LoginService.vpn.IsVpnEnabled = true;
                        GoBackOrLaunchApp();
                    }
                    else
                    {
                        Link.IsChecked = false;
                        Link.ShowError = true;
                        Flower.Play(FlowStatus.Fail, res.Description);
                    }
                    break;
                case VPNLoginStatus.NeedCaptcha:
                    Link.IsChecked = false;
                    de.Visibility = Visibility.Collapsed;
                    string captchaId = res.Description;
                    long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string captchaUrl = $"{VpnService.Base}/captcha/{captchaId}.png?reload={timeStamp}";
                    captcha.Source = new BitmapImage(new Uri(captchaUrl));
                    captchabox.Visibility = Visibility.Visible;
                    Flower.Play(FlowStatus.Fail, res.Message??"需要验证码");
                    break;
                default:
                    Link.IsChecked = false;
                    Link.ShowError = true;
                    Flower.Play(FlowStatus.Fail, res.Description);
                    break;
            }
        }
        private void SaveToken(string id,string pass)
        {
            Set.Values["IsVpnUsable"] = "1";
            var ticket = LoginService.vpn.Ticket;
            var route = LoginService.vpn.Route;
            string ticket_value = ticket.Value;
            string route_value = route.Value;
            
            PasswordManager.SavePassword(id, "VpnUserName");
            PasswordManager.SavePassword(pass, "VpnPassWord");
            if (!string.IsNullOrEmpty(ticket_value) && (!string.IsNullOrEmpty(route_value)))
            {
                PasswordManager.SavePassword(ticket_value, "Ticket");
                PasswordManager.SavePassword(route_value, "Route");
                Flower.Play(FlowStatus.Success, "已保存VPN凭据");
            }//保存失败或者token为空时，会出现VPN启用但找不到令牌的情况。
            else
            {
                Flower.Play(FlowStatus.Fail, "未保存VPN凭据");
            }
        }
        private void GoBackOrLaunchApp ()
        {
            if (mode == 0)
            {
                VpnPane.Visibility = Visibility.Collapsed;
                GuidePane.Visibility = Visibility.Collapsed;
                LoginPane.Visibility = Visibility.Visible;
            }
            else
            {
                var window = new MainWindow();
                window.Activate();
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    this.Close();
                });//尝试修复竞争条件
            }
        }

        private async void LinkToForum_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await InitializeNetwork();
            }
            catch(Exception ex)
            {
                await App.Logger.WriteAsync("Login", "网络初始化出错", ex.Message);
                debug("日志已记录。");
            }
            
        }
        private void OpenPasswordLoginPane()
        {
            GuidePane.Visibility = Visibility.Collapsed;
            VpnPane.Visibility = Visibility.Collapsed;
            LoginPane.Visibility = Visibility.Collapsed;
            PasswordLoginPane.Visibility = Visibility.Visible;
        }
        private void debug(string text)
        {
            AppNotification notification = new AppNotificationBuilder()
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
                var result = await LoginService.LoginAsync(ccidbox.Text, ccpassbox.Password);
                LoginWithPassword.IsChecked = false;
                if (!result.IsSuccess)
                {
                    //
                    await App.Logger.WriteAsync("Login", "登录失败", result.Message);
                    LoginWithPassword.ShowError = true;
                    Flower.Play(FlowStatus.Fail,result.Message);
                    return;
                }
                var token = result.Data;
                if (token == null)
                {
                    //
                    await App.Logger.WriteAsync("Login", "登录失败,令牌为空", result.Message);
                    LoginWithPassword.ShowError = true;
                    Flower.Play(FlowStatus.Fail, "发生错误。请报告开发者");
                    return;
                }
                if (token.IsValid)
                {
                    InjectToken(token);
                }
                else
                {
                    await App.Logger.WriteAsync("Login", "登录失败", token.Message);
                    LoginWithPassword.ShowError = true;
                    Flower.Play(FlowStatus.Fail, token.Message);
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
            var window = new MainWindow();
            window.Activate();
            this.DispatcherQueue.TryEnqueue(() =>
            {
                this.Close();
            });
        }

        private void PasswordLoginBack_Click(object sender, RoutedEventArgs e)
        {
            PasswordLoginPane.Visibility = Visibility.Collapsed;
            VpnPane.Visibility = Visibility.Collapsed;
            GuidePane.Visibility = Visibility.Collapsed;
            LoginPane.Visibility = Visibility.Visible;
        }

        private void captchabox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            LoginService.vpn.CaptchaValue= captchabox.Text;
        }
    }
    
}
