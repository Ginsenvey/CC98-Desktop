using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using CC98.Share.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MediaViewer : Window
{
    public List<string> Pictures = [];
    public int CurrentIndex = 0;
    private MediaType _mediaType = MediaType.Image;
    public string CurrentUrl=>Pictures[CurrentIndex];
    public int Direction = 0;
    public double CurrentAngle => 90.0 * Direction;
    public float Scale = 1.0f;
    public string ScaleText=>Scale.ToString("P0");
    public MediaViewer(ViewerNavigationInfo info)
    {
        this.InitializeComponent();
        //初始化环境参数
        _mediaType=info.Type;
        CurrentIndex = info.CurrentIndex;
        Pictures.AddRange(info.Urls);
        //初始化UI
        this.Title = "资源预览";
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(GridTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cc98.ico");
        AppWindow.SetIcon(iconPath);
        AppWindow.SetTaskbarIcon(iconPath);
        this.SystemBackdrop=new MicaBackdrop();
        Activated += MediaViewer_Activated;
        this.Closed += MediaViewer_Closed;
    }

    private void LoadImage()
    {
        Direction = 0;
        ImageTransform.Angle = CurrentAngle;
        Scale = 1.0f;
        ScaleImage();
        this.InnerImage.Src = CurrentUrl;
        MediaInfo.Text = CurrentUrl;
        Posi.Text = $"{CurrentIndex + 1} / {Pictures.Count}";
    }
       
    private async void MediaViewer_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        switch (_mediaType)
        {
            case MediaType.Image:
                VideoPlayer.Visibility = Visibility.Collapsed;
                LoadImage();
                break;
            case MediaType.Video:
                VideoPlayer.Visibility = Visibility.Visible;
                Grid.SetRow(VideoPlayer, 1);
                Grid.SetRowSpan(VideoPlayer, 2);
                var source = await LoginService.Vpn.GetSourceAsync(CurrentUrl);
                if (source == null) return;
                VideoPlayer.Source = source;
                break;
        }     
    }

       
        
    private void RotateImage()
    {
        Direction = (Direction == 3) ? 0 : Direction + 1;
        ImageTransform.Angle = CurrentAngle;
    }

    private void Rotate_Click(object sender, RoutedEventArgs e)
    {
        RotateImage();
    }

    private void MediaViewer_Closed(object sender, WindowEventArgs e)
    {
        // 释放资源示例
        VideoPlayer.Source = null;
        InnerImage.Src = "";
    }
    private void ScaleImage()
    {
        Viewer.ZoomToFactor(Scale);
        zoomfactor.Text=ScaleText;
    }
       

    private void zoomout_Click(object sender, RoutedEventArgs e)
    {
        if (Scale <= 0.1f) return;
        Scale -= 0.1f;
        ScaleImage();
    }

    private void zoomin_Click(object sender, RoutedEventArgs e)
    {
        if (Scale >= 4.0f) return;
        Scale += 0.1f;
        ScaleImage();
    }

    private async void CopyPic_Click(object sender, RoutedEventArgs e)
    {
        var r=await ImageExtension.CopyImageToClipboardAsync(CurrentUrl);
        if (r)
        {
            Flower.Play(FlowStatus.Success, "已复制图片到剪贴板");
        }
        else
        {
            Flower.Play(FlowStatus.Fail, "复制失败");
        }
    }

    private void last_Click(object sender, RoutedEventArgs e)
    {
        CurrentIndex=(CurrentIndex==0)?Pictures.Count-1:CurrentIndex-1;
        LoadImage();
    }

    private void next_Click(object sender, RoutedEventArgs e)
    {
        CurrentIndex=(CurrentIndex==Pictures.Count-1)?0:CurrentIndex+1;
        LoadImage();
    }

    private void Viewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        Scale=Viewer.ZoomFactor;
        zoomfactor.Text = ScaleText;
    }

    private async void SavePic_Click(object sender, RoutedEventArgs e)
    {
        var r=await ImageExtension.DownloadImagesAsync(CurrentUrl);
        if (r == null)
        {
            ShowTip("保存图片出错", "未知原因");
            return;
        }
        ShowTip("保存图片成功", r);
    }
    private void ShowTip(string title,string subTitle)
    {
        Tip.Title = title;
        Tip.Subtitle = subTitle;
        Tip.IsOpen = true;
    }
}