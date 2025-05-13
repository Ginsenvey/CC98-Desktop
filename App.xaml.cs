using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public ApplicationDataContainer Set;
        public App()
        {
            this.InitializeComponent();
            Set = ApplicationData.Current.LocalSettings;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (Set.Values.ContainsKey("IsActive"))//已初始化
            {
                if (Set.Values["IsActive"] as string == "1")//已登录
                {
                    m_window = new MainWindow();
                    m_window.Activate();
                }
                else//未登录
                {
                    loginpage = new login();
                    loginpage.Title = "登录";
                    
                    loginpage.Activate();
                }
            }
            else
            {
                
                loginpage = new login();
                loginpage.Title = "登录";
                
                
                loginpage.Activate();
            }
            
        }

        private Window? m_window;
        
        private Window loginpage;
    }
}
