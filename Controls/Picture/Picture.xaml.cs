using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Controls.Picture;

public sealed partial class Picture : UserControl
{
    public Picture()
    {
        InitializeComponent();
    }

    #region 图片加载

    // 加载代次:Source 快速变化时,旧请求完成不得覆盖新请求的结果
    private int _loadGeneration;

    private async Task LoadImage()
    {
        var generation = ++_loadGeneration;
        shimmer.IsActive = true;
        try
        {
            var src = Src;
            var callback = LoadImageCallback;
            var bitmap = await callback.LoadImage(src);
            // 仅当 Src 未再次变化时才应用结果,避免慢请求覆盖新图
            if (generation != _loadGeneration) return;
            Viewer.Source = bitmap;
        }
        catch
        {
            if (generation == _loadGeneration) Viewer.Source = null;
        }
        finally
        {
            // 无论成功/失败都关闭 shimmer,避免破图位置永久显示加载动画
            if (generation == _loadGeneration)
            {
                shimmer.IsActive = false;
                shimmer.Visibility = Visibility.Collapsed;
            }
        }
    }

    #endregion

    #region 依赖属性

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(
            nameof(Stretch),
            typeof(Stretch),
            typeof(Picture),
            new(Stretch.Uniform));

    public static readonly DependencyProperty SrcProperty =
        DependencyProperty.Register(
            "Src",
            typeof(string),
            typeof(Picture),
            new("", OnSrcChanged));

    public static readonly DependencyProperty HideProperty =
        DependencyProperty.Register(
            "Hide",
            typeof(bool),
            typeof(Picture),
            new(false));

    public static readonly DependencyProperty LoadImageCallbackProperty =
        DependencyProperty.Register("LoadImageCallback", typeof(IImageLoader),
            typeof(Picture), new(new DefaultImageLoader()));

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public string Src
    {
        get => (string)GetValue(SrcProperty);
        set => SetValue(SrcProperty, value);
    }

    public bool Hide
    {
        get => (bool)GetValue(HideProperty);
        set => SetValue(HideProperty, value);
    }

    public IImageLoader LoadImageCallback
    {
        get => (IImageLoader)GetValue(LoadImageCallbackProperty);
        set => SetValue(LoadImageCallbackProperty, value);
    }

    #endregion


    #region 事件处理

    private static async void OnSrcChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Picture picture)
        {
            if (picture.Hide)
            {
                picture.DisplayButton.Visibility = Visibility.Visible;
                picture.shimmer.Visibility = Visibility.Collapsed;
                picture.Viewer.Source = null;
            }
            else
            {
                await picture.LoadImage();
            }
        }
    }

    private async void DisplayButton_Click(object sender, RoutedEventArgs e)
    {
        DisplayButton.Visibility = Visibility.Collapsed;
        shimmer.Visibility = Visibility.Visible;
        await LoadImage();
    }

    #endregion
}