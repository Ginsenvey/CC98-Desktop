using CC98.Kernel;
using CC98.Kernel.OpenID;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class login : Window
    {
        public ApplicationDataContainer Set;
        public login(int mode)
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


        private async Task<string> InitializeNetwork()
        {
            var network_status = await CCloginservice.vpn.CheckNetwork(false);
            if (network_status == "0")//无网络
            {
                if (ValidationHelper.IsTokenExist(Set, "IsVpnUsable") == "1")
                {
                    //检测是否已初始化Ticket。若已初始化，使用并检查有效性。无效则重连。未初始化是出错的情况。
                    if (PasswordManager.PasswordExists("Ticket") && PasswordManager.PasswordExists("Route"))
                    {
                        //构造Ticket
                        var ticket_value = PasswordManager.RetrievePassword("Ticket");
                        var route_value = PasswordManager.RetrievePassword("Route");
                        var ticket = new Cookie("wengine_vpn_ticketwebvpn_zju_edu_cn", ticket_value, "/", "webvpn.zju.edu.cn");
                        var route = new Cookie("route", route_value, "/", "webvpn.zju.edu.cn");
                        ticket.HttpOnly = true;
                        if (!string.IsNullOrEmpty(ticket_value) && (!string.IsNullOrEmpty(route_value)))
                        {
                            CCloginservice.vpn.Jar.Add(ticket);
                            CCloginservice.vpn.Jar.Add(route);
                            string new_status = await CCloginservice.vpn.CheckNetwork(true);
                            if (new_status == "1")//该函数不受IsVpnEnable和Logined影响
                            {
                                CCloginservice.vpn.Logined = true;
                                CCloginservice.vpn.IsVpnEnabled = true;
                                return "1";
                            }
                            else//过期，尝试使用凭据重新获取Ticket
                            {
                                CCloginservice.vpn.Logined = false;
                                CCloginservice.vpn.IsVpnEnabled = false;
                                if (PasswordManager.PasswordExists("VpnUserName") && PasswordManager.PasswordExists("VpnPassWord"))
                                {
                                    string id = PasswordManager.RetrievePassword("VpnUserName");
                                    string pass = PasswordManager.RetrievePassword("VpnPassWord");
                                    string vpn_res = await CCloginservice.vpn.LoginAsync(id, pass);
                                    if (vpn_res == "1")//连接成功
                                    {
                                        //这里不需要再额外修改Logined,因为LoginAsync中已经修改
                                        CCloginservice.vpn.IsVpnEnabled = true;
                                        var new_ticket = CCloginservice.vpn.Ticket;
                                        var new_route = CCloginservice.vpn.Route;
                                        string _ticket = new_ticket.Value;
                                        string _route = new_route.Value;
                                        if (!string.IsNullOrEmpty(_ticket) && (!string.IsNullOrEmpty(_route)))
                                        {
                                            PasswordManager.SavePassword(_ticket, "Ticket");
                                            PasswordManager.SavePassword(_route, "Route");
                                            return "1";
                                        }//保存失败或者token为空时，会出现VPN启用但找不到令牌的情况。
                                        else
                                        {
                                            //通常不会有此情况
                                            return "2:未获取到Ticket";
                                        }
                                        //此时vpn应该可用
                                    }
                                    else//登录失败，可能是因为凭据错误
                                    {
                                        return "3:凭据错误或者VPN欠费";
                                    }
                                }
                                else
                                {
                                    return "4:凭据不完整。请修改VPN凭据";
                                }
                            }

                        }
                        else
                        {
                            //存在这个凭据，但是解析为Cookie失败
                            return "5:Ticket令牌解析失败。反馈此问题。";
                        }
                    }
                    else
                    {
                        return "5:Ticket令牌意外地未被保存。反馈此问题。";
                    }
                }
                else//vpn未启用，这可能是代码逻辑问题
                {
                    return "6";
                }
            }
            else if (network_status == "1")
            {
                return "1";
            }
            else
            {
                return $"7:{network_status.Split(":")[1]}";
            }

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
            var status = await InitializeNetwork();
            switch (status)
            {
                case "1":
                    var oidc_service = new OpenID();
                    var Loop = oidc_service.GenerateAuthLoop();
                    PasswordManager.SavePassword(Loop.veri, "Verifier");
                    PasswordManager.SavePassword(Loop.state, "State");
                    await Launcher.LaunchUriAsync(new Uri(Loop.url));
                    await Task.Delay(2000);
                    Application.Current.Exit();
                    break;
                case "6"://未启用VPN
                    Flower.PlayAnimation("\uEA39", "未启用VPN");
                    GuidePane.Visibility=Visibility.Collapsed;
                    LoginPane.Visibility = Visibility.Collapsed;
                    VpnPane.Visibility = Visibility.Visible;
                    break;
                default:
                    Flower.PlayAnimation("\uEA39", status);
                    break;
            }    
        }

        private async void MarkdownTextBlock_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(e.Link));
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
            if (ValidationHelper.IsTokenExist(Set, "IsVpnUsable") == "1")
            {
                Flower.PlayAnimation("\uE930", "已配置VPN，无需其他操作");
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
            if (!string.IsNullOrEmpty(idbox.Text) && !string.IsNullOrEmpty(passbox.Password))
            {
                Link.IsChecked = true;
                var r = await SetupVPN(idbox.Text, passbox.Password);
                if (r == "1")
                {
                    Flower.PlayAnimation("\uE930", "已保存VPN凭据");
                    Link.IsChecked = false;
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
                else
                {
                    Link.IsChecked = false;
                    Link.ShowError = true;
                    Flower.PlayAnimation("\uEA39", r);
                }

            }
            else
            {
                Flower.PlayAnimation("\uEA39", "凭据不完整");
            }
        }
        private async Task<string> SetupVPN(string id,string pass)
        {
            try
            {
                string vpn_res = await CCloginservice.vpn.LoginAsync(id, pass);
                if (vpn_res == "1")
                {
                    Set.Values["IsVpnUsable"] = "1";
                    var ticket = CCloginservice.vpn.Ticket;
                    var route=CCloginservice.vpn.Route;
                    string ticket_value = ticket.Value;
                    string route_value=route.Value;
                    CCloginservice.vpn.IsVpnEnabled = true;
                    PasswordManager.SavePassword(idbox.Text, "VpnUserName");
                    PasswordManager.SavePassword(passbox.Password, "VpnPassWord");
                    if (!string.IsNullOrEmpty(ticket_value)&&(!string.IsNullOrEmpty(route_value)))
                    {
                        PasswordManager.SavePassword(ticket_value, "Ticket");
                        PasswordManager.SavePassword(route_value, "Route");
                        return "1";
                    }//保存失败或者token为空时，会出现VPN启用但找不到令牌的情况。
                    else
                    {
                        return "2:保存Ticket失败";
                    }
                }
                else
                {
                    return vpn_res;
                }
            }
            catch(Exception ex)
            {
                return $"3:{ex.Message}";
            }
        }

        private async void LinkToForum_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var status = await InitializeNetwork();
                switch (status)
                {
                    case "1":

                        GuidePane.Visibility = Visibility.Collapsed;
                        VpnPane.Visibility = Visibility.Collapsed;
                        LoginPane.Visibility = Visibility.Collapsed;
                        PasswordLoginPane.Visibility = Visibility.Visible;
                        break;
                    case "6"://未启用VPN
                        Flower.PlayAnimation("\uEA39", "未启用VPN");
                        GuidePane.Visibility = Visibility.Collapsed;
                        LoginPane.Visibility = Visibility.Collapsed;
                        VpnPane.Visibility = Visibility.Visible;
                        break;
                    default:
                        Flower.PlayAnimation("\uEA39", status);
                        break;
                }
            }
            catch(Exception ex)
            {
                AppNotification notification = new AppNotificationBuilder()
                    .AddText("出错")
                    .AddText(ex.Message)
                    .BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
            
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
                var r = await CCloginservice.LoginAsync(ccidbox.Text, ccpassbox.Password);
                Auth(r);
            }
            else
            {
                Flower.PlayAnimation("\uEA39", "凭据不完整");
            }
        }
        
        private void Auth(string re)
        {
            if (re.Contains("access_token"))
            {
                try
                {
                    var js = Deserializer.ToDictionary(re);
                    if (js != null)
                    {
                        string access = ValidationHelper.GetKey(js, "access_token");
                        string refresh = ValidationHelper.GetKey(js, "refresh_token");
                        if (access != "0" && refresh != "0")
                        {
                            PasswordManager.SavePassword(access, "Access");
                            PasswordManager.SavePassword(refresh, "Refresh");
                            LoginWithPassword.IsChecked = false;
                            Set.Values["IsActive"] = "2";
                            var window = new MainWindow();
                            window.Activate();
                            this.DispatcherQueue.TryEnqueue(() =>
                            {
                                this.Close();
                            });
                        }
                        else
                        {                            
                            //不应存在此情况
                            HandleError("出错。请报告开发者");
                        }
                    }
                    else
                    {
                        HandleError("登录失败:无法解析令牌");
                    }
                }
                catch(Exception ex)//解析错误，凭据处理错误
                {
                    HandleError(ex.Message);
                }
            }
            else//未返回有效凭据
            {
                LoginWithPassword.IsChecked = false;
                LoginWithPassword.ShowError = true;
                Flower.PlayAnimation("\uEA39", "凭据不正确");
                Set.Values["IsActive"] = "0";
            }
        }
        private void HandleError(string message)
        {
            LoginWithPassword.IsChecked = false;
            LoginWithPassword.ShowError = true;
            Set.Values["IsActive"] = "0";
            Flower.PlayAnimation("\uEA39", message);
            PasswordLoginPane.Visibility = Visibility.Collapsed;
            VpnPane.Visibility = Visibility.Collapsed;
            GuidePane.Visibility = Visibility.Collapsed;
            LoginPane.Visibility = Visibility.Visible;
        }

        private void PasswordLoginBack_Click(object sender, RoutedEventArgs e)
        {
            PasswordLoginPane.Visibility = Visibility.Collapsed;
            VpnPane.Visibility = Visibility.Collapsed;
            GuidePane.Visibility = Visibility.Collapsed;
            LoginPane.Visibility = Visibility.Visible;
        }
    }
    
}
