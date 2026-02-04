using CC98.Controls;
using CC98.Kernel;
using CC98.Kernel.Network;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using ColorCode.Compilation.Languages;
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
using Windows.UI.Text;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public static event Action<ElementTheme> ThemeChanged;
        public static new App Current => (App)Application.Current;
        private static AppLog _logger;
        public static AppLog Logger => _logger ?? throw new InvalidOperationException("Logger未初始化");
        // 在 App 类中添加字段以保留托盘图标引用，防止被 GC 回收
        private DevWinUI.SystemTrayIcon? _trayIcon;

        public static void RaiseThemeChanged(ElementTheme theme)
        {
            ThemeChanged?.Invoke(theme);
        }
        public App()
        {
            this.InitializeComponent();            
        }
        

        
        private async Task<string> InitializeNetwork()
        {
            var network_status = await LoginService.vpn.CheckNetwork(false);
            if (network_status == "0")//无网络
            {
                if (ValidationHelper.GetValue(Set, "IsVpnUsable") == "1")
                {
                    //检测是否已初始化Ticket。若已初始化，使用并检查有效性。无效则重连。未初始化是出错的情况。
                    if (PasswordManager.PasswordExists("Ticket")&& PasswordManager.PasswordExists("Route"))
                    {
                        //提取环节
                        var ticket_value = PasswordManager.RetrievePassword("Ticket");
                        var route_value= PasswordManager.RetrievePassword("Route");
                        var ticket = new Cookie("wengine_vpn_ticketwebvpn_zju_edu_cn", ticket_value, "/", "webvpn.zju.edu.cn");
                        var route= new Cookie("route", route_value, "/", "webvpn.zju.edu.cn");
                        ticket.HttpOnly = true;
                        //注入环节
                        if (!string.IsNullOrEmpty(ticket_value)&&(!string.IsNullOrEmpty(route_value)))
                        {
                            LoginService.vpn.Jar.Add(ticket);
                            LoginService.vpn.Jar.Add(route);
                            string new_status = await LoginService.vpn.CheckNetwork(true);
                            if (new_status== "1")//该函数不受IsVpnEnable和Logined影响
                            {
                                LoginService.vpn.Logined = true;
                                LoginService.vpn.IsVpnEnabled = true;
                                return "1";
                            }
                            else//过期，尝试使用凭据重新获取Ticket
                            {
                                LoginService.vpn.Logined = false;
                                LoginService.vpn.IsVpnEnabled = false;
                                if (PasswordManager.PasswordExists("VpnUserName") && PasswordManager.PasswordExists("VpnPassWord"))
                                {
                                    string id = PasswordManager.RetrievePassword("VpnUserName");
                                    string pass = PasswordManager.RetrievePassword("VpnPassWord");
                                    var res = await LoginService.vpn.LoginAsync(id, pass);
                                    if (res.Status==VPNLoginStatus.Success)//连接成功
                                    {
                                        LoginService.vpn.IsVpnEnabled = true;
                                        var new_ticket = LoginService.vpn.Ticket;
                                        var new_route=LoginService.vpn.Route;
                                        string _ticket = new_ticket.Value;
                                        string _route = new_route.Value;
                                        if (!string.IsNullOrEmpty(_ticket)&&(!string.IsNullOrEmpty(_route)))
                                        {
                                            //更新环节
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
                                    else//登录失败，可能是因为凭据错误，或者需要确认顶号
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
        


        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                //初始化日志
                _logger = new AppLog("CC98");
                await Logger.InitializeAsync();
            }
            catch (Exception ex)
            {
                //弹出
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
            var e= AppInstance.GetActivatedEventArgs();
            if (e.Kind == ActivationKind.Protocol)
            {
                var protocol = (ProtocolActivatedEventArgs)e;
                var query = System.Web.HttpUtility.ParseQueryString(protocol.Uri.Query);
                AuthFromOpenID(query); 
            }
            else
            {
                string IsActive = ValidationHelper.GetValue(Set, "IsActive");
                if (IsActive == "1"||IsActive=="2")//已登录
                {
                    try
                    {
                        var status = await InitializeNetwork();
                        switch (status)
                        {
                            case "1":
                                m_window = new MainWindow();
                                m_window.Closed += M_window_Closed;
                                m_window.Activate();
                                DisplayTrayIcon();
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

        private void M_window_Closed(object sender, WindowEventArgs args)
        {
            if (_trayIcon != null)
            {
                _trayIcon.IsVisible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

     

        private async void AuthFromOpenID(NameValueCollection query)
        {
            string code = ValidationHelper.GetValue(query, "code");
            string iss = ValidationHelper.GetValue(query, "iss");
            string state = ValidationHelper.GetValue(query, "state");
            string session_state = ValidationHelper.GetValue(query, "session_state");
            if (code == "0" || iss == "0" || state == "0" || session_state == "0")
            {
                ShowError("登录失败", "回调参数不完整", "请报告开发者");
                ActivateLogin(0);
                return;
            }
            string state_to_verify = PasswordManager.RetrievePassword("State");
            PasswordManager.RemovePassword("State");
            if (state_to_verify != state)
            {
                ShowError("警告", "返回验证参数不正确", "你可能重复点击了登录按钮，或当前网络环境有风险。");
            }
            string veri = PasswordManager.RetrievePassword("Verifier");
            PasswordManager.ClearAllPasswords("Verifier");
            if (veri != null)
            {
                var result = await LoginService.OAuth(veri, code);
                if (!result.IsSuccess)
                {
                    //
                    ShowError("登录失败", "发生错误", result.Message);
                    ActivateLogin(0);
                    return;
                }
                var token = result.Data;
                if (token == null)
                {
                    //
                    ActivateLogin(0);
                    return;
                }
                if (token.IsValid)
                {
                    InjectToken(token);
                }
                else
                {
                    Set.Values["IsActive"] = "0";
                    ShowError("登录失败", "未取得有效令牌", token.Message);
                    ActivateLogin(0);

                }
               
            }
        }


        private void InjectToken(AuthorizeResult result)
        {
            PasswordManager.SavePassword(result.AccessToken, "Access");
            PasswordManager.SavePassword(result.RefreshToken, "Refresh");
            Set.Values["IsActive"] = "1";
            m_window = new MainWindow();
            m_window.Activate();
        }

        private void DisplayTrayIcon()
        {
            try
            {
                var icon = WindowHelper.GetWindowIcon(m_window);
                uint iconId = 9898;

                // 保留引用，避免被 GC 回收
                _trayIcon = new SystemTrayIcon(iconId, icon, "CC98论坛");
                _trayIcon.LeftClick += (s, e) =>
                {
                    m_window.DispatcherQueue.TryEnqueue(() => {
                        if (!m_window.Visible) m_window.AppWindow.Show();
                        m_window.Activate();
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(m_window.AppWindow.Id);
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
                        m_window.DispatcherQueue.TryEnqueue(() => m_window.Activate());
                    };

                    var exitItem = new MenuFlyoutItem { Text = "退出", Width = 180, Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.ArrowExit } };
                    exitItem.Click += (_, __) =>
                    {
                        LoginService.vpn.Dispose();
                        m_window.DispatcherQueue.TryEnqueue(() =>
                        {
                            m_window.Close();
                            loginpage?.Close();
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
            catch (Exception ex)
            {
                Logger.Write("系统托盘", "注册系统托盘发生错误", ex.Message);
            }
        }
        private void ShowError(string title,string subtitle,string message)
        {
            AppNotification notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(subtitle)
                    .AddText(message)
                    .BuildNotification();
            AppNotificationManager.Default.Show(notification);
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
