using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.Text;

namespace CC98.Services.Helpers;
/// <summary>
/// 提供了对普通文本框高亮的拓展方法，也适用于版面页的高亮
/// </summary>
public static class TextBlockExtensions
{
    public static readonly DependencyProperty HighlightedTextProperty =
        DependencyProperty.RegisterAttached(
            "HighlightedText",
            typeof(string),
            typeof(TextBlockExtensions),
            new PropertyMetadata(null, OnHighlightedTextChanged));

    public static string GetHighlightedText(TextBlock obj)
    {
        return (string)obj.GetValue(HighlightedTextProperty);
    }

    public static void SetHighlightedText(TextBlock obj, string value)
    {
        obj.SetValue(HighlightedTextProperty, value);
    }

    // 可选的：高亮颜色
    public static readonly DependencyProperty HighlightColorProperty =
        DependencyProperty.RegisterAttached(
            "HighlightColor",
            typeof(string),
            typeof(TextBlockExtensions),
            new PropertyMetadata(""));
    public static readonly DependencyProperty BoldFontFamilyProperty =
    DependencyProperty.RegisterAttached(
        "BoldFontFamily",
        typeof(FontFamily),
        typeof(TextBlockExtensions),
        new PropertyMetadata(null, OnHighlightedTextChanged));
    public static string GetHighlightColor(TextBlock obj)
    {
        return (string)obj.GetValue(HighlightColorProperty);
    }

    public static void SetHighlightColor(TextBlock obj, string value)
    {
        obj.SetValue(HighlightColorProperty, value);
    }

    // 可选的：关键词列表
    public static readonly DependencyProperty KeywordsProperty =
        DependencyProperty.RegisterAttached(
            "Keywords",
            typeof(string),
            typeof(TextBlockExtensions),
            new PropertyMetadata(null, OnKeywordsChanged));

    public static readonly DependencyProperty IsBoldProperty =
            DependencyProperty.RegisterAttached(
                "IsBold",
                typeof(bool),
                typeof(TextBlockExtensions),
                new PropertyMetadata(false, OnHighlightedTextChanged));

    public static readonly DependencyProperty IsItalicProperty =
        DependencyProperty.RegisterAttached(
            "IsItalic",
            typeof(bool),
            typeof(TextBlockExtensions),
            new PropertyMetadata(false, OnHighlightedTextChanged));
    public static string GetKeywords(TextBlock obj)
    {
        return (string)obj.GetValue(KeywordsProperty);
    }

    public static void SetKeywords(TextBlock obj, string value)
    {
        obj.SetValue(KeywordsProperty, value);
    }
    public static bool GetIsBold(TextBlock obj) => (bool)obj.GetValue(IsBoldProperty);
    public static void SetIsBold(TextBlock obj, bool value) => obj.SetValue(IsBoldProperty, value);

    public static bool GetIsItalic(TextBlock obj) => (bool)obj.GetValue(IsItalicProperty);
    public static void SetIsItalic(TextBlock obj, bool value) => obj.SetValue(IsItalicProperty, value);
    public static FontFamily GetBoldFontFamily(TextBlock obj) => (FontFamily)obj.GetValue(BoldFontFamilyProperty);
    public static void SetBoldFontFamily(TextBlock obj, FontFamily value) => obj.SetValue(BoldFontFamilyProperty, value);

    private static void OnHighlightedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            UpdateHighlightedText(textBlock);
        }
    }

    private static void OnKeywordsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            UpdateHighlightedText(textBlock);
        }
    }

    private static void UpdateHighlightedText(TextBlock textBlock)
    {
        var text = GetHighlightedText(textBlock);
        var keywordsStr = GetKeywords(textBlock);
        var colorStr = GetHighlightColor(textBlock);
        var isBold = GetIsBold(textBlock);     
        var isItalic = GetIsItalic(textBlock);
        var boldFontFamily = GetBoldFontFamily(textBlock);

        if (string.IsNullOrEmpty(text))
        {
            textBlock.Inlines.Clear();
            textBlock.Inlines.Add(new Run { Text = "" });
            return;
        }

        if (string.IsNullOrEmpty(keywordsStr))
        {
            textBlock.Inlines.Clear();
            textBlock.Inlines.Add(new Run { Text = text });
            return;
        }

        var keywords = keywordsStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        SolidColorBrush highlightBrush;
        if (string.IsNullOrEmpty(colorStr))
        {
            highlightBrush=(textBlock.Foreground as SolidColorBrush)!;
        }
        else
        {
            var color = ParseColor(colorStr);
            highlightBrush = new SolidColorBrush(color);
        }
       

        textBlock.Inlines.Clear();

        // 构建高亮文本
        var pattern = string.Join("|", keywords.Select(k => Regex.Escape(k)));
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        int lastIndex = 0;
        foreach (Match match in regex.Matches(text))
        {
            if (match.Index > lastIndex)
            {
                textBlock.Inlines.Add(new Run
                {
                    Text = text[lastIndex..match.Index]
                });
            }

            textBlock.Inlines.Add(new Run
            {
                Text = match.Value,
                Foreground = highlightBrush,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = isItalic ? FontStyle.Italic : FontStyle.Normal,
                FontFamily = isBold && boldFontFamily != null ? boldFontFamily : textBlock.FontFamily
            });

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            textBlock.Inlines.Add(new Run { Text = text.Substring(lastIndex) });
        }
    }
    private static Color ParseColor(string colorStr)
    {
        try
        {
            // 1) 资源键:优先在应用资源中查找 Brush/Color(支持 {ThemeResource} 间接传入的资源名)
            if (Application.Current.Resources.TryGetValue(colorStr, out var res))
            {
                if (res is SolidColorBrush brush) return brush.Color;
                if (res is Color c) return c;
            }

            // 2) 系统强调色(规格默认高亮色)
            if (string.Equals(colorStr, "SystemAccentColor", StringComparison.OrdinalIgnoreCase))
            {
                return new Windows.UI.ViewManagement.UISettings()
                    .GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
            }

            // 3) 十六进制 #RRGGBB / #AARRGGBB
            if (colorStr.StartsWith("#"))
            {
                if (colorStr.Length == 7)
                {
                    return ColorHelper.FromArgb(255,
                        Convert.ToByte(colorStr.Substring(1, 2), 16),
                        Convert.ToByte(colorStr.Substring(3, 2), 16),
                        Convert.ToByte(colorStr.Substring(5, 2), 16));
                }
                if (colorStr.Length == 9)
                {
                    return ColorHelper.FromArgb(
                        Convert.ToByte(colorStr.Substring(1, 2), 16),
                        Convert.ToByte(colorStr.Substring(3, 2), 16),
                        Convert.ToByte(colorStr.Substring(5, 2), 16),
                        Convert.ToByte(colorStr.Substring(7, 2), 16));
                }
            }

            // 4) 命名颜色(常见色,AOT 安全,无反射)
            if (NamedColors.TryGetValue(colorStr, out var named)) return named;

            return ColorHelper.FromArgb(255, 196, 171, 212);
        }
        catch
        {
            return ColorHelper.FromArgb(255, 196, 171, 212);
        }
    }

    /// <summary>
    /// 常见命名颜色映射(AOT 环境下不能反射访问 Colors 静态属性,故显式列出常用色)。
    /// </summary>
    private static readonly Dictionary<string, Color> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Black"] = Colors.Black,
        ["White"] = Colors.White,
        ["Gray"] = Colors.Gray,
        ["DarkGray"] = Colors.DarkGray,
        ["LightGray"] = Colors.LightGray,
        ["Red"] = Colors.Red,
        ["DarkRed"] = Colors.DarkRed,
        ["Green"] = Colors.Green,
        ["DarkGreen"] = Colors.DarkGreen,
        ["Blue"] = Colors.Blue,
        ["DarkBlue"] = Colors.DarkBlue,
        ["Yellow"] = Colors.Yellow,
        ["Orange"] = Colors.Orange,
        ["Purple"] = Colors.Purple,
        ["Pink"] = Colors.Pink,
        ["Cyan"] = Colors.Cyan,
        ["Teal"] = Colors.Teal,
        ["Magenta"] = Colors.Magenta,
        ["Brown"] = Colors.Brown,
        ["Transparent"] = Colors.Transparent
    };
}