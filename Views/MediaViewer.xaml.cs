using System;
using System.Collections.Generic;
using System.IO;
using CC98.Controls.Picture;
using CC98.Controls.Primitives;
using CC98.Kernel.Authorize;
using CC98.Objects;
using CC98.Services.Extensions;
using CC98.Services.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MediaViewer : Window
{
    public List<string> Pictures = [];
    public int CurrentIndex = 0;
    public string CurrentUrl=>Pictures[CurrentIndex];
    public int Direction = 0;
    public double CurrentAngle => 90.0 * Direction;
    public float Scale = 1.0f;
    public string ScaleText=>Scale.ToString("P0");
    public MediaViewer(ViewerNavigationInfo info)
    {
        InitializeComponent();
        //初始化环境参数(仅支持图片预览,视频预览功能已移除)
        CurrentIndex = info.CurrentIndex;
        Pictures.AddRange(info.Urls);
        //初始化UI
        Title = "资源预览";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(GridTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cc98.ico");
        AppWindow.SetIcon(iconPath);
        AppWindow.SetTaskbarIcon(iconPath);
        SystemBackdrop=new MicaBackdrop();
        Activated += MediaViewer_Activated;
        Closed += MediaViewer_Closed;
    }

    private void LoadImage()
    {
        Direction = 0;
        ImageTransform.Angle = CurrentAngle;
        // 等待新图解码完成后自动适配视口,期间不缩放(保持上次状态直到适配)
        _autoFitPending = true;
        InnerImage.Src = CurrentUrl;
        MediaInfo.Text = CurrentUrl;
        Posi.Text = $"{CurrentIndex + 1} / {Pictures.Count}";
    }
    // 只在首次激活时加载,避免每次窗口重新获得焦点都重置用户的缩放/旋转状态
    private bool _initialized;
    private void MediaViewer_Activated(object sender, WindowActivatedEventArgs args)
    {
        // 仅支持图片预览(视频预览功能已移除)
        if (_initialized) return;
        _initialized = true;
        LoadImage();
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
        var r=await Downloader.CopyImageToClipboardAsync(CurrentUrl);
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

    // 是否等待本次加载的图片解码完成后自动适配视口
    private bool _autoFitPending;

    /// <summary>
    /// 图片解码完成后回调:计算并应用适配因子,保证用户无需手动缩放即可看到全貌。
    /// </summary>
    private void InnerImage_ImageOpened(object sender, PictureImageOpenedEventArgs e)
    {
        if (!_autoFitPending) return;
        _autoFitPending = false;
        FitToViewport(e.PixelWidth, e.PixelHeight);
    }

    /// <summary>
    /// 计算适配因子并缩放:图片宽被 Uniform 约束为视口宽,显示高 = 视口宽/宽高比,
    /// 完整显示所需缩放 = min(1, 视口高/显示高)。只缩不放大,小图保持 100%。
    /// </summary>
    private void FitToViewport(int imgW, int imgH)
    {
        var vw = Viewer.ViewportWidth;
        var vh = Viewer.ViewportHeight;
        if (vw <= 0 || vh <= 0 || imgW <= 0 || imgH <= 0)
        {
            // 视口或图片尺寸未知,回退 100%
            Scale = 1.0f;
            ScaleImage();
            return;
        }

        // 宽高比使用像素比例(与 DIP 比例一致,无需 DPI 换算)
        var aspect = (double)imgW / imgH;
        var displayedHeight = vw / aspect;
        var fit = vh / displayedHeight;
        if (fit >= 1.0) fit = 1.0; // 不放大
        Scale = (float)fit;
        ScaleImage();
    }

    private void Viewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        Scale = Viewer.ZoomFactor;
        zoomfactor.Text = ScaleText;
    }

    private async void SavePic_Click(object sender, RoutedEventArgs e)
    {
        var r=await Downloader.DownloadFileAsync(CurrentUrl);
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