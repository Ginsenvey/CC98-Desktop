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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            if(Set.Values.ContainsKey("ThemePic"))
            {
                if (Set.Values["ThemePic"] as string != "0")
                {
                    string pic = (string)Set.Values["ThemePic"];
                    var bitmap = new BitmapImage(new Uri(pic));
                    PicPreview.ImageSource = bitmap;
                }
               
            }
            else
            {
                Set.Values["ThemePic"] = "0";
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

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            de.Text = "tr";
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
    }
    public class Pic
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
