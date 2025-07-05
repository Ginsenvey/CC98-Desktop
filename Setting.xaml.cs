using DevWinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using CCkernel;
using Windows.Security.Credentials;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;
using Microsoft.Security.Authentication.OAuth;
using HtmlAgilityPack;
using System.Net.Http;
using Windows.Media.Protection.PlayReady;
using System.Reflection.Emit;
using System.Net;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Setting : Page
    {
        public ObservableCollection<Pic> pics = new ObservableCollection<Pic>();
        public Setting()
        {
            this.InitializeComponent();
            LoadSettings();
            LoadPics();
        }
        //第一次进入时，初始化设置项。
        //如果项存在且有值，为选项赋值。
        private void LoadSettings()
        {
            if (Set.Values.ContainsKey("Effect"))
            {
                string effect = (string)Set.Values["Effect"];
                EffectHistory = effect;
                switch (effect)
                {
                    case "0":
                        Mica.IsChecked = true;
                        
                        break;
                    case "1":
                        MicaAlt.IsChecked = true;
                        
                        break;
                    case "2":
                        AcrylicBase.IsChecked = true;
                        
                        break;
                    case "3":
                        AcrylicThin.IsChecked = true;
                        
                        break;

                    default:
                        Mica.IsChecked = true;
                        
                        break;
                }
            }
            else
            {
                Set.Values["Effect"] = "0";
                EffectHistory = "0";
                Mica.IsChecked = true;
            }
            if (Set.Values.ContainsKey("Theme"))
            {
                string theme = (string)Set.Values["Theme"];
                if (theme == "0")
                {
                    Light.IsChecked = true;
                }
                else if (theme == "1")
                {
                    Dark.IsChecked = true;
                }
                else
                {
                    Follow.IsChecked = true;
                }
            }
            else
            {
                Set.Values["Theme"] = "2";
                Follow.IsChecked = true;
            }
            if(ValidationHelper.IsTokenExist(Set,"Themepic"))
            {
                string pic = (string)Set.Values["ThemePic"];
                var bitmap = new BitmapImage(new Uri(pic));
                PicPreview.ImageSource = bitmap;
            }
            else
            {
                //这种情况不存在。
            }
        }
        public string EffectHistory = "";
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        private void ToFeedBack_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Topic), "6173309");
        }

        

        private void Effect_Checked(object sender, RoutedEventArgs e)
        {
            
            Set.Values["Effect"]= ((RadioButton)sender).Tag.ToString();
            if (((RadioButton)sender).Tag.ToString() != EffectHistory)//当选项与原设置不同时，才进行设置。
            {
                EffectHistory = ((RadioButton)sender).Tag.ToString();
                switch (((RadioButton)sender).Tag.ToString())
                {
                    case "0":
                        Mica.IsChecked = true;
                        (App.Current as App).m_window.SystemBackdrop = new MicaSystemBackdrop();
                        break;
                    case "1":
                        MicaAlt.IsChecked = true;
                        (App.Current as App).m_window.SystemBackdrop = new MicaSystemBackdrop(MicaKind.BaseAlt);
                        break;
                    case "2":
                        AcrylicBase.IsChecked = true;
                        (App.Current as App).m_window.SystemBackdrop = new AcrylicSystemBackdrop();
                        break;
                    case "3":
                        AcrylicThin.IsChecked = true;
                        (App.Current as App).m_window.SystemBackdrop = new AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
                        break;

                    default:
                        Mica.IsChecked = true;
                        (App.Current as App).m_window.SystemBackdrop = new MicaSystemBackdrop();
                        break;
                }
            }
            else
            {

            }
            
        }

        private void Light_Checked(object sender, RoutedEventArgs e)
        {
            string theme = ((RadioButton)sender).Tag.ToString();
            Set.Values["Theme"] = theme;
            try
            {
                if (theme == "1")
                {
                    
                    App.RaiseThemeChanged(ElementTheme.Dark);

                }
                else if (theme == "0")
                {
                    
                    App.RaiseThemeChanged(ElementTheme.Light);

                }
                else
                {
                    
                    App.RaiseThemeChanged(ElementTheme.Default);
                }
            }
            catch { }
            ;
        }

        

        private  void LoadPics()
        {
            
            string themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Themes");
            var Files = Directory.GetFiles(themesPath, "*.jpg", SearchOption.AllDirectories);
            pics.Clear();
            foreach ( var file in Files)
            {  
                string filename = Path.GetFileName(file);
                pics.Add(new Pic{ FileName = filename, FilePath = file });  
            }
            ThemesGrid.ItemsSource = pics;
        }

        private void ThemesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemesGrid.SelectedItem!=null)
            {
                var selected = pics[ThemesGrid.SelectedIndex];
                var bitmap=new BitmapImage(new Uri(selected.FilePath));
                PicPreview.ImageSource = bitmap;
                Set.Values["ThemePic"] = selected.FilePath;
            }
            

        }

        private void ForceGC_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            
        }

        private void ViewLocalSet_Click(object sender, RoutedEventArgs e)
        {
            string settings = "配置项:\n";
            foreach (var item in Set.Values)
            {
                settings += $"{item.Key}: {item.Value}\n";
            }
            LocalSet.Text = settings;
            LocalSetManager.IsExpanded = true;
        }

        private void LocalSetManager_Expanded(object sender, EventArgs e)
        {
            string settings = "配置项:\n";
            foreach (var item in Set.Values)
            {
                settings += $"{item.Key}: {item.Value}\n";
            }
            LocalSet.Text = settings;
            LocalSetManager.IsExpanded = true;
        }

        private async void SwitchUser_Click(object sender, RoutedEventArgs e)
        {
            Set.Values.Clear();
            await RestartApplicationAsync();
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
                    Content = $"重新启动应用失败: {ex.Message}",
                    CloseButtonText = "OK"
                };
                dialog.XamlRoot = this.XamlRoot;
                await dialog.ShowAsync();
            }
        }
        private static PasswordVault vault = new PasswordVault();
        private async void DeepAuth_Click(object sender, RoutedEventArgs e)
        {
            AuthDialog.XamlRoot = this.XamlRoot;
            bool isDeepAuthEnabled = false;
            if (Set.Values.ContainsKey("DeepAuth"))
            {
                if (Set.Values["DeepAuth"] as string=="1")
                {
                    isDeepAuthEnabled = true;
                }
            }
            if (isDeepAuthEnabled == false)
            {
                var r = await AuthDialog.ShowAsync();
                if (r == ContentDialogResult.Primary)
                {
                    if (IdBox.Text != "" && PassBox.Password != "")
                    {
                        
                        string DeepAuthRes = await CCloginservice.DeepAuthService(IdBox.Text, PassBox.Password);
                        if (DeepAuthRes == "0")
                        {
                            ContentDialog dialog = new ContentDialog
                            {
                                Title = "登录失败",
                                Content = "发生错误。检查凭据或者报告此问题",
                                CloseButtonText = "退出"
                            };
                            dialog.XamlRoot = this.XamlRoot;
                            await dialog.ShowAsync();
                        }
                        else
                        {

                            PasswordCredential credential = new PasswordCredential("CC98", "User", PassBox.Password);
                            vault.Add(credential);
                            Set.Values["DeepAuth"] = "1";//深度认证已启用
                            AppNotification notification = new AppNotificationBuilder()
        .AddText("登录成功！")
        .AddText("现在，可以使用抽卡等实验性功能。")
        .BuildNotification();

                            AppNotificationManager.Default.Show(notification);
                        }
                    }
                }
                else
                {
                    
                }
            }
            else
            {
                AuthDialog.Content= "深度认证已启用，无需进行其他操作。";
                await AuthDialog.ShowAsync();
            }
            

            
        }
        
        private async void DrawACard_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Game));
            
        }

        private async void Emoji_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = new List<string>();
            for(int i= 0; i < 55; i++)
            {
                string param = "";
                if (i < 10)
                {
                    param="0"+i.ToString();
                }
                else
                {
                    param=i.ToString();
                }
                list.Add(param);

                
            }
            
            foreach (string param in list)
            {
                string url = "https://www.cc98.org/static/images/ms/ms"+param + ".png";
                string path = "C:\\Users\\Ansherly\\Documents\\Emoji\\" + "ms" + param+".png";
                var fileres = await CCloginservice.client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (fileres.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream contentStream = await fileres.Content.ReadAsStreamAsync(),
                    fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await contentStream.CopyToAsync(fileStream);

                    }
                }
            }
        }
    }
    public class Pic
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
    public class User
    {
        public string Name { get; set; }
        public string Portrait { get; set; }
        
    }
}
