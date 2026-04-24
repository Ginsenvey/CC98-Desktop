using CC98.Kernel;
using DevWinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using Windows.Storage;
using CC98.Services;
using CC98.Objects;
using Microsoft.Windows.AppLifecycle;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Setting : Page
{
    public ObservableCollection<ThemePicture> Pics = [];
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
            var effect = (string)Set.Values["Effect"];
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
            var theme = (string)Set.Values["Theme"];
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
        var pic = ValidationHelper.GetValue(Set, "Themepic");
        if (pic!="0")
        {  
            var bitmap = new BitmapImage(new(pic));
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
            
        var isTailVisible = ValidationHelper.GetValue(Set, "IsTailVisible");
        if (isTailVisible == "0")
        {
            Set.Values["IsTailVisible"] = "2";//初始化为不显示
            TailVisibility.IsOn = false;
        }
        else
        {
            if (isTailVisible == "1")
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
        //指向开发记录楼
        var param = new TopicNavigationInfo
        {
            IsJumpingMode = false,
            TopicId = 6173309
        };
        Frame.Navigate(typeof(Topic), param);
    }

        

    private void Effect_Checked(object sender, RoutedEventArgs e)
    {
        var button= (RadioButton)sender;
        if (button == null) return;
        var tag=button.Tag;
        if(tag is not string effect) return;
        Set.Values["Effect"]=effect;
        if (effect == EffectHistory) return;//当选项与原设置不同时，才进行设置。
        EffectHistory =effect;
        switch (effect)
        {
            case "0":
                Mica.IsChecked = true;
                App.Current.AppMainWindow.SystemBackdrop = new MicaSystemBackdrop();
                break;
            case "1":
                MicaAlt.IsChecked = true;
                App.Current.AppMainWindow.SystemBackdrop = new MicaSystemBackdrop(MicaKind.BaseAlt);
                break;
            case "2":
                AcrylicBase.IsChecked = true;
                App.Current.AppMainWindow.SystemBackdrop = new AcrylicSystemBackdrop();
                break;
            case "3":
                AcrylicThin.IsChecked = true;
                App.Current.AppMainWindow.SystemBackdrop = new AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
                break;
            default:
                Mica.IsChecked = true;
                App.Current.AppMainWindow.SystemBackdrop = new MicaSystemBackdrop();
                break;
        }

    }

    private void Light_Checked(object sender, RoutedEventArgs e)
    {
        if (((RadioButton)sender)?.Tag is not string theme) return;
        Set.Values["Theme"] = theme;
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

        

    private  void LoadPics()
    {
            
        var themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Themes");
        var files = Directory.GetFiles(themesPath, "*.jpg", SearchOption.AllDirectories);
        Pics.Clear();
        foreach ( var file in files)
        {  
            var filename = Path.GetFileName(file);
            Pics.Add(new() { FileName = filename, FilePath = file });  
        }
        ThemesGrid.ItemsSource = Pics;
    }

    private void ThemesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemesGrid.SelectedItem!=null)
        {
            var selected = Pics[ThemesGrid.SelectedIndex];
            var bitmap=new BitmapImage(new(selected.FilePath));
            PicPreview.ImageSource = bitmap;
            Set.Values["ThemePic"] = selected.FilePath;
        }
            

    }

       

    private void LocalSetManager_Expanded(object sender, EventArgs e)
    {
        var settings = "";
        foreach (var item in Set.Values)
        {
            //不显示收藏夹，美化打印太长
            if (item.Key == "Favorites") continue;
            settings += $"{item.Key}:{item.Value}\n";
        }
        LocalSet.Text = settings;

        LocalSetManager.IsExpanded = true;
    }

    private  void SwitchUser_Click(object sender, RoutedEventArgs e)
    {
        //登出：清理设置，清理密码，退出应用
        Set.Values.Clear();
        PasswordManager.Logout();
        //重启应用
        AppInstance.Restart("");
    }
        

    private void TitlePage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var c = TitlePage.SelectedIndex;
        if (c != -1)
        {
            Set.Values["TitlePage"] = (c+1).ToString();
        }
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
public class ThemePicture
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
}