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
            this.Title = "��ԴԤ��";
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
                msg.Title = "����ʧ��";
                if (r == "100")
                {
                    msg.Content = "ͼƬԴ����";
                }
                else if (r.Contains("101"))
                {
                    msg.Content = "����ʧ�ܣ�������Ϣ��" + r.Split(':')[1];
                }
                else
                {
                    msg.Content = "����ʧ�ܣ�������";
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
                    // 1. �� Image �ؼ���Ⱦ��λͼ
                    var renderTargetBitmap = new RenderTargetBitmap();
                    await renderTargetBitmap.RenderAsync(PicViewer);

                    // 2. ��ȡ���ػ�����
                    var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
                    if (mode)
                    {
                        StorageFolder downloadsFolder = KnownFolders.PicturesLibrary;
                        StorageFile file = await downloadsFolder.CreateFileAsync(
                            $"CC98Picture_{DateTime.Now:yyyyMMddHHmmss}.png",
                            CreationCollisionOption.ReplaceExisting);

                        // 4. ����Ϊ PNG ������
                        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                            encoder.SetPixelData(
                                BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Premultiplied,
                                (uint)renderTargetBitmap.PixelWidth,
                                (uint)renderTargetBitmap.PixelHeight,
                                96.0, // ˮƽ DPI
                                96.0, // ��ֱ DPI
                                pixelBuffer.ToArray());

                            await encoder.FlushAsync();
                        }
                        return "200:"+downloadsFolder.DisplayName;
                    }
                    else
                    {
                        // 3. ����������ת��Ϊ SoftwareBitmap
                        using (var softwareBitmap = new SoftwareBitmap(
                            BitmapPixelFormat.Bgra8,
                            renderTargetBitmap.PixelWidth,
                            renderTargetBitmap.PixelHeight,
                            BitmapAlphaMode.Premultiplied))
                        {
                            softwareBitmap.CopyFromBuffer(pixelBuffer);

                            // 4. �� SoftwareBitmap ����Ϊ PNG ��
                            var stream = new InMemoryRandomAccessStream();
                            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                            encoder.SetSoftwareBitmap(softwareBitmap);
                            await encoder.FlushAsync();

                            // 5. ���� DataPackage �����ü���������
                            var dataPackage = new DataPackage();
                            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
                            Clipboard.SetContent(dataPackage);

                            // ��ʾ���Ƴɹ�
                            return "200:���Ƴɹ�";
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
            msg.Title= "������ʾ";
            if(r.Contains("200"))
            {
                msg.Content = "�ѱ��浽ͼƬ�ļ��С�";
                msg.IsOpen = true;
            }
            else if (r.Contains("101"))
            {
                msg.Content = "����ʧ�ܣ�������Ϣ��" + r.Split(':')[1];
                msg.IsOpen = true;
            }
            else
            {
                msg.Content = "����ʧ�ܣ�������";
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
