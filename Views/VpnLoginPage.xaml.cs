using CC98.Kernel;
using CC98.Kernel.Authorize;
using CC98.Kernel.Network;
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class VpnLoginPage : Page
    {
        public VpnLoginPage()
        {
            InitializeComponent();
        }

        private void ErrorBox_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            ErrorBox.Subtitle = "";
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
            if (string.IsNullOrEmpty(password))
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
                var vpnService = App.Current.GetService<IVpnService>();
                var res = await vpnService.LoginAsync(userName, password);
                if (res == null || !res.IsSuccess)
                {
                    //
                    ErrorBox.Subtitle = res?.Message;
                    VisualStateManager.GoToState(this, "Fail", true);
                    return;
                }
                //登录成功
                ;

            }
            catch (Exception ex)
            {
                ErrorBox.Subtitle = ex.Message;
                VisualStateManager.GoToState(this, "Fail", true);
                //记录异常
            }
        }
    }
}
