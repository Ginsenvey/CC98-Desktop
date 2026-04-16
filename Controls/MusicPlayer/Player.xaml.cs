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
// MusicPlayer.xaml.cs
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using CC98.Share.Controls.Primitives;
using UbbRender.Common;

namespace CC98.Share.Controls;

public sealed partial class MusicPlayer : UserControl, IDisposable
{
    private GlobalMediaPlayer _globalPlayer;
    private bool _isInitialized = false;
    private bool _isPlaying = false;
    private bool _disposed = false;
    private DispatcherTimer _progressTimer;

    public MusicPlayer()
    {
        this.InitializeComponent();
        InitializeMediaPlayer();
        SetupProgressTimer();
    }

    #region 依赖属性

    public static readonly DependencyProperty SrcProperty =
        DependencyProperty.Register("Src", typeof(string), typeof(MusicPlayer),
            new PropertyMetadata(null, OnSrcChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(MusicPlayer),
            new PropertyMetadata("音频"));

    public static readonly DependencyProperty LoadMediaCallbackProperty =
        DependencyProperty.Register("LoadMediaCallback", typeof(IMediaLoader),
            typeof(MusicPlayer), new PropertyMetadata(new DefaultMediaLoader()));

    public string Src
    {
        get => (string)GetValue(SrcProperty);
        set => SetValue(SrcProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IMediaLoader LoadMediaCallback
    {
        get => (IMediaLoader)GetValue(LoadMediaCallbackProperty);
        set => SetValue(LoadMediaCallbackProperty, value);
    }

    #endregion

    #region 事件

    public event EventHandler<MediaClickEventArgs> DownloadStarted;

    private void OnDownloadStarted()
    {
        DownloadStarted?.Invoke(this, new MediaClickEventArgs(Src,MediaType.Audio));
    }

    #endregion

    #region 初始化

    private void InitializeMediaPlayer()
    {
        // 使用全局单例 MediaPlayer
        _globalPlayer = GlobalMediaPlayer.Instance;

        //订阅事件（只订阅，不处理底层释放）
        _globalPlayer.MediaOpened += OnMediaOpened;
        _globalPlayer.MediaEnded += OnMediaEnded;
        _globalPlayer.MediaFailed += OnMediaFailed;
        _globalPlayer.CurrentStateChanged += OnCurrentStateChanged;
    }

    private void SetupProgressTimer()
    {
        _progressTimer = new DispatcherTimer();
        _progressTimer.Interval = TimeSpan.FromMilliseconds(250);
        _progressTimer.Tick += UpdateProgress;
    }

    private static void OnSrcChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MusicPlayer player && !player._isPlaying)
        {
            player._isInitialized = false;
            player.ResetUI();
        }
    }

    #endregion

    #region 播放控制

    private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_disposed) return;

        try
        {
            if (!_isInitialized)
            {
                await InitializeAndPlay();
            }
            else
            {
                TogglePlayPause();
            }
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("MusicPlayer","初始化媒体出错",ex.Message);
            ShowErrorMessage("播放失败");
        }
    }

    private async Task InitializeAndPlay()
    {
        if (string.IsNullOrEmpty(Src))
        {
            ShowErrorMessage("音频源为空");
            return;
        }
        LoadingIndicator.Visibility = Visibility.Visible;
        try
        {
            var mediaSource = await LoadMediaCallback.LoadMedia(Src);

            if (mediaSource == null)
            {
                ShowErrorMessage($"无法加载媒体:{Src}");
                return;
            }

            // 使用全局播放器设置源并播放
            _globalPlayer.SetSource(mediaSource);
            _isInitialized = true;
            ProgressSlider.IsEnabled = true;
            // 开始播放
            _globalPlayer.Play();
            
            _isPlaying = true;
            UpdatePlayPauseButton();
            _progressTimer.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"初始化失败: {ex.Message}");
            ShowErrorMessage("初始化失败");
            _isInitialized = false;
        }
        finally
        {
            LoadingIndicator.Visibility = Visibility.Collapsed;
        }
    }

    private void TogglePlayPause()
    {
        if (_isPlaying)
        {
            _globalPlayer.Pause();
            _progressTimer.Stop();
        }
        else
        {
            _globalPlayer.Play();
            _progressTimer.Start();
        }
        _isPlaying = !_isPlaying;
        UpdatePlayPauseButton();
    }

    private void UpdatePlayPauseButton()
    {
        PlayPauseIcon.Glyph = _isPlaying ? "\uE103" : "\uE102"; // 暂停/播放图标
        ToolTipService.SetToolTip(PlayPauseButton, _isPlaying ? "暂停" : "播放");
    }

    #endregion

    #region 进度条控制

    private void UpdateProgress(object sender, object e)
    {
        var session = _globalPlayer?.PlaybackSession;
        if (session != null && session.NaturalDuration.TotalSeconds >0)
        {
            var current = session.Position.TotalSeconds;
            var total = session.NaturalDuration.TotalSeconds;

            ProgressSlider.Value = (current / total) *100;

            CurrentTimeText.Text = FormatTime(current);
            TotalTimeText.Text = FormatTime(total);
        }
    }

    // 使用正确的事件签名 (RangeBaseValueChangedEventArgs) 来处理滑块拖动
    private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var session = _globalPlayer?.PlaybackSession;
        if (session != null && session.NaturalDuration.TotalSeconds >0)
        {
            var newPosition = TimeSpan.FromSeconds(
                ProgressSlider.Value /100.0 * session.NaturalDuration.TotalSeconds);
            session.Position = newPosition;
        }
    }

    private string FormatTime(double seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:D2}";
    }

    #endregion

    #region 下载功能

    private void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Src))
        {
            OnDownloadStarted();
        }
        else
        {
            ShowErrorMessage("没有可下载的音频源");
        }
    }

    

    #endregion

    #region MediaPlayer 事件处理

    private void OnMediaOpened(MediaPlayer sender, object args)
    {
        // 媒体打开成功
        DispatcherQueue.TryEnqueue(() =>
        {
            TotalTimeText.Text = FormatTime(sender.PlaybackSession.NaturalDuration.TotalSeconds);
        });
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _isPlaying = false;
            UpdatePlayPauseButton();
            _progressTimer.Stop();
            ProgressSlider.Value = 0;
            CurrentTimeText.Text = "0:00";
        });
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ShowErrorMessage($"播放失败: {args.ErrorMessage}");
            _isPlaying = false;
            _isInitialized = false;
            UpdatePlayPauseButton();
            _progressTimer.Stop();
            ResetUI();
        });
    }

    private void OnCurrentStateChanged(MediaPlayer sender, object args)
    {
        // 可以根据需要处理状态变化
    }

    #endregion

    #region UI 辅助方法

    private void ResetUI()
    {
        ProgressSlider.Value = 0;
        CurrentTimeText.Text = "0:00";
        TotalTimeText.Text = "0:00";
        PlayPauseIcon.Glyph = "\uE102";
        ToolTipService.SetToolTip(PlayPauseButton, "播放");
    }

    private void ShowErrorMessage(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;

        // 3秒后隐藏错误信息
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, e) =>
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }

    #endregion

    #region IDisposable 实现

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            _progressTimer?.Stop();
            _progressTimer = null;

            if (_globalPlayer != null)
            {
                //只取消订阅事件，不释放全局播放器
                _globalPlayer.MediaOpened -= OnMediaOpened;
                _globalPlayer.MediaEnded -= OnMediaEnded;
                _globalPlayer.MediaFailed -= OnMediaFailed;
                _globalPlayer.CurrentStateChanged -= OnCurrentStateChanged;

                // 不要调用 Dispose 在全局播放器上
            }

            GC.SuppressFinalize(this);
        }
    }

    ~MusicPlayer()
    {
        Dispose();
    }

    #endregion

}