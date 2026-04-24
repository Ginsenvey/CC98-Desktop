// VideoPlayer.xaml.cs

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CC98.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CC98.Controls.VideoPlayer;

public sealed partial class VideoPlayer : UserControl, IDisposable
{
    private bool _isInitialized = false;
    private bool _isDisposed = false;
        

    public VideoPlayer()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region 依赖属性

    public static readonly DependencyProperty SrcProperty =
        DependencyProperty.Register("Src", typeof(string), typeof(VideoPlayer),
            new(null, OnSrcChanged));

    public static readonly DependencyProperty LoadVideoCallbackProperty =
        DependencyProperty.Register("LoadVideoCallback", typeof(IMediaLoader),
            typeof(VideoPlayer), new(new DefaultMediaLoader()));

    public string Src
    {
        get => (string)GetValue(SrcProperty);
        set => SetValue(SrcProperty, value);
    }

    public IMediaLoader LoadVideoCallback
    {
        get => (IMediaLoader)GetValue(LoadVideoCallbackProperty);
        set => SetValue(LoadVideoCallbackProperty, value);
    }

    // 暴露 MediaPlayerElement 的原始属性
    public MediaPlayerElement MediaPlayerElement => MediaPlayer;

    public bool AreTransportControlsEnabled
    {
        get => MediaPlayer.AreTransportControlsEnabled;
        set => MediaPlayer.AreTransportControlsEnabled = value;
    }

    public MediaTransportControls TransportControls
    {
        get => (MediaTransportControls)MediaPlayer.TransportControls;
    }

    public Stretch Stretch
    {
        get => MediaPlayer.Stretch;
        set => MediaPlayer.Stretch = value;
    }

    public bool AutoPlay
    {
        get => MediaPlayer.AutoPlay;
        set => MediaPlayer.AutoPlay = value;
    }



    #endregion

    #region 事件

    public event EventHandler<VideoPlayerEventArgs> VideoLoaded;
    public event EventHandler<VideoPlayerFailedEventArgs> VideoFailed;
    public event EventHandler<VideoPlayerEventArgs> VideoInitialized;

    #endregion

    #region 初始化与生命周期

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 注册焦点事件
            

        // 注册 MediaPlayer 事件
        if (MediaPlayer.MediaPlayer != null)
        {
            MediaPlayer.MediaPlayer.MediaOpened += OnMediaOpened;
            MediaPlayer.MediaPlayer.MediaFailed += OnMediaFailed;
            MediaPlayer.MediaPlayer.MediaEnded += OnMediaEnded;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
            

        if (MediaPlayer.MediaPlayer != null)
        {
            MediaPlayer.MediaPlayer.MediaOpened -= OnMediaOpened;
            MediaPlayer.MediaPlayer.MediaFailed -= OnMediaFailed;
            MediaPlayer.MediaPlayer.MediaEnded -= OnMediaEnded;
        }
    }

    private static void OnSrcChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VideoPlayer player)
        {
            player._isInitialized = false;
            player.ShowPlaceholder();        
        }
    }

        
        

    #endregion

    #region 加载





    // 手动请求加载视频
    public async Task<bool> LoadVideoAsync()
    {
        return await InitializeVideoAsync();
    }

    #endregion

    #region 视频加载

    private async Task<bool> InitializeVideoAsync()
    {
        if (_isInitialized || _isDisposed || string.IsNullOrEmpty(Src))
        {
            return false;
        }

        try
        {
            ShowLoadingIndicator();

            var src = Src;  // 在 UI 线程读取依赖属性
            var callback = LoadVideoCallback;  // 在 UI 线程读取依赖属性

            // 在后台线程执行加载操作
            var mediaSource = await Task.Run(() =>
            {
                try
                {
                    return callback?.LoadMedia(src);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"后台线程加载失败: {ex.Message}");
                    return null;
                }
            });

            if (mediaSource == null)
            {
                throw new("无法加载视频源");
            }


            // 切换到 UI 线程设置源
            await DispatcherQueue.EnqueueAsync(() =>
            {
                MediaPlayer.Source = mediaSource;
                _isInitialized = true;

                HideLoadingIndicator();
                HidePlaceholder();

                VideoInitialized?.Invoke(this, new(Src));

                Debug.WriteLine($"视频初始化成功: {Src}");
            });

            return true;
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                HideLoadingIndicator();
                ShowErrorMessage($"视频加载失败: {ex.Message}");

                VideoFailed?.Invoke(this, new(Src, ex.Message));

                Debug.WriteLine($"视频初始化失败: {ex.Message}");
            });

            return false;
        }
    }

    #endregion

    #region MediaPlayer 事件处理

    private void OnMediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            HideLoadingIndicator();
            HidePlaceholder();

            VideoLoaded?.Invoke(this, new(Src));

            Debug.WriteLine($"视频开始播放: {Src}");
        });
    }

    private void OnMediaFailed(Windows.Media.Playback.MediaPlayer sender,
        Windows.Media.Playback.MediaPlayerFailedEventArgs args)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            HideLoadingIndicator();
            ShowErrorMessage($"播放失败: {args.ErrorMessage}");

            VideoFailed?.Invoke(this, new(Src, args.ErrorMessage));

            Debug.WriteLine($"视频播放失败: {args.ErrorMessage}");
        });
    }

    private void OnMediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            Debug.WriteLine($"视频播放结束: {Src}");
            // 可以在这里添加播放结束后的逻辑
        });
    }

    #endregion

    #region UI 状态管理

    private void LoadingButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadVideoAsync();
    }
    private void ShowPlaceholder()
    {
        PlaceholderGrid.Visibility = Visibility.Visible;
        VideoPlayerGrid.Visibility = Visibility.Collapsed;
    }

    private void HidePlaceholder()
    {
        PlaceholderGrid.Visibility = Visibility.Collapsed;
        VideoPlayerGrid.Visibility = Visibility.Visible;
    }

    private void ShowLoadingIndicator()
    {
        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
            
    }

    private void HideLoadingIndicator()
    {
        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Visibility.Collapsed;
            
    }

    private void ShowErrorMessage(string message)
    {
        VideoPlayerGrid.Visibility = Visibility.Collapsed;
        PlaceholderGrid.Visibility = Visibility.Collapsed;
    }


    // 重试加载
    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        _isInitialized = false;
        ShowPlaceholder();

            
            
        _ = InitializeVideoAsync();
           
    }

    #endregion

    #region IDisposable 实现

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            // 清理 MediaPlayer 资源
            // 忽略对已经dispose的对象进行操作
            try
            {
                if (MediaPlayer.MediaPlayer != null)
                {
                    MediaPlayer.MediaPlayer.Pause();
                    MediaPlayer.MediaPlayer.Source = null;
                    MediaPlayer.MediaPlayer.Dispose();
                }

                MediaPlayer.Source = null;
            }
            catch { }

            GC.SuppressFinalize(this);
        }
    }

    ~VideoPlayer()
    {
        Dispose();
    }


    #endregion

        
}

#region 事件参数类

public class VideoPlayerEventArgs : EventArgs
{
    public string Source { get; }

    public VideoPlayerEventArgs(string source)
    {
        Source = source;
    }
}

public class VideoPlayerFailedEventArgs : VideoPlayerEventArgs
{
    public string ErrorMessage { get; }

    public VideoPlayerFailedEventArgs(string source, string errorMessage)
        : base(source)
    {
        ErrorMessage = errorMessage;
    }
}

#endregion