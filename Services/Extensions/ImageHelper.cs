using CC98.Kernel;
using CC98.Kernel.Authorize;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CC98.Services.Extensions;

public class ImageHelper
{
    /// <summary>
    ///     复制在线图片到剪贴板
    /// </summary>
    /// <param name="imageUrl">图片URL</param>
    /// <returns>是否成功</returns>
    public static async Task<bool> CopyImageToClipboardAsync(string imageUrl)
    {
        try
        {
            var apiService = App.Current.GetService<ApiService>();
            var imageBytes = await apiService.GetBytesAsync(imageUrl);

            using var stream = new MemoryStream(imageBytes);
            var randomAccessStream = new InMemoryRandomAccessStream();
            await randomAccessStream.WriteAsync(imageBytes.AsBuffer());
            randomAccessStream.Seek(0);

            // 创建数据包
            var dataPackage = new DataPackage();
            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(randomAccessStream));

            // 设置描述信息
            dataPackage.Properties.Title = "CC98图片";
            dataPackage.Properties.Description = "已复制的图片";

            // 复制到剪贴板
            Clipboard.SetContent(dataPackage);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"复制图片失败: {ex.Message}");
            return false;
        }
    }
    //TODO:rewrite
    /// <summary>
    ///     下载图片到用户下载文件夹
    /// </summary>
    /// <param name="imageUrl">图片URL</param>
    /// <param name="fileName">文件名（可选，不指定则从URL自动提取）</param>
    /// <returns>下载成功返回文件路径，失败返回null</returns>
    public static async Task<string?> DownloadImagesAsync(string imageUrl, string fileName = null)
    {
        try
        {
            // 获取用户的下载文件夹
            var downloadsFolder = await GetDownloadsFolderAsync();
            if (downloadsFolder == null)
            {
                Debug.WriteLine("无法访问下载文件夹");
                return null;
            }

            // 如果没有指定文件名，从URL中提取
            if (string.IsNullOrEmpty(fileName)) fileName = ExtractFileNameFromUrl(imageUrl);

            // 处理文件名冲突
            fileName = await GetUniqueFileNameAsync(downloadsFolder, fileName);

            // 创建文件
            var file = await downloadsFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            var apiService = App.Current.GetService<ApiService>();
            var imageBytes = await apiService.GetBytesAsync(imageUrl);
            await using var stream = await file.OpenStreamForWriteAsync();
            await stream.WriteAsync(imageBytes);
            return file.Path;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"下载图片失败: {ex.Message}");
            await App.Logger.WriteAsync("ImageDownloader", "图片下载出错", ex.Message);
            return null;
        }
    }

    /// <summary>
    ///     获取用户的下载文件夹
    /// </summary>
    private static async Task<StorageFolder> GetDownloadsFolderAsync()
    {
        try
        {
            return KnownFolders.SavedPictures;
        }
        catch
        {
            // 如果 KnownFolders 不可用
            try
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var downloadsPath = Path.Combine(userProfile, "Downloads");
                return await StorageFolder.GetFolderFromPathAsync(downloadsPath);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     从URL中提取文件名
    /// </summary>
    private static string ExtractFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);

            // 如果文件名无效，生成默认文件名
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
                fileName = $"CC98_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

            return fileName;
        }
        catch
        {
            return $"CC98_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        }
    }

    /// <summary>
    ///     确保文件名唯一（如果文件已存在，添加数字后缀）
    /// </summary>
    private static async Task<string> GetUniqueFileNameAsync(StorageFolder folder, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var counter = 1;
        var uniqueFileName = fileName;

        while (await folder.TryGetItemAsync(uniqueFileName) != null)
        {
            uniqueFileName = $"{baseName}_{counter}{extension}";
            counter++;
        }

        return uniqueFileName;
    }
}