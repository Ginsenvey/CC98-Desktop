using CCkernel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

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
        public login()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();
            this.ExtendsContentIntoTitleBar= true;
            this.SetTitleBar(GridTitleBar);
            this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            Set = ApplicationData.Current.LocalSettings;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(idbox.Text) && (!string.IsNullOrEmpty(passbox.Password)))
            {
                Auth();
            }
        }
        private async void Auth()
        {
            
            string ResText = await CCloginservice.LoginAsync(idbox.Text,passbox.Password);
            if (ResText.Contains("access_token"))
            {
                try
                {
                    var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(ResText);
                    string access = js["access_token"] as string;
                    string token_type = js["token_type"] as string;
                    int expire = Convert.ToInt16(js["expires_in"]);
                    string refresh = js["refresh_token"] as string;
                    Set.Values["Access"] = access;
                    Set.Values["Refresh"] = refresh;
                    Set.Values["IsActive"] = "1";
                    this.Close();
                    await RestartApplicationAsync();

                }
                catch (Exception e)
                {
                    Set.Values["IsActive"] = "0";
                    
                }
            }
            else
            {
                Set.Values["IsActive"] = "0";
            }
        }
        
        private async Task RestartApplicationAsync()
        {
            try
            {
                // 获取当前应用的可执行文件路径
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                // 退出当前应用
                Application.Current.Exit();

                // 重新启动应用
                Process.Start(exePath);
            }
            catch (Exception ex)
            {
                // 捕获异常，处理错误
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"An error occurred while trying to restart the application: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }
    }
}
