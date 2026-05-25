using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using CC98.Kernel.Authorize;
using System.Net.Http;

namespace CC98.Services.Helpers;

public static partial class UrlEx
{
    /// <summary>
    ///     判断是否是 Web URL 的正则表达式。
    /// </summary>
    [GeneratedRegex(@"^http(s)?\://", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex WebUriRegex { get; }

    /// <summary>
    ///     判断是否是 Appx 资源 URL 的正则表达式。
    /// </summary>
    [GeneratedRegex(@"^ms-app(x|data)\:///", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AppUriRegex { get; }

    /// <summary>
    ///     判断一个 URL 地址是否为 Web 字段。
    /// </summary>
    /// <param name="url">要判断的 URL 地址。</param>
    /// <returns>如果<paramref name="url" />是 Web 资源地址，返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    public static bool IsWebUrl(string url)
    {
        return WebUriRegex.IsMatch(url);
    }

    public static bool IsLocalPath(string path)
    {
        return AppUriRegex.IsMatch(path) ||
               Path.IsPathRooted(path) || // 绝对路径
               !Path.HasExtension(path); // 资源路径
    }

    /// <summary>
    ///     尝试从指定的路径加载表情图。
    /// </summary>
    /// <param name="path">要加载的表情图的路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>返回表情图的路径，如果不存在则返回修改后的路径。</returns>
    public static async Task<string> LocateEmojiAsync(string path, CancellationToken cancellationToken = default)
    {
        //应对表情包有两种格式的情况
        if (await IsResourceExistsAsync(path, cancellationToken)) return path;

        Uri uri = new(path);
        return uri.ChangeExtension(".gif").ToString();
    }

    /// <summary>
    ///     判断给定路径的资源是否存在。
    /// </summary>
    /// <param name="path">要检查的资源路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>如果资源存在，则为 <see langword="true" />；否则为 <see langword="false" />。</returns>
    public static async Task<bool> IsResourceExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageFile.GetFileFromApplicationUriAsync(new(path)).AsTask(cancellationToken);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("加载资源 {0} 时发生异常，错误消息: {1}", path, ex.Message);

            // 其他异常情况也视为不存在
            return false;
        }
    }

    /// <summary>
    ///     加载基于 Web 地址的图像。
    /// </summary>
    /// <param name="url">图像资源的 URL。</param>
    /// <param name="lowRes">是否使用低分辨率模式加载。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。操作结果为加载后的图片对象。</returns>
    public static async Task<BitmapSource> LoadWebImageAsync(string url, bool lowRes = false,
        CancellationToken cancellationToken = default)
    {
        using var client=new HttpClient();
        var res=await client.GetAsync(url, cancellationToken);
        var imageBytes = await res.Content.ReadAsByteArrayAsync();
        return await LoadFromBytesAsync(imageBytes, lowRes, cancellationToken);
    }

    public static async Task<BitmapSource> LoadLocalImage(string path, CancellationToken cancellationToken = default)
    {
        if (path.StartsWith("ms-appx:///") || path.StartsWith("ms-appdata:///"))
            // 应用资源路径
            return new BitmapImage(new(await LocateEmojiAsync(path, cancellationToken)));

        if (File.Exists(path))
        {
            // 本地文件路径
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenReadAsync();
            return await LoadFromStreamAsync(stream.AsStream(), cancellationToken);
        }

        // 尝试作为资源加载
        var uri = new Uri($"ms-appx:///Assets/{path}");
        return new BitmapImage(uri);
    }

    public static async Task<BitmapSource> LoadFromStreamAsync(Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var ras = new InMemoryRandomAccessStream();
        await stream.CopyToAsync(ras.AsStream(), cancellationToken);
        ras.Seek(0);

        var bitmapImage = new BitmapImage();
        await bitmapImage.SetSourceAsync(ras);
        bitmapImage.DecodePixelHeight = 48;
        bitmapImage.DecodePixelWidth = 48;
        return bitmapImage;
    }

    public static async Task<BitmapSource> LoadFromBytesAsync(byte[] bytes, bool lowRes = false,
        CancellationToken cancellationToken = default)
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

    extension(Uri uri)
    {
        /// <summary>
        ///     判断该地址是否为 Web 地址。
        /// </summary>
        /// <value>如果<paramref name="uri" />是 Web 资源地址，返回 <see langword="true" />；否则返回 <see langword="false" />。</value>
        public bool IsWebUri
        {
            get
            {
                return uri.Scheme.ToLowerInvariant() switch
                {
                    "http" or "https" => true,
                    _ => false
                };
            }
        }

        /// <summary>
        ///     判断一个 URL 是否指向本地资源。
        /// </summary>
        /// <value>如果 <paramref name="uri" /> 指向本地资源，则为 <see langword="true" />；否则为 <see langword="false" />>。</value>
        public bool IsLocalUri
        {
            get
            {
                return uri.Scheme.ToLowerInvariant() switch
                {
                    "ms-appx" or "ms-appdata" or "file" => true,
                    _ => uri.ToString().Contains("/Assets/", StringComparison.OrdinalIgnoreCase)
                };
            }
        }

        public Uri ChangeExtension(string newExtension)
        {
            // 获取原始URI的绝对路径（对于ms-appx协议，这类似于“/Assets/Images/image.gif”）
            var absolutePath = uri.AbsolutePath;
            // 检查是否有查询字符串，如果有需要保留（但通常ms-appx资源URI没有查询字符串）
            var query = uri.Query;
            // 修改文件路径：去掉原扩展名，加上新扩展名
            var lastDotIndex = absolutePath.LastIndexOf('.');
            var lastSlashIndex = absolutePath.LastIndexOf('/');
            // 确保我们只修改文件名的扩展名部分（即最后一个点号，且该点号在最后一个斜杠之后）
            if (lastDotIndex > lastSlashIndex)
                absolutePath = absolutePath[..lastDotIndex] + newExtension;
            else
                // 如果没有找到点号，或者点号不在文件名中（即在目录名中），则直接在后面添加扩展名
                absolutePath += newExtension;
            // 重新构建URI，使用原始协议（ms-appx）和新的路径
            // 注意：原始URI的主机部分（如果有）通常为空，因为ms-appx协议是本地资源
            var newUriString = $"{uri.Scheme}://{uri.Host}{absolutePath}{query}";
            return new(newUriString);
        }
    }
}