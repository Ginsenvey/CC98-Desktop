using CCkernel;
using Microsoft.Security.Authentication.OAuth;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.BadgeNotifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
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
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;
            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, true);
            AppWindow.SetPresenter(presenter);
            CenterWindow();
            Set = ApplicationData.Current.LocalSettings;
            LoadParams(mode);
        }
        public int mode = 0;
        private void LoadParams(int mode)
        {
            this.mode = mode;
            if (mode == 0) { }//普通登录
            else if(mode==1)//只需要验证VPN
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
                    //检测是否已初始化TWFID。若已初始化，使用并检查有效性。无效则重连。未初始化是出错的情况。
                    if (PasswordManager.PasswordExists("TWFID"))
                    {
                        var token = JsonConvert.DeserializeObject<Cookie>(PasswordManager.RetrievePassword("TWFID"));
                        if (token != null)
                        {
                            CCloginservice.vpn.Jar.Add(token);
                            if (await CCloginservice.vpn.CheckNetwork(true) == "1")//该函数不受IsVpnEnable和Logine影响
                            {
                                CCloginservice.vpn.IsVpnEnabled = true;
                                return "1";
                            }
                            else//过期，尝试使用凭据重新获取TWFID
                            {
                                CCloginservice.vpn.IsVpnEnabled = false;
                                if (PasswordManager.PasswordExists("VpnUserName") && PasswordManager.PasswordExists("VpnPassWord"))
                                {
                                    string id = PasswordManager.RetrievePassword("VpnUserName");
                                    string pass = PasswordManager.RetrievePassword("VpnPassWord");
                                    string vpn_res = await CCloginservice.vpn.LoginAsync(id, pass);
                                    if (vpn_res == "1")//连接成功
                                    {
                                        CCloginservice.vpn.IsVpnEnabled = true;
                                        var cookie = CCloginservice.vpn.TWFID;
                                        string _token = JsonConvert.SerializeObject(cookie);
                                        if (!string.IsNullOrEmpty(_token))
                                        {
                                            PasswordManager.SavePassword(_token, "TWFID");
                                            return "1";
                                        }//保存失败或者token为空时，会出现VPN启用但找不到令牌的情况。
                                        else
                                        {
                                            //通常不会有此情况
                                            return "2:未获取到TWFID";
                                        }
                                        //此时vpn应该可用
                                    }
                                    else//登录失败，可能是因为凭据错误
                                    {
                                        return "3:登录失败，请检查凭据";
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
                            return "5:TWFID令牌解析失败。反馈此问题。";
                        }
                    }
                    else
                    {
                        return "5:TWFID令牌意外地未被保存。反馈此问题。";
                        //("出错", "TWFID令牌意外地消失了。");
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
            string guidance = "> 登录之前，请确保处于ZJU内网环境，或者使用[ZJU Connect](https://github.com/Mythologyli/ZJU-Connect-for-Windows/releases)，打开RVPN，并设置系统代理。\r\n\r\n **忘记密码/无账号？**\r\n\r\n进入[CC98](https://www.cc98.org/logon)官网操作。\r\n\r\n**遇到问题/想要新功能?**\r\n\r\n你可以在开发者的Github Issue处，或[CC98桌面客户端开发进度记录楼](https://www.cc98.org/topic/6173309))反馈此问题。通常，前端bug修复比较快。\r\n\r\n你也可以克隆本应用仓库，自由修改和编译新的分支。不过，在分发时，应当告知所有的改动。\r\n\r\n**成为开发者**\r\n\r\n本应用使用`Windows App SDK`,`C#`,`XAML`构建。欢迎所有对.NET生态感兴趣的uu加入本应用的开发，欢迎所有使用者对本应用UI、功能和代码提供建议。";
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
                var r=await SetupVPN(idbox.Text,passbox.Password);
                if (r == "1")
                {
                    Flower.PlayAnimation("\uE930", "已保存VPN凭据");
                    if (mode == 0)
                    {
                        VpnPane.Visibility = Visibility.Collapsed;
                        GuidePane.Visibility = Visibility.Collapsed;
                        LoginPane.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        this.Close();
                        var window=new MainWindow();
                        window.Activate();
                    }
                    
                    
                }
                else
                {
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
            string vpn_res = await CCloginservice.vpn.LoginAsync(id, pass);
            if (vpn_res == "1")
            {
                Set.Values["IsVpnUsable"] = "1";
                var cookie = CCloginservice.vpn.TWFID;
                string token = JsonConvert.SerializeObject(cookie);
                CCloginservice.vpn.IsVpnEnabled = true;
                PasswordManager.SavePassword(idbox.Text, "VpnUserName");
                PasswordManager.SavePassword(passbox.Password, "VpnPassWord");
                if (!string.IsNullOrEmpty(token))
                {
                    PasswordManager.SavePassword(token, "TWFID");
                    return "1";
                }//保存失败或者token为空时，会出现VPN启用但找不到令牌的情况。
                else
                {
                    return "2:保存TWFID失败";
                }   
            }
            else
            {
                return "3:检查网络或凭据";
            }
        }
    }
    
}
