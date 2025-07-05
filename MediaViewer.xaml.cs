using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition.SystemBackdrops;
using DevWinUI;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using static System.Net.Mime.MediaTypeNames;
using Windows.Storage;
using Windows.Media.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class MediaViewer : Window
    {
        private string _url;
        private string _type;
        public MediaViewer(Dictionary<string,string> param)
        {
            this.InitializeComponent();
            _url = param["url"];
            _type=param["type"];
            this.Title = "资源预览";
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(GridTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            
            this.SystemBackdrop=new AcrylicSystemBackdrop();

            Activated += MediaViewer_Activated;
        }

        private void MediaViewer_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_type == "video")
            {
                ImageControl.Visibility = Visibility.Collapsed;
                zoomer.Visibility = Visibility.Collapsed;
                VideoPlayer.Visibility = Visibility.Visible;
                Grid.SetRow(VideoPlayer,0);
                Grid.SetRowSpan(VideoPlayer, 2);
                VideoPlayer.Source = MediaSource.CreateFromUri(new Uri(_url));
            }
            else if (_type == "image")
            {
                ImageControl.Visibility = Visibility.Visible;
                zoomer.Visibility = Visibility.Visible;
                VideoPlayer.Visibility = Visibility.Collapsed;
                var bitmap = new BitmapImage(new Uri(_url));
                PicViewer.Source = bitmap;
                zoomer.ZoomToFactor(0.6f);
            }
                
            
        }

        private void zoomer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            zoomfactor.Text=(zoomer.ZoomFactor*100).ToString("F2")+"%";
        }

        private void zoomin_Click(object sender, RoutedEventArgs e)
        {
            zoomer.ZoomToFactor(zoomer.ZoomFactor + 0.1f);
        }

        private void zoomout_Click(object sender, RoutedEventArgs e)
        {
            zoomer.ZoomToFactor(zoomer.ZoomFactor - 0.1f);
        }

        private async void SavePic_Click(object sender, RoutedEventArgs e)
        {
            string r = await ProcessImage(false);
            if (!r.Contains("200"))
            {
                msg.Title = "复制失败";
                if (r == "100")
                {
                    msg.Content = "图片源错误";
                }
                else if (r.Contains("101"))
                {
                    msg.Content = "复制失败，错误信息：" + r.Split(':')[1];
                }
                else
                {
                    msg.Content = "复制失败，请重试";
                }
                msg.IsOpen = true;
            }
        }
        private async Task<string> ProcessImage(bool mode)
        {
            if (PicViewer.Source is BitmapSource bitmapSource)
            {
                try
                {
                    // 1. 将 Image 控件渲染到位图
                    var renderTargetBitmap = new RenderTargetBitmap();
                    await renderTargetBitmap.RenderAsync(PicViewer);

                    // 2. 获取像素缓冲区
                    var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
                    if (mode)
                    {
                        StorageFolder downloadsFolder = KnownFolders.PicturesLibrary;
                        StorageFile file = await downloadsFolder.CreateFileAsync(
                            $"CC98Picture_{DateTime.Now:yyyyMMddHHmmss}.png",
                            CreationCollisionOption.ReplaceExisting);

                        // 4. 编码为 PNG 并保存
                        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                            encoder.SetPixelData(
                                BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Premultiplied,
                                (uint)renderTargetBitmap.PixelWidth,
                                (uint)renderTargetBitmap.PixelHeight,
                                96.0, // 水平 DPI
                                96.0, // 垂直 DPI
                                pixelBuffer.ToArray());

                            await encoder.FlushAsync();
                        }
                        return "200:"+downloadsFolder.DisplayName;
                    }
                    else
                    {
                        // 3. 将像素数据转换为 SoftwareBitmap
                        using (var softwareBitmap = new SoftwareBitmap(
                            BitmapPixelFormat.Bgra8,
                            renderTargetBitmap.PixelWidth,
                            renderTargetBitmap.PixelHeight,
                            BitmapAlphaMode.Premultiplied))
                        {
                            softwareBitmap.CopyFromBuffer(pixelBuffer);

                            // 4. 将 SoftwareBitmap 编码为 PNG 流
                            var stream = new InMemoryRandomAccessStream();
                            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                            encoder.SetSoftwareBitmap(softwareBitmap);
                            await encoder.FlushAsync();

                            // 5. 创建 DataPackage 并设置剪贴板内容
                            var dataPackage = new DataPackage();
                            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
                            Clipboard.SetContent(dataPackage);

                            // 提示复制成功
                            return "200:复制成功";
                        }
                        
                    }

                }
                catch (Exception ex)
                {
                    return "101:"+ ex.Message;
                }
            }
            else
            {
                return "100";
            }
        }
        
        

        private async void SavePic_Click_1(object sender, RoutedEventArgs e)
        {
            string r=await ProcessImage(true);
            msg.Title= "下载提示";
            if(r.Contains("200"))
            {
                msg.Content = "已保存到图片文件夹。";
                msg.IsOpen = true;
            }
            else if (r.Contains("101"))
            {
                msg.Content = "下载失败，错误信息：" + r.Split(':')[1];
                msg.IsOpen = true;
            }
            else
            {
                msg.Content = "下载失败，请重试";
                msg.IsOpen = true;
            }
        }
        public double angle = 0;
        private void Rotate_Click(object sender, RoutedEventArgs e)
        {
            
            if (angle<270)
            {
                angle += 90;
                ImageRotationTransform.Angle = angle;
            }
            else
            {
                angle = 0;
                ImageRotationTransform.Angle = angle;
            }
            
            

        }
    }
}
