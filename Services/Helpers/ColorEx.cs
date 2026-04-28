using System;
using ABI.Windows.UI;

namespace CC98.Services.Helpers;

public static class ColorEx
{

    public static string GenerateMorandiColorHex()
    {
        var random = Random.Shared;
        double hue = random.Next(0, 360);

        // 低饱和度（10-30%）
        var saturation = random.Next(20, 60) / 100.0;

        // 中低明度（50-70%）
        var lightness = random.Next(50, 70) / 100.0;
        // 将 HSL 转换为 RGB
        var (r, g, b) = HslToRgb(hue, saturation, lightness);


        // 转换为十六进制
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    // HSL 转 RGB 辅助函数
    private static (byte r, byte g, byte b) HslToRgb(double h, double s, double l)
    {
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = l - c / 2;

        (double r, double g, double b) rgb = h switch
        {
            < 60 => (c, x, 0),
            < 120 => (x, c, 0),
            < 180 => (0, c, x),
            < 240 => (0, x, c),
            < 300 => (x, 0, c),
            _ => (c, 0, x)
        };

        var r = (byte)((rgb.r + m) * 255);
        var g = (byte)((rgb.g + m) * 255);
        var b = (byte)((rgb.b + m) * 255);

        return (r, g, b);
    }
}