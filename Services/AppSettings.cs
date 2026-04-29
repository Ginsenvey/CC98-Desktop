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

    private T? GetValueByName<T>(string key)
    {
        if (LocalSettings.Values.TryGetValue(key, out var value) && value is T propertyValue)
        {
            return propertyValue;
        }
        else
        {
            return default;
        }
    }
    private T? GetValue<T>(string key)
    {
        if (LocalSettings.Values.TryGetValue(key, out var value) && value is T propertyValue)
        {
            return propertyValue;
        }
        else
        {
            return default;
        }
    }
    private bool SetValue<T>(string key, object value)
    {
        try
        {
            LocalSettings.Values[key] = value;
            OnPropertyChanged();
            return true;
        }
        catch
        {
            // 忽略存储异常（例如权限等）
            OnPropertyChanged();
            return false;
        }
    }
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
            
        }
    }

    public bool ShowBigPaper
    {
        get => GetValue<bool>(nameof(ShowBigPaper));
        set=> SetValue<bool>(nameof(ShowBigPaper), value);
    }
    public int ActiveMode
    {
        get => GetValue<int>(nameof(ActiveMode));
        set => SetValue<int>(nameof(ActiveMode), value);
    }
    public int UserId
    {
        get => GetValue<int>(nameof(UserId));
        set => SetValue<int>(nameof(UserId), value);
    }
    public int TitlePage
    {
        get => GetValue<int>(nameof(TitlePage));
        set => SetValue<int>(nameof(TitlePage), value);
    }
    public int Theme 
    {         
        get => GetValue<int>(nameof(Theme));
        set => SetValue<int>(nameof(Theme), value);
    }
    public int Effect
    {
        get => GetValue<int>(nameof(Effect));
        set => SetValue<int>(nameof(Effect), value);
    }
    public bool IsTailVisible
    {
        get => GetValue<bool>(nameof(IsTailVisible));
        set => SetValue<bool>(nameof(IsTailVisible), value);
    }
    public string CustomBoards
    {
        get => GetValue<string>(nameof(CustomBoards)) ?? string.Empty;
        set => SetValue<string>(nameof(CustomBoards), value);
    }
    public string ThemePicture
    {
        get => GetValue<string>(nameof(ThemePicture)) ?? string.Empty;
        set => SetValue<string>(nameof(ThemePicture), value);
    }
    public string Portrait
    {
        get => GetValue<string>(nameof(Portrait)) ?? string.Empty;
        set => SetValue<string>(nameof(Portrait), value);
    }
    //开发者模式，禁用网络检查
    public string DevelopMode
    {
        get => GetValue<string>(nameof(DevelopMode)) ?? string.Empty;
        set => SetValue<string>(nameof(DevelopMode), value);
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