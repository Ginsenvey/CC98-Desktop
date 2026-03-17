
using CC98.Kernel.UserExperience;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Controls
{
    public sealed partial class SmartImage : UserControl
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(object), typeof(SmartImage),
            new PropertyMetadata(null, OnSourceChanged));
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(SmartImage),
            new PropertyMetadata(null));
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch),
                typeof(Stretch),
                typeof(SmartImage),
                new PropertyMetadata(Stretch.Uniform));

        public static readonly DependencyProperty UseWebVpnProperty =
            DependencyProperty.Register(
                nameof(UseWebVpn),
                typeof(bool),
                typeof(SmartImage),
                new PropertyMetadata(true));

        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }
        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public bool UseWebVpn
        {
            get => (bool)GetValue(UseWebVpnProperty);
            set => SetValue(UseWebVpnProperty, value);
        }

        

        public SmartImage()
        {
            this.InitializeComponent();
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmartImage control)
            {
                control.LoadImage();  
            }
        }

        private async void LoadImage()
        {
            if (Source == null) 
            {
                InnerImage.Source = null;
                return;
            } 

            try
            {
                LoadingIndicator.IsActive = true;
                InnerImage.Opacity = 0.5;

                switch (Source)
                {
                    case string url when ImageResolver.IsWebUrl(url):
                        InnerImage.Source= await ImageResolver.LoadWebImage(url);
                        ImageSource = InnerImage.Source;
                        break;

                    case string path when ImageResolver.IsLocalPath(path):
                        InnerImage.Source = await ImageResolver.LoadLocalImage(path);
                        ImageSource = InnerImage.Source;
                        break;

                    case Uri uri when ImageResolver.IsWebUri(uri):
                        InnerImage.Source = await ImageResolver.LoadWebImage(uri.ToString());
                        ImageSource = InnerImage.Source;
                        break;

                    case Uri uri when ImageResolver.IsLocalUri(uri):
                        InnerImage.Source = await ImageResolver.LoadLocalImage(uri.ToString());
                        ImageSource = InnerImage.Source;
                        break;

                    case ImageSource imageSource:
                        InnerImage.Source = imageSource;
                        ImageSource = imageSource;
                        break;

                }
            }
            catch 
            {
                InnerImage.Source =null;
            }
            finally
            {
                LoadingIndicator.IsActive = false;
                InnerImage.Opacity = 1;
            }
        }

        private void InnerImage_Unloaded(object sender, RoutedEventArgs e)
        {
            //此处释放资源会导致UBBTextBlock中Emoji标签无法正常显示。因此注释掉。
            //InnerImage.Source = null;
        }
    }
}
