using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace CC98.Services
{
    /// <summary>
    /// 应用设置单例。将设置持久化到 ApplicationData.Current.LocalSettings。
    /// 可在 XAML 中作为 StaticResource 或绑定源使用，支持 PropertyChanged 通知。
    /// </summary>
    public sealed partial class AppSettings : INotifyPropertyChanged
    {
        private const string HideImageKey = "HideImage";
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        private static readonly Lazy<AppSettings> _instance = new(() => new AppSettings());
        public static AppSettings Current => _instance.Value;

        private bool _hideImage;

        private AppSettings()
        {
            // 从 LocalSettings 加载初始值，若不存在则默认 false
            if (_localSettings.Values.TryGetValue(HideImageKey, out var value) && value is bool b)
            {
                _hideImage = b;
            }
            else
            {
                _hideImage = false;
            }
        }

        /// <summary>
        /// 是否隐藏图片。设置时会保存到 ApplicationData 并触发 PropertyChanged。
        /// </summary>
        public bool HideImage
        {
            get => _hideImage;
            set
            {
                if (_hideImage == value) return;
                _hideImage = value;
                try
                {
                    _localSettings.Values[HideImageKey] = value;
                }
                catch
                {
                    // 忽略存储异常（例如权限等），仍然触发通知
                }
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
