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
        public Window m_window { get; set; }
        private Window loginPage;
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
        

        
        private async void InitializeNetwork()
        {
            var network_status = await LoginService.vpn.CheckNetwork(false);
            if (network_status == NetworkStatus.InCampus)
            {
                //启动
                await StartUp();
                return;
            }
            if (network_status == NetworkStatus.NotInCampus)//在校外
            {
                await Logger.WriteAsync("App", "初始化网络", "检测VPN可用性");
                if (ValidationHelper.GetValue(Set, "IsVpnUsable") != "1")
                {
                    //打开VPN配置设置
                    await Logger.WriteAsync("App", "初始化网络", "未配置VPN,跳转登录");
                    ActivateLogin(1);
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
                var new_status = await LoginService.vpn.CheckNetwork(true);
                await Logger.WriteAsync("App", "初始化网络", $"新的网络状态为：{new_status.ToString()}");
                if (new_status == NetworkStatus.ByVPN)
                {
                    LoginService.vpn.Logined = true;
                    LoginService.vpn.IsVpnEnabled = true;
                    await StartUp();
                    //启动
                    return;
                }
                bool success = await ReloginVPN();
                await Logger.WriteAsync("App", "初始化网络", success ? "重连成功": "重连失败");
                if (success)
                {
                    LoginService.vpn.IsVpnEnabled = true;
                    SaveToken();
                    //此时vpn应该可用
                    await StartUp();
                }
            }
            if (network_status == NetworkStatus.MirrorError)
            {
                ShowError("出错", "连接镜像站失败", "日志已记录");
            }
            if (network_status == NetworkStatus.UnknownError)
            {
                ShowError("出错", "IP可能被镜像站拦截", "日志已记录");
            }
            if (network_status == NetworkStatus.NoConnection)
            {
                ShowError("出错", "无互联网连接", "日志已记录");
            }
            return;
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
            if(res.Status==VPNLoginStatus.Success)return true;
            if (res.Status == VPNLoginStatus.NeedConfirm)
            {
                var confirm_res = await LoginService.vpn.Confirm();
                if (confirm_res.Status == VPNLoginStatus.Success)
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
            if (res.Status == VPNLoginStatus.NeedCaptcha||res.Status==VPNLoginStatus.Error)
            {
                //报错
                return false;
            }
            return false;
        }

        private bool SaveToken()
        {
            var new_ticket = LoginService.vpn.Ticket;
            var new_route = LoginService.vpn.Route;
            string _ticket = new_ticket.Value;
            string _route = new_route.Value;
            if (string.IsNullOrEmpty(_ticket) || (string.IsNullOrEmpty(_route)))
            {
                //报错
                ShowError("VPN凭据不完整");
                return false;
            }
            
            //更新环节
            PasswordManager.SavePassword(_ticket, "Ticket");
            PasswordManager.SavePassword(_route, "Route");
            return true;
            //保存失败或者token为空时，会出现VPN启用但找不到令牌的情况。
        }
        protected override  async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            await InitializeAppLog();
            var e= AppInstance.GetActivatedEventArgs();
            if (e.Kind == ActivationKind.Protocol)
            {
                AuthFromOpenID(e);
                return;
            }
            string IsActive = ValidationHelper.GetValue(Set, "IsActive");
            if (IsActive =="0")
            {
                ActivateLogin(0);
            }
            else //未登录
            {
                try
                {
                    InitializeNetwork();
                }
                catch (Exception ex)
                {
                    //报错
                    ShowError(ex.Message);
                }
            }


        }

        private async Task InitializeAppLog()
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
            AppDomain.CurrentDomain.UnhandledException += async (s, e) =>
            {
                await Logger.WriteAsync("全局异常捕获", $"未处理异常: {e.ExceptionObject}");
            };

            
        }
        private async Task StartUp()
        {
            try
            {
                //必须在构造函数前加上异常处理
                m_window = new MainWindow();
                await Logger.WriteAsync("App", "应用主窗口启动");
                m_window.Closed += M_window_Closed;
                m_window.Activate();
                DisplayTrayIcon();
            }
            catch (Exception ex)
            {
                await Logger.WriteAsync("App", "主窗口启动出错",ex.Message);
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

     

        private async void AuthFromOpenID(IActivatedEventArgs e)
        {
            var protocol = (ProtocolActivatedEventArgs)e;
            var query = System.Web.HttpUtility.ParseQueryString(protocol.Uri.Query);
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
                    InjectTokenFromAuth(token);
                }
                else
                {
                    Set.Values["IsActive"] = "0";
                    ShowError("登录失败", "未取得有效令牌", token.Message);
                    ActivateLogin(0);
                }
               
            }
        }
        

        private void InjectTokenFromAuth(AuthorizeResult result)
        {
            PasswordManager.SavePassword(result.AccessToken, "Access");
            PasswordManager.SavePassword(result.RefreshToken, "Refresh");
            Set.Values["IsActive"] = "1";
            m_window = new MainWindow();
            m_window.Activate();
        }

        private void DisplayTrayIcon()
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
                        loginPage?.Close();
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
        private void ShowError(string title,string subtitle="",string message="")
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
            loginPage = new Login(mode);
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(loginPage);
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
            loginPage.Title = "登录";
            loginPage.Activate();
        }
        
    }
}
