using CC98.Services.Helpers;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CC98.Controls.Primitives;

public class SmartMarkdownImageProvider : IImageProvider
{
    public async Task<Image> GetImage(string url)
    {
        BitmapSource? result=null;
        try
        {
            if (UrlEx.IsLocalPath(url))
                result = await UrlEx.LoadLocalImage(url);
            else if (UrlEx.IsWebUrl(url)) result = await UrlEx.LoadWebImageAsync(url);
        }
        catch
        {
            result = null;
        }
        //如果加载失败，返回一个占位图
        return new Image
        {
            Source = result ?? new BitmapImage(new Uri("ms-appx:///Assets/Images/placeholder.png")),
            MaxWidth = 400,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
        };
    }
    //需要改进，请在urlex类中添加判断内链的方法
    public bool ShouldUseThisProvider(string url)
    {
        return url.Contains("cc98");
    }
}
