using CCkernel;
using CCUserModel;
using DevWinUI;
using HtmlAgilityPack;
using Microsoft.Security.Authentication.OAuth;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.BadgeNotifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;
using Windows.Security.Credentials;
using Windows.Storage;
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
                    Follow.IsChecked = true;
                }
                else if (theme == "1")
                {
                    Light.IsChecked = true;
                }
                else
                {
                    Dark.IsChecked = true;
                }
            }
            else
            {
                Set.Values["Theme"] = "2";
                Follow.IsChecked = true;
            }
            string pic = ValidationHelper.IsTokenExist(Set, "Themepic");
            if (pic!="0")
            {  
                var bitmap = new BitmapImage(new Uri(pic));
                PicPreview.ImageSource = bitmap;
            }
            else
            {
                //这种情况不存在。
            }
            if (ValidationHelper.IsTokenExist(Set,"TitlePage") != "0")
            {
                TitlePage.SelectedIndex = Convert.ToInt32(Set.Values["TitlePage"])-1;
            }
            else
            {
                Set.Values["TitlePage"] = "1";
            }
            string _IsImageVisible = ValidationHelper.IsTokenExist(Set, "IsImageVisible");
            if (_IsImageVisible == "0")
            {
                Set.Values["IsImageVisible"] = "2";//初始化为不显示
                IsImageVisible.IsOn = false;
            }
            else
            {
                if (_IsImageVisible == "1")
                {
                    IsImageVisible.IsOn = true;//1
                }
                else
                {
                    IsImageVisible.IsOn = false;//2
                }
            }
            string _IsTailVisible = ValidationHelper.IsTokenExist(Set, "IsTailVisible");
            if (_IsTailVisible == "0")
            {
                Set.Values["IsTailVisible"] = "2";//初始化为不显示
                TailVisibility.IsOn = false;
            }
            else
            {
                if (_IsTailVisible == "1")
                {
                    TailVisibility.IsOn = true;//1
                }
                else
                {
                    TailVisibility.IsOn = false;//2
                }
            }
            string color = ValidationHelper.IsTokenExist(Set, "BaseColor");
            BaseColorPiker.SelectedItem = BaseColorPiker.Items.First(i => (i as ComboBoxItem).Tag.ToString() == color);
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
                    
                    App.RaiseThemeChanged(ElementTheme.Light);

                }
                else if (theme == "2")
                {
                    
                    App.RaiseThemeChanged(ElementTheme.Dark);

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

        private  void SwitchUser_Click(object sender, RoutedEventArgs e)
        {
            Set.Values.Clear();
            PasswordManager.Logout();
            Application.Current.Exit();
        }
        
      
        private async void DeepAuth_Click(object sender, RoutedEventArgs e)
        {
            AuthDialog.XamlRoot = this.XamlRoot;
            bool isDeepAuthEnabled = false;
            var d = ValidationHelper.IsTokenExist(Set, "DeepAuth");
            if (d!= "0")
            {
                isDeepAuthEnabled = true;//只要值不存在，或者存在但值为0，都返回0.
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

                            PasswordManager.SavePassword(PassBox.Password,"DeepAuth",IdBox.Text);//此密码的用户名为真名，而不是"CC98"
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
                    //关闭对话框
                }
            }
            else
            {
                AuthDialog.Content= "深度认证已启用，无需进行其他操作。";
                await AuthDialog.ShowAsync();
            }
            

            
        }
        
        private  void DrawACard_Click(object sender, RoutedEventArgs e)
        {
            bool IsEnabled = false;
            var d = ValidationHelper.IsTokenExist(Set, "DeepAuth");
            if (d!= "0")
            {
                IsEnabled = true;
                Frame.Navigate(typeof(Game));
            }

            if (IsEnabled == false)
            {
                Flower.PlayAnimation("\uEA39","未深度授权");
            }
        }
        private async void Emoji_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if (h != null)
            {
                var _tag = h.Tag;
                if(_tag is string tag)
                {
                    if (tag == "0")
                    {
                        StorageFolder Folder = ApplicationData.Current.LocalCacheFolder;
                        string path = Folder.Path + "/" + "CustomEmoji.json";
                        string content = ValidationHelper.JsonReader(path);
                        if (!content.StartsWith("10:"))
                        {
                            var package = new DataPackage();
                            package.SetText(content);
                            Clipboard.SetContent(package);

                            Flower.PlayAnimation("\uE930", "已复制到剪贴板");
                        }
                        else
                        {
                            Flower.PlayAnimation("\uEA39", content);
                        }
                    }
                    else if (tag == "1")
                    {
                        var package = Clipboard.GetContent();
                        if (package.Contains(StandardDataFormats.Text))
                        {
                            var text = await package.GetTextAsync();
                            try
                            {
                                var add=JsonConvert.DeserializeObject<List<string>>(text);
                                if (add != null)
                                {
                                    var list = CustomEmoji.GetAllEmoji();
                                    list.AddRange(add);
                                    string content = JsonConvert.SerializeObject(list);
                                    string path = "CustomEmoji.json";
                                    ValidationHelper.JsonWritter(content, path);
                                    Flower.PlayAnimation("\uE930", "载入完成");
                                }
                                else
                                {
                                    Flower.PlayAnimation("\uEA39", "内容无效");
                                }
                                
                            }
                            catch(Exception ex)
                            {
                                Flower.PlayAnimation("\uEA39", ex.Message);
                            }
                        }
                        else
                        {
                            Flower.PlayAnimation("\uEA39", "剪切板中没有文本");
                        }
                    }
                    else
                    {
                        CustomEmoji.ClearAllEmoji();
                        Flower.PlayAnimation("\uE930", "已清空表情");
                    }
                }
            }
        }
        private async void Emoji0_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = new List<string>();
            for(int i= 0; i < 92; i++)
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
                string url = "https://www.cc98.org/static/images/em/em"+param + ".gif";
                string path = "C:\\Users\\Ansherly\\Documents\\Emoji\\" + "em" + param+".gif";
                var fileres = await CCloginservice.vpn.client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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

        private void TitlePage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var c = TitlePage.SelectedIndex;
            if (c != -1)
            {
                Set.Values["TitlePage"] = (c+1).ToString();
             
            }
        }

        private void IsImageVisible_Toggled(object sender, RoutedEventArgs e)
        {
            if(IsImageVisible.IsOn)
            {
                Set.Values["IsImageVisible"] = 1;
            }
            else
            {
                Set.Values["IsImageVisible"] = 2;
            }
        }

        private async void VpnSetup_Click(object sender, RoutedEventArgs e)
        {
            if(ValidationHelper.IsTokenExist(Set, "IsVpnUsable") == "1")
            {
                Flower.PlayAnimation("\uE946", "VPN已配置,无需其他操作");
            }
            else
            {
                AuthDialog.XamlRoot = this.XamlRoot;
                var r = await AuthDialog.ShowAsync();
                if (r == ContentDialogResult.Primary)
                {
                    if (!string.IsNullOrEmpty(IdBox.Text) && !string.IsNullOrEmpty(PassBox.Password))
                    {
                        string vpn_res = await CCloginservice.vpn.LoginAsync(IdBox.Text, PassBox.Password);
                        if (vpn_res == "1")
                        {
                            Set.Values["IsVpnUsable"] = "1";
                            var cookie = CCloginservice.vpn.TWFID;
                            string token = JsonConvert.SerializeObject(cookie);
                            if (!string.IsNullOrEmpty(token))
                            {
                                PasswordManager.SavePassword(token, "TWFID");
                                Flower.PlayAnimation("\uE930", "Cookie已自动保存。");
                            }//保存失败或者token为空时，会出现VPN启用但找不到令牌的情况。
                            else
                            {
                                Flower.PlayAnimation("\uEA39", "Cookie保存失败");
                            }
                            PasswordManager.SavePassword(IdBox.Text, "VpnUserName");
                            PasswordManager.SavePassword(PassBox.Password, "VpnPassWord");


                        }
                        else
                        {
                            Flower.PlayAnimation("\uEA39", vpn_res);
                        }
                    }
                    else
                    {
                        Flower.PlayAnimation("\uEA39", "凭据不完整");
                    }
                }
            }
            
        }

      

        private void BaseColorPiker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox c)
            {
                if(c.SelectedItem is ComboBoxItem i)
                {
                    if(i.Tag is string tag)
                    {
                        Set.Values["BaseColor"] = tag;
                    }
                    
                }
            }
            
        }

        private void TailVisibility_Toggled(object sender, RoutedEventArgs e)
        {
            if (TailVisibility.IsOn)
            {
                Set.Values["IsTailVisible"] = 1;
            }
            else
            {
                Set.Values["IsTailVisible"] = 2;
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
