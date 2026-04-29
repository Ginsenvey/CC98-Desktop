using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace CC98.Services;

/// <summary>
///     应用设置单例。将设置持久化到 ApplicationData.Current.LocalSettings。
///     可在 XAML 中作为 StaticResource 或绑定源使用，支持 PropertyChanged 通知。
/// </summary>
public sealed partial class AppSettings : INotifyPropertyChanged
{
    //需要迁移的设置项

    private ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;
    public static AppSettings Current { get; } = new();

    /// <summary>
    ///     是否隐藏图片。设置时会保存到 ApplicationData 并触发 PropertyChanged。
    /// </summary>
    public bool HideImage
    {
        get => GetValueByName<bool>(nameof(HideImage));
        set
        {
            try
            {
                LocalSettings.Values[nameof(HideImage)] = value;
            }
            catch
            {
                // 忽略存储异常（例如权限等），仍然触发通知
            }

            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private T? GetValueByName<T>(string key)
    {
        if (LocalSettings.Values.TryGetValue(key, out var value) && value is T propertyValue) return propertyValue;

        return default;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}