using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using CC98.Kernel;

using Microsoft.UI.Xaml;

using Microsoft.Windows.Storage.Pickers;

namespace CC98.Services.Helpers;

/// <summary>
/// 文件上传结果(强类型,替代 magic string 判断)。
/// </summary>
public sealed class UploadResult
{
    /// <summary>是否上传成功。</summary>
    public bool Success { get; init; }

    /// <summary>上传成功后的文件 URL。</summary>
    public string Url { get; init; } = "";

    /// <summary>文件名。</summary>
    public string FileName { get; init; } = "";

    /// <summary>失败时的错误信息。</summary>
    public string Error { get; init; } = "";

    public static UploadResult Ok(string url, string fileName) => new() { Success = true, Url = url, FileName = fileName };

    public static UploadResult Fail(string error) => new() { Error = error };
}

/// <summary>
/// 统一文件上传服务:媒体类型映射、文件选择、流式上传。
/// SketchPage 与 ChatPage 共用,避免各页面重复实现。
/// </summary>
public static class FileUploadService
{
    private static readonly ApiService ApiService = App.Current.GetService<ApiService>();

    /// <summary>
    /// 按媒体类型返回文件选择器配置(过滤器 + 起始库)。
    /// </summary>
    /// <param name="label">媒体类型:img / video / audio / upload。</param>
    public static (List<string> Filters, PickerLocationId Location) GetPickSpec(string label)
    {
        List<string> filters = label switch
        {
            "img" => [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"],
            "video" => [".mp4", ".mkv", ".avi", ".mov", ".wmv"],
            "audio" => [".mp3", ".wav", ".m4a", ".flac", ".aac"],
            "upload" => [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar", ".7z"],
            _ => []
        };
        PickerLocationId location = label switch
        {
            "img" => PickerLocationId.PicturesLibrary,
            "video" => PickerLocationId.VideosLibrary,
            "audio" => PickerLocationId.MusicLibrary,
            "upload" => PickerLocationId.DocumentsLibrary,
            _ => PickerLocationId.ComputerFolder
        };
        return (filters, location);
    }

    /// <summary>
    /// 打开文件选择器,选择文件并上传。
    /// </summary>
    /// <param name="xamlRoot">当前窗口的 XamlRoot,用于初始化文件选择器。</param>
    /// <param name="label">媒体类型:img / video / audio / upload。</param>
    /// <returns>上传结果。用户取消时返回 Success=false 且 Error 为空。</returns>
    public static async Task<UploadResult> PickAndUploadAsync(XamlRoot xamlRoot, string label)
    {
        var (filters, location) = GetPickSpec(label);
        try
        {
            var picker = new FileOpenPicker(xamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                CommitButtonText = "上传",
                SuggestedStartLocation = location
            };
            foreach (var filter in filters) picker.FileTypeFilter.Add(filter);            var file = await picker.PickSingleFileAsync();
            if (file == null) return new UploadResult(); // 用户取消,非错误

            return await UploadAsync(file.Path);
        }
        catch (Exception ex)
        {
            return UploadResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 流式上传本地文件到 CC98。
    /// </summary>
    /// <param name="filePath">本地文件完整路径。</param>
    /// <returns>上传结果。</returns>
    public static async Task<UploadResult> UploadAsync(string filePath)
    {
        try
        {
            var url = ApiEndpoints.Forum.UploadFile;
            using var formData = new MultipartFormDataContent();
            // 流式上传:避免将整个文件读入内存
            using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new("multipart/form-data");
            formData.Add(fileContent, "files", Path.GetFileName(filePath));

            var res = await ApiService.Submit<List<string>>(url, formData);
            if (!res.IsSuccess || res.Data == null || res.Data.Count == 0)
                return UploadResult.Fail(res.Message);

            return UploadResult.Ok(res.Data[0], Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            return UploadResult.Fail(ex.Message);
        }
    }
}
