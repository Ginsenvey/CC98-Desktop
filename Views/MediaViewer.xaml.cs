using CC98.Kernel;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using CC98.Share.Controls;
using CC98.Share.Controls.Primitives;
using DevWinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Input;
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class MediaViewer : Window
    {
        public List<string> pictures = [];
        public int currentIndex = 0;
        private MediaType mediaType = MediaType.Image;
        public string CurrentUrl=>pictures[currentIndex];
        public int direction = 0;
        public double CurrentAngle => 90.0 * direction;
        public float scale = 1.0f;
        public string ScaleText=>scale.ToString("P0");
        public MediaViewer(ViewerNavigationInfo info)
        {
            this.InitializeComponent();
            //初始化环境参数
            mediaType=info.Type;
            currentIndex = info.CurrentIndex;
            pictures.AddRange(info.Urls);
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
            direction = 0;
            ImageTransform.Angle = CurrentAngle;
            scale = 1.0f;
            ScaleImage();
            this.InnerImage.Src = CurrentUrl;
            MediaInfo.Text = CurrentUrl;
            Posi.Text = $"{currentIndex + 1} / {pictures.Count}";
        }
       
        private async void MediaViewer_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            switch (mediaType)
            {
                case MediaType.Image:
                    VideoPlayer.Visibility = Visibility.Collapsed;
                    LoadImage();
                    break;
                case MediaType.Video:
                    VideoPlayer.Visibility = Visibility.Visible;
                    Grid.SetRow(VideoPlayer, 1);
                    Grid.SetRowSpan(VideoPlayer, 2);
                    var source = await LoginService.vpn.GetSourceAsync(CurrentUrl);
                    if (source == null) return;
                    VideoPlayer.Source = source;
                    break;
            }     
        }

       
        
        private void RotateImage()
        {
            direction = (direction == 3) ? 0 : direction + 1;
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
            Viewer.ZoomToFactor(scale);
            zoomfactor.Text=ScaleText;
        }
       

        private void zoomout_Click(object sender, RoutedEventArgs e)
        {
            if (scale <= 0.1f) return;
            scale -= 0.1f;
            ScaleImage();
        }

        private void zoomin_Click(object sender, RoutedEventArgs e)
        {
            if (scale >= 4.0f) return;
            scale += 0.1f;
            ScaleImage();
        }

        private async void CopyPic_Click(object sender, RoutedEventArgs e)
        {
            bool r=await ImageExtension.CopyImageToClipboardAsync(CurrentUrl);
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
            currentIndex=(currentIndex==0)?pictures.Count-1:currentIndex-1;
            LoadImage();
        }

        private void next_Click(object sender, RoutedEventArgs e)
        {
            currentIndex=(currentIndex==pictures.Count-1)?0:currentIndex+1;
            LoadImage();
        }

        private void Viewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            scale=Viewer.ZoomFactor;
            zoomfactor.Text = ScaleText;
        }

        private async void SavePic_Click(object sender, RoutedEventArgs e)
        {
            string? r=await ImageExtension.DownloadImagesAsync(CurrentUrl);
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
}
