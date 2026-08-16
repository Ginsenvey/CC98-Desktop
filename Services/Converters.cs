using System;
using Windows.UI;
using CC98.Kernel;
using FluentIcons.Common;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using CC98.Services.Helpers;

namespace CC98.Services;

public partial class UbbTextConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        if (value != null)
        {
            var input = value as string ?? string.Empty;
            if (!string.IsNullOrEmpty(input)) return UbbToMarkdown.Convert(input, !AppSettings.Current.HideImage);

            return string.Empty;
        }

        return string.Empty;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
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
        if (value is bool isFollowing) return isFollowing ? "取消关注" : "关注";
        return "关注";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public partial class BoolToVariantConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool state) return state ? IconVariant.Color : IconVariant.Regular;

        return IconVariant.Regular;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}