using CC98.Kernel;
using CC98.Kernel.UserExperience;
using CC98.Objects;
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
        private MediaType _type = MediaType.Image;
        private string _url = "";

        private double _currentScale = 1.0;
        private double _currentRotation = 0;
        private const double ScaleStep = 0.1;
        private const double MinScale = 0.5;
        private const double MaxScale = 3.0;
        public MediaViewer(ViewerNavigationInfo info)
        {
            this.InitializeComponent();
            _type=info.Type;
            pictures.AddRange(info.Urls);
            flipview.SelectedIndex = info.CurrentIndex;
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
            this.Content.PointerWheelChanged += OnPointerWheelChanged;
           
        }

        
        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                // 使用正确的方法检测 Ctrl 键
                bool isCtrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
                    .HasFlag(CoreVirtualKeyStates.Down);

                if (!isCtrlPressed)
                    return;

                // 获取当前图片
                var picture = GetCurrentPicture();
                if (picture == null)
                    return;

                // 获取变换
                var transform = picture.RenderTransform as CompositeTransform;
                if (transform == null)
                    return;

                var properties = e.GetCurrentPoint(null).Properties;

                // 调整缩放
                if (properties.MouseWheelDelta > 0)
                {
                    _currentScale = Math.Min(_currentScale + ScaleStep, MaxScale);
                }
                else
                {
                    _currentScale = Math.Max(_currentScale - ScaleStep, MinScale);
                }

                transform.ScaleX = _currentScale;
                transform.ScaleY = _currentScale;

                if (zoomfactor != null)
                {
                    zoomfactor.Text = $"{_currentScale * 100:F0}%";
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private async void MediaViewer_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            if (_type == MediaType.Video)
            {

                VideoPlayer.Visibility = Visibility.Visible;
                Grid.SetRow(VideoPlayer,1);
                Grid.SetRowSpan(VideoPlayer, 2);
                var source =await LoginService.vpn.GetSourceAsync(_url);
                if (source != null)
                {
                    VideoPlayer.Source = source;
                }
            }
            else if (_type == MediaType.Image)
            {
                
                VideoPlayer.Visibility = Visibility.Collapsed;
            }
                
            
        }

        private Picture? GetCurrentPicture()
        {
            if (flipview.SelectedItem == null) return null;

            // 获取当前 FlipView 项的容器
            var container = flipview.ContainerFromIndex(flipview.SelectedIndex) as FlipViewItem;
            if (container == null) return null;

            // 查找 Picture 控件
            return FindDescendant<Picture>(container);
        }
        private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;

                var result = FindDescendant<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
        

        private void Rotate_Click(object sender, RoutedEventArgs e)
        {

            var picture = GetCurrentPicture();
            if (picture == null) return;

            var transform = picture.RenderTransform as CompositeTransform;
            if (transform == null) return;

            // 每次点击旋转 90 度
            _currentRotation = (_currentRotation + 90) % 360;
            transform.Rotation = _currentRotation;
        }

        private async void AddAsEmoji_Click(object sender, RoutedEventArgs e)
        {
            if (_type == MediaType.Image && !_url.StartsWith("ms-appx"))
            {
                await CustomEmoji.SaveEmojiAsync(_url);
            }
            else
            {
                
            }
        }
        private void MediaViewer_Closed(object sender, WindowEventArgs e)
        {
            // 释放资源示例
            VideoPlayer.Source = null;
        }

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentScale = 1.0;
            _currentRotation = 0;
            zoomfactor.Text = "100%";
            //实现双向绑定
            currentIndex = flipview.SelectedIndex;
            MediaInfo.Text = pictures[currentIndex];
            Posi.Text= $"{currentIndex + 1} / {pictures.Count}";

            var picture = GetCurrentPicture();
            if (picture != null)
            {
                var transform = picture.RenderTransform as CompositeTransform;
                if (transform != null)
                {
                    transform.ScaleX = 1;
                    transform.ScaleY = 1;
                    transform.Rotation = 0;
                }
            }
        }

        private void zoomout_Click(object sender, RoutedEventArgs e)
        {

        }

        private void zoomin_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CopyPic_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
