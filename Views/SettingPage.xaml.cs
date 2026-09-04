using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using DevWinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     设置页面。
/// </summary>
public sealed partial class SettingPage : Page
{
    public string EffectHistory = "";
    public ObservableCollection<ThemePicture> Pics = [];
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;

    public SettingPage()
    {
        InitializeComponent();
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
                Follow.IsChecked = true;
            else if (theme == "1")
                Light.IsChecked = true;
            else
                Dark.IsChecked = true;
        }
        else
        {
            Set.Values["Theme"] = "2";
            Follow.IsChecked = true;
        }

       

        
    }

    private void ToFeedBack_Click(object sender, RoutedEventArgs e)
    {
        //指向开发记录楼
        var param = new TopicNavigationInfo
        {
            IsJumpingMode = false,
            TopicId = 6173309
        };
        Frame.Navigate(typeof(TopicPage), param);
    }


    private void Effect_Checked(object sender, RoutedEventArgs e)
    {
        var button = (RadioButton)sender;
        if (button == null) return;
        var tag = button.Tag;
        if (tag is not string effect) return;
        Set.Values["Effect"] = effect;
        if (effect == EffectHistory) return; //当选项与原设置不同时，才进行设置。
        EffectHistory = effect;
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
            App.RaiseThemeChanged(ElementTheme.Light);
        else if (theme == "2")
            App.RaiseThemeChanged(ElementTheme.Dark);
        else
            App.RaiseThemeChanged(ElementTheme.Default);
    }


    private void LoadPics()
    {
        var themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Themes");
        var files = Directory.GetFiles(themesPath, "*.jpg", SearchOption.AllDirectories);
        Pics.Clear();
        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            Pics.Add(new() { FileName = filename, FilePath = file });
        }

        ThemesGrid.ItemsSource = Pics;
    }

    private void ThemesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemesGrid.SelectedItem != null)
        {
            var selected = Pics[ThemesGrid.SelectedIndex];
            //预览尺寸很小,按显示宽度 2 倍解码即可,避免整张 1080p/4K 原图解码
            var bitmap = new BitmapImage(new(selected.FilePath)) { DecodePixelWidth = 120 };
            PicPreview.ImageSource = bitmap;
            AppSettings.Current.ThemePicture = selected.FilePath;
        }
    }


    private void LocalSetManager_Expanded(object sender, EventArgs e)
    {
        var settings = "";
        foreach (var item in Set.Values)
        {
            //不显示收藏夹，美化打印太长
            if (item.Key == "FavoriteGroups") continue;
            settings += $"{item.Key}:{item.Value}\n";
        }

        LocalSet.Text = settings;

        LocalSetManager.IsExpanded = true;
    }

    private async void SwitchUser_Click(object sender, RoutedEventArgs e)
    {
        AppSettings.Current.IsActive = false;
        await ClearAppCacheAsync();
        PasswordManager.Logout();
        //重启应用
        AppInstance.Restart("");
    }
    /// <summary>
    /// 清理应用缓存，包括本地缓存文件夹中的所有文件和子文件夹。防止生成多份头像。
    /// </summary>
    /// <returns></returns>
    public async Task ClearAppCacheAsync()
    {
        try
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            var items = await localCacheFolder.GetItemsAsync();
            foreach (var item in items)
            {
                if (item is StorageFile file)
                {
                    await file.DeleteAsync();
                }
                else if (item is StorageFolder folder)
                {
                    await folder.DeleteAsync();
                }
            }

            Debug.WriteLine("应用缓存已成功清理。");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"清理缓存时发生错误: {ex.Message}");
            // 处理异常，例如文件正在使用中
        }
    }


}

public class ThemePicture
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";

    /// <summary>
    /// 低分辨率缩略图:只按显示宽度 2 倍解码,避免把整张壁纸原图解码后缩放到 60x35。
    /// </summary>
    public BitmapImage Thumb => new(new Uri(FilePath)) { DecodePixelWidth = 120 };
}