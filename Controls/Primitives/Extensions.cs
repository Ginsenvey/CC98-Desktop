using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;
namespace CC98.Share.Extensions;
public static class DispatcherQueueExtensions
{
    public static async Task EnqueueAsync(this DispatcherQueue dispatcher,
        Action action,
        DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (!dispatcher.TryEnqueue(priority, () =>
        {
            try
            {
                action();
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue the action"));
        }

        await tcs.Task;
    }
}


public static class StringExtensions
{
    /// <summary>
    /// 判断字符串是否为合法的 URL（支持 http、https、ms-appx、ms-appdata）
    /// </summary>
    /// <param name="input">要检查的字符串</param>
    /// <param name="allowRelative">是否允许相对路径（如 "/Assets/image.jpg"）</param>
    /// <returns>是否为合法 URL</returns>
    public static bool IsValidUrl(this string input, bool allowRelative = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // 1. 检查是否为绝对 URI
        if (Uri.TryCreate(input, UriKind.Absolute, out var absoluteUri))
        {
            return IsSupportedScheme(absoluteUri.Scheme);
        }

        // 2. 如果需要检查相对路径
        if (allowRelative)
        {
            // 检查是否是合法的相对路径（以 / 或 ./ 或 ../ 开头）
            if (input.StartsWith("/") || input.StartsWith("./") || input.StartsWith("../"))
            {
                // 进一步验证路径格式
                return IsValidRelativePath(input);
            }

            // 尝试作为相对 URI 解析
            if (Uri.TryCreate(input, UriKind.Relative, out var relativeUri))
            {
                // 相对 URI 只要不是绝对 URI 且格式正确就算合法
                return !string.IsNullOrEmpty(relativeUri.ToString());
            }
        }

        return false;
    }

    /// <summary>
    /// 判断字符串是否为绝对 URL（必须包含协议）
    /// </summary>
    public static bool IsAbsoluteUrl(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return Uri.TryCreate(input, UriKind.Absolute, out var uri)
            && IsSupportedScheme(uri.Scheme);
    }

    /// <summary>
    /// 判断字符串是否为 ms-appx 协议的 URL
    /// </summary>
    public static bool IsMsAppxUrl(this string input)
    {
        return IsUrlWithScheme(input, "ms-appx");
    }

    /// <summary>
    /// 判断字符串是否为 ms-appdata 协议的 URL
    /// </summary>
    public static bool IsMsAppDataUrl(this string input)
    {
        return IsUrlWithScheme(input, "ms-appdata");
    }

    /// <summary>
    /// 判断字符串是否为 HTTP/HTTPS URL
    /// </summary>
    public static bool IsHttpUrl(this string input)
    {
        return IsUrlWithScheme(input, "http") || IsUrlWithScheme(input, "https");
    }

    /// <summary>
    /// 判断字符串是否为文件路径（本地文件）
    /// </summary>
    public static bool IsFilePath(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // 检查 Windows 文件路径格式
        if (input.Length >= 3 &&
            char.IsLetter(input[0]) &&
            input[1] == ':' &&
            (input[2] == '\\' || input[2] == '/'))
        {
            return true;
        }

        // 检查 UNC 路径
        if (input.StartsWith(@"\\") || input.StartsWith("//"))
        {
            return true;
        }

        return false;
    }

    #region 私有辅助方法

    private static bool IsSupportedScheme(string scheme)
    {
        return scheme switch
        {
            "http" => true,
            "https" => true,
            "ms-appx" => true,
            "ms-appdata" => true,
            _ => false
        };
    }

    private static bool IsUrlWithScheme(string input, string scheme)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return uri.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsValidRelativePath(string path)
    {
        // 简单的相对路径验证
        // 不允许包含非法字符
        char[] invalidPathChars = System.IO.Path.GetInvalidPathChars();

        foreach (char c in path)
        {
            if (Array.IndexOf(invalidPathChars, c) >= 0)
                return false;
        }

        // 检查路径遍历攻击
        if (path.Contains("..\\") || path.Contains("../") && !path.StartsWith("../"))
        {
            // 允许以 ../ 开头，但不允许在中间出现
            return false;
        }

        return true;
    }

    #endregion
}