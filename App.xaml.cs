
using CC98.Kernel;
using CC98.UserExperience;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public ApplicationDataContainer Set;
        public static event Action<ElementTheme> ThemeChanged;

        // 触发事件的方法
        public static void RaiseThemeChanged(ElementTheme theme)
        {
            ThemeChanged?.Invoke(theme);
        }
        public App()
        {
            this.InitializeComponent();
            Set = ApplicationData.Current.LocalSettings;
            
        }
        private async Task<string> InitializeNetwork()
        {
            var network_status = await CCloginservice.vpn.CheckNetwork(false);
            if (network_status == "0")//无网络
            {
                if (ValidationHelper.IsTokenExist(Set, "IsVpnUsable") == "1")
                {
                    //检测是否已初始化Ticket。若已初始化，使用并检查有效性。无效则重连。未初始化是出错的情况。
                    if (PasswordManager.PasswordExists("Ticket")&& PasswordManager.PasswordExists("Route"))
                    {
                        //构造Ticket
                        var ticket_value = PasswordManager.RetrievePassword("Ticket");
                        var route_value= PasswordManager.RetrievePassword("Route");
                        var ticket = new Cookie("wengine_vpn_ticketwebvpn_zju_edu_cn", ticket_value, "/", "webvpn.zju.edu.cn");
                        var route= new Cookie("wengine_vpn_ticketwebvpn_zju_edu_cn", route_value, "/", "webvpn.zju.edu.cn");
                        ticket.HttpOnly = true;
                        if (!string.IsNullOrEmpty(ticket_value)&&(!string.IsNullOrEmpty(route_value)))
                        {
                            CCloginservice.vpn.Jar.Add(ticket);
                            CCloginservice.vpn.Jar.Add(route);
                            if (await CCloginservice.vpn.CheckNetwork(true) == "1")//该函数不受IsVpnEnable和Logined影响
                            {
                                CCloginservice.vpn.IsVpnEnabled = true;
                                return "1";
                            }
                            else//过期，尝试使用凭据重新获取Ticket
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
                                        var new_ticket = CCloginservice.vpn.Ticket;
                                        var new_route=CCloginservice.vpn.Route;
                                        string _ticket = new_ticket.Value;
                                        string _route = new_route.Value;
                                        if (!string.IsNullOrEmpty(_ticket)&&(!string.IsNullOrEmpty(_route)))
                                        {
                                            PasswordManager.SavePassword(_ticket, "Ticket");
                                            PasswordManager.SavePassword("_route", "Route");
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
        


        //登录检查.对于IsActive=1的情况，检查是否在内网。若否，检查vpn是否可用。可用则检查vpn token是否过期。不可用则激活login(mode=1)。
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var e= AppInstance.GetActivatedEventArgs();
            if (e.Kind == ActivationKind.Protocol)
            {
                var protocol = (ProtocolActivatedEventArgs)e;
                var query = System.Web.HttpUtility.ParseQueryString(protocol.Uri.Query);
                AuthFromOpenID(query);
            }
            else
            {
                string IsActive = ValidationHelper.IsTokenExist(Set, "IsActive");
                if (IsActive == "1"||IsActive=="2")//已登录
                {
                    try
                    {
                        var status = await InitializeNetwork();
                        switch (status)
                        {
                            case "1":
                                m_window = new MainWindow();
                                m_window.Activate();
                                break;
                            case "6":
                                ActivateLogin(1);
                                break;
                            default:
                                AppNotification _notification = new AppNotificationBuilder()
                                .AddText("网络异常")
                                .AddText("详细信息:\n" + status)
                                .BuildNotification();
                                AppNotificationManager.Default.Show(_notification);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppNotification notification = new AppNotificationBuilder()
                        .AddText("启动失败")
                        .AddText(ex.Message)
                        .BuildNotification();
                        AppNotificationManager.Default.Show(notification);
                    }
                    

                }

                else //未登录
                {
                    ActivateLogin(0);
                }
            }
            
            
        }
        private async void AuthFromOpenID(NameValueCollection query)
        {
            string code = ValidationHelper.GetValue(query, "code");
            string iss = ValidationHelper.GetValue(query, "iss");
            string state = ValidationHelper.GetValue(query, "state");
            string session_state = ValidationHelper.GetValue(query, "session_state");
            if (code != "0" && iss != "0" && state != "0" && session_state != "0")
            {
                string state_to_verify=PasswordManager.RetrievePassword("State");
                PasswordManager.RemovePassword("State");
                if (state_to_verify == state)//检验
                {
                    string veri = PasswordManager.RetrievePassword("Verifier");
                    if (veri != null)
                    {
                        string res_text = await CCloginservice.OAuth(veri, code);
                        Auth(res_text);
                        PasswordManager.ClearAllPasswords("Verifier");
                    }
                }
                else
                {
                    AppNotification notification = new AppNotificationBuilder()
                    .AddText("警告")
                    .AddText("服务器返回验证参数不正确。当前网络环境可能有风险，或者存在其他问题。")
                    .BuildNotification();
                    AppNotificationManager.Default.Show(notification);
                }
                
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
                            Set.Values["IsActive"] = "1";
                            m_window = new MainWindow();
                            m_window.Activate();
                        }
                        else
                        {
                            //不应存在此情况
                        }
                    }
                }
                catch//解析错误，凭据处理错误
                {
                    Set.Values["IsActive"] = "0";
                    ActivateLogin(0);
                }
            }
            else//未返回有效凭据
            {           
                Set.Values["IsActive"] = "0";
                ActivateLogin(0);
            }
        }  
        private void ActivateLogin(int mode)
        {
            loginpage = new login(mode);
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(loginpage);
            var windowStyle = Win32Interop.GetWindowLong(hWnd, Win32Interop.GWL_STYLE);
            Win32Interop.SetWindowLong(hWnd, Win32Interop.GWL_STYLE, windowStyle & ~Win32Interop.WS_THICKFRAME);
            var desiredWidth = 720;  // 逻辑像素
            var desiredHeight = 460; // 逻辑像素
            var dpi = Win32Interop.GetDpiForWindow(hWnd);
            var scalingFactor = dpi / 96.0;
            Win32Interop.SetWindowPos(
                hWnd,
                Win32Interop.HWND_TOP,
                0, 0,
                (int)(desiredWidth * scalingFactor),
                (int)(desiredHeight * scalingFactor),
                Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOZORDER);
            loginpage.Title = "登录";
            loginpage.Activate();
        }
        public  Window m_window { get; private set; }
        
        private Window loginpage;
    }
}
