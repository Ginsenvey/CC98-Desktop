using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC98.Objects;

public class ThemePicture
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";

    /// <summary>
    /// 低分辨率缩略图:只按显示宽度 2 倍解码,避免把整张壁纸原图解码后缩放到 60x35。
    /// </summary>
    public BitmapImage Thumb => new(new Uri(FilePath)) { DecodePixelWidth = 120 };
}
