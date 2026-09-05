using System;
using Windows.UI;
using CC98.Kernel;
using CC98.Objects;
using FluentIcons.Common;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using CC98.Services.Helpers;

namespace CC98.Services;

public partial class BoolToColorConverter : IValueConverter
{
    // 定义可配置的颜色，方便复用
    public SolidColorBrush TrueColor { get; set; } = new SolidColorBrush(Colors.Green);
    public SolidColorBrush FalseColor { get; set; } = new SolidColorBrush(Colors.Gray);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueColor : FalseColor;
        }
        return FalseColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public partial class BoolToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
/// <summary>
/// 将十六进制颜色字符串转换为 SolidColorBrush 的值转换器。
/// </summary>
public partial class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hexColor)
            try
            {
                // 移除可能的 "#" 前缀
                hexColor = hexColor.Replace("#", string.Empty);

                // 解析 ARGB 或 RGB 格式
                byte a = 255; // 默认不透明
                byte r, g, b;

                if (hexColor.Length == 6) // RGB 格式（如 "4287F5"）
                {
                    r = System.Convert.ToByte(hexColor.Substring(0, 2), 16);
                    g = System.Convert.ToByte(hexColor.Substring(2, 2), 16);
                    b = System.Convert.ToByte(hexColor.Substring(4, 2), 16);
                }
                else if (hexColor.Length == 8) // ARGB 格式（如 "FF4287F5"）
                {
                    a = System.Convert.ToByte(hexColor.Substring(0, 2), 16);
                    r = System.Convert.ToByte(hexColor.Substring(2, 2), 16);
                    g = System.Convert.ToByte(hexColor.Substring(4, 2), 16);
                    b = System.Convert.ToByte(hexColor.Substring(6, 2), 16);
                }
                else
                {
                    return new SolidColorBrush(Colors.Transparent); // 无效格式返回透明
                }

                return new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
            catch
            {
                return new SolidColorBrush(Colors.Transparent); // 解析失败返回透明
            }

        return new SolidColorBrush(Colors.Transparent); // 非字符串输入返回透明
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException(); // 单向绑定不需要反向转换
    }
}

public partial class BooltoVisibilityConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool flag) return flag ? Visibility.Visible : Visibility.Collapsed;

        return Visibility.Collapsed;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
/// <summary>
/// 点赞状态 → 图标变体转换器。
/// </summary>
public partial class LikeToVariantConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int state)
            return state switch
            {
                1 => IconVariant.Filled,
                _ => IconVariant.Regular
            };

        return IconVariant.Regular;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
/// <summary>
/// 点踩状态 → 图标变体转换器。
/// </summary>
public partial class DisLikeToVariantConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int state)
            return state switch
            {
                2 => IconVariant.Filled,
                _ => IconVariant.Regular
            };

        return IconVariant.Regular;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public partial class ReBooltoVisibilityConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool flag) return flag ? Visibility.Collapsed : Visibility.Visible;

        return Visibility.Collapsed;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public partial class BoolToFollowTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isFollowing) return isFollowing ? "取消关注" : "关注用户";
        return "关注用户";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
/// <summary>
/// 布尔值转换为图标变体的值转换器。
/// </summary>
public partial class BoolToVariantConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool state && state) ? IconVariant.Color : IconVariant.Regular;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转换为图标变体的值转换器（Filled/Regular）。
/// </summary>
public partial class BoolToFilledConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool state && state) ? IconVariant.Filled : IconVariant.Regular;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 搜索建议类型 → 图标(Symbol)转换。
/// </summary>
public partial class SearchSuggestionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is SearchSuggestionType type
            ? type switch
            {
                SearchSuggestionType.Topic => Symbol.Document,
                SearchSuggestionType.User => Symbol.Person,
                SearchSuggestionType.UserId => Symbol.Person,
                SearchSuggestionType.Board => Symbol.Board,
                SearchSuggestionType.TopicId => Symbol.Document,
                _ => Symbol.Search
            }
            : Symbol.Search;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 字符串非空 → Visible,空/空白 → Collapsed。
/// </summary>
public partial class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}