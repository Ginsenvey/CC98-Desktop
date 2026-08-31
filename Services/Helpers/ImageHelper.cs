using CC98.Kernel;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CC98.Services.Helpers;

public class ImageHelper
{
    /// <summary>
    /// 加载基于 Web 地址的图像。
    /// </summary>
    /// <param name="url">图像资源的 URL。</param>
    /// <param name="lowRes">是否使用低分辨率模式加载。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。操作结果为加载后的图片对象。</returns>
    public static async Task<BitmapSource> LoadWebImageAsync(string url, bool lowRes = false,CancellationToken cancellationToken = default)
    {
        var apiService = App.Current.GetService<ApiService>();
        var imageBytes = await apiService.GetBytesAsync(url, cancellationToken);
        return await LoadFromBytesAsync(imageBytes!, lowRes, cancellationToken);
    }

    public static async Task<BitmapSource> LoadLocalImage(string path,bool lowRes = false, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine("开始加载本地图像");
        if (path.StartsWith("ms-appx:///") || path.StartsWith("ms-appdata:///"))
        {
            return new BitmapImage(new(await UrlEx.LocateEmojiAsync(path, cancellationToken)));
        }

        if (File.Exists(path))
        {
            Debug.WriteLine($"存在文件: {path}");
            // 本地文件路径
            var file = await StorageFile.GetFileFromPathAsync(path);
            using IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
            BitmapImage bitmapImage = new();
            if (lowRes)
            {
                bitmapImage.DecodePixelHeight = 64;
                bitmapImage.DecodePixelWidth = 64;
            }
            await bitmapImage.SetSourceAsync(fileStream);
            return bitmapImage;
        }

        // 尝试作为资源加载
        var uri = new Uri($"ms-appx:///Assets/{path}");
        return new BitmapImage(uri);
    }
  

    public static async Task<BitmapSource> LoadFromBytesAsync(byte[] bytes, bool lowRes = false,CancellationToken cancellationToken = default)
    {
        using var ras = new InMemoryRandomAccessStream();
        await ras.WriteAsync(bytes.AsBuffer()).AsTask(cancellationToken);
        ras.Seek(0);
        var bitmapImage = new BitmapImage();
        if (lowRes)
        {
            bitmapImage.DecodePixelHeight = 64;
            bitmapImage.DecodePixelWidth = 64;
        }

        await bitmapImage.SetSourceAsync(ras);
        return bitmapImage;
    }
}
