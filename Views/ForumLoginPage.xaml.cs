using CC98.Kernel;
using CC98.Kernel.Authorize;
using CC98.Objects;
using CC98.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ForumLoginPage : Page
    {
        public ForumLoginPage()
        {
            InitializeComponent();
        }
        public static ApiService ApiService => App.Current.GetService<ApiService>();
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
      

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
             
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string userName = UserNameBox.Text;
            string password = PasswordBox.Password;
            if (string.IsNullOrEmpty(userName))
            {
                //
                UserNameBox.PlaceholderText = "用户名和密码不可为空";
                return;
            }
            if(string.IsNullOrEmpty(password))
            {
                PasswordBox.PlaceholderText = "用户名和密码不可为空";
                return;
            }
            VisualStateManager.GoToState(this, "Logging", true);
            await LoginAsync(userName, password);
        }
        private async Task LoginAsync(string userName, string password)
        {
            try
            {
                var loginService = App.Current.GetService<LoginService>();
                var appConfig = App.Current.GetService<AppConfig>();
                var res = await loginService.LoginWithPasswordAsync(userName, password);
                if (res != null && res.IsError)
                {
                    //
                    ErrorBox.Content = res.Error ?? res.ErrorDescription;
                    VisualStateManager.GoToState(this, "Fail", true);
                    return;
                }
                //登录成功
                AppSettings.Current.ActiveMode = (int)ActiveMode.Password;
                AppSettings.Current.IsActive = true;
                await LaunchApp();

            }
            catch (Exception ex)
            {
                ErrorBox.Content = ex.Message;
                VisualStateManager.GoToState(this, "Fail", true);
                //记录异常
            }
        }
        private async Task LaunchApp()
        {
            try
            {
                App.Current.AppMainWindow = new MainWindow();
                App.Current.AppMainWindow.Activate();
                App.Current.LoginWindow.Close();
            }
            catch(Exception ex)
            {
                //记录异常
                Debug.WriteLine($"启动主窗口失败: {ex}");
            }
        }

        private void ErrorBox_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            ErrorBox.Content = "";
        }

        private void OpenIdLoginButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(OpenIdLoginPage));
        }

        private async void CheckNetworkButton_Click(object sender, RoutedEventArgs e)
        {
            var mirrorService = App.Current.GetService<MirrorService>();
            var status = await mirrorService.CheckNetworkAsync();
            ErrorBox.Content= MirrorService.FriendlyStatus(status);
            ErrorBox.IsOpen = true;
        }
    }
}
