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
        public MediaViewer(string url)
        {
            this.InitializeComponent();
            _url = url;
            this.Title = "×ÊÔ´Ô¤ÀÀ";
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(GridTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            
            this.SystemBackdrop=new AcrylicSystemBackdrop();

            Activated += MediaViewer_Activated;
        }

        private void MediaViewer_Activated(object sender, WindowActivatedEventArgs args)
        {
            var bitmap=new BitmapImage(new Uri(_url));
            PicViewer.Source = bitmap;
            zoomer.ZoomToFactor(0.6f);
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
    }
}
