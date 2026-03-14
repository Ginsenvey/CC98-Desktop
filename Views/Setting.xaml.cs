using CC98.Kernel;
using CC98.Kernel.UserExperience;
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
using System.Text.Json;
using CC98.Services;
using CC98.Services.Extensions;
using CC98.Objects;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
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
            string pic = ValidationHelper.GetValue(Set, "Themepic");
            if (pic!="0")
            {  
                var bitmap = new BitmapImage(new Uri(pic));
                PicPreview.ImageSource = bitmap;
            }
            else
            {
                //这种情况不存在。
            }
            if (ValidationHelper.GetValue(Set,"TitlePage") != "0")
            {
                TitlePage.SelectedIndex = Convert.ToInt32(Set.Values["TitlePage"])-1;
            }
            else
            {
                Set.Values["TitlePage"] = "1";
            }
            string _IsImageVisible = ValidationHelper.GetValue(Set, "IsImageVisible");
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
            string _IsTailVisible = ValidationHelper.GetValue(Set, "IsTailVisible");
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
            var showBigPaper = ValidationHelper.GetValue(Set, "ShowBigPaper");
            if (showBigPaper == "0")
            {
                //赋予默认值：打开
                showBigPaper = "1";
                Set.Values["ShowBigPaper"] = "1";
            }
            ShowBigPaper.IsOn = showBigPaper == "1";
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
                        App.Current.m_window.SystemBackdrop = new MicaSystemBackdrop();
                        break;
                    case "1":
                        MicaAlt.IsChecked = true;
                        App.Current.m_window.SystemBackdrop = new MicaSystemBackdrop(MicaKind.BaseAlt);
                        break;
                    case "2":
                        AcrylicBase.IsChecked = true;
                        App.Current.m_window.SystemBackdrop = new AcrylicSystemBackdrop();
                        break;
                    case "3":
                        AcrylicThin.IsChecked = true;
                        App.Current.m_window.SystemBackdrop = new AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
                        break;
                    default:
                        Mica.IsChecked = true;
                        App.Current.m_window.SystemBackdrop = new MicaSystemBackdrop();
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
            string settings = "";
            foreach (var item in Set.Values)
            {
                settings += $"{item.Key}:{item.Value}\n";
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
        
      
        
        
        
        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            CustomEmoji.ClearAllEmoji();
            Flower.Play(Objects.FlowStatus.Success, "已清空表情");
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
            Set.Values["IsImageVisible"] = IsImageVisible.IsOn?1:2;
        }


        private void TailVisibility_Toggled(object sender, RoutedEventArgs e)
        {
            Set.Values["IsTailVisible"] = TailVisibility.IsOn?1:2;
        }

        private async void ExportLog_Click(object sender, RoutedEventArgs e)
        {
            var r= await App.Logger.SaveToDesktopAsync();
            if (r.Success)
            {
                Flower.Play(FlowStatus.Success, "导出日志成功");
            }
            else
            {
                Flower.Play(FlowStatus.Fail, "导出失败");
            }
        }

        private void ShowBigPaper_Toggled(object sender, RoutedEventArgs e)
        {
            Set.Values["ShowBigPaper"] = ShowBigPaper.IsOn?1:2;
        }
    }
    public class Pic
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
    
}
