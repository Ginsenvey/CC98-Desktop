using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using CC98.Kernel.Authorize;
using CC98.Objects;
using CC98.Services.Extensions;
using CC98.Kernel.Network;

namespace CC98.Kernel;

/// <summary>
///     对Http请求做二次封装，提供给UI层。
/// </summary>
public static class RequestSender
{
    public static HttpClient HttpClient => App.GetService<IVpnService>().HttpClient;
    /// <summary>
    /// 封装通用GET请求，并实现自动错误处理。
    /// </summary>
    public static async Task<ApiResponse<T>> Fetch<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await HttpClient.GetAsync(endpoint, cancellationToken);
            return await Deserialize<T>(res, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"网络错误: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return ApiResponse<T>.Fail($"数据解析错误: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ApiResponse<T>.Fail("请求超时");
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("Kernel", "其他错误", ex.Message);
            return ApiResponse<T>.Fail($"系统错误: {ex.Message}");
        }
    }

    /// <summary>
    ///     适用于PUT。
    /// </summary>
    public static async Task<ApiResponse> Put(string endpoint, HttpContent? content,CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await HttpClient.PutAsync(endpoint, content, cancellationToken);
            var json = await res.Content.ReadAsStringAsync();
            return res.IsSuccessStatusCode
                ? ApiResponse.Success(json)
                : ApiResponse.Fail((res.ReasonPhrase ?? "响应失败") + ":" + json, (int)res.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse.Fail($"网络错误: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return ApiResponse.Fail($"数据解析错误: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ApiResponse.Fail("请求超时");
        }
        catch (Exception ex)
        {
            return ApiResponse.Fail($"系统错误: {ex.Message}");
        }
    }

    /// <summary>
    ///     适用于DELETE。
    /// </summary>
    public static async Task<ApiResponse> Delete(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await HttpClient.DeleteAsync(endpoint, cancellationToken);
            var json = await res.Content.ReadAsStringAsync();
            return res.IsSuccessStatusCode
                ? ApiResponse.Success(json)
                : ApiResponse.Fail((res.ReasonPhrase ?? "响应失败") + ":" + json, (int)res.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse.Fail($"网络错误: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return ApiResponse.Fail($"数据解析错误: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ApiResponse.Fail("请求超时");
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("Kernel", "其他错误", ex.Message);
            return ApiResponse.Fail($"系统错误: {ex.Message}");
        }
    }

    /// <summary>
    ///     T是返回类型，不是提交数据类型。提交数据类型由HttpContent的具体实现决定
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="endpoint"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static async Task<ApiResponse<T>> Submit<T>(string endpoint, HttpContent content, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await HttpClient.PostAsync(endpoint, content, cancellationToken);
            return await Deserialize<T>(res, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"网络错误: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return ApiResponse<T>.Fail($"数据解析错误: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ApiResponse<T>.Fail("请求超时");
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("Kernel", "其他错误", ex.Message);
            return ApiResponse<T>.Fail($"系统错误: {ex.Message}");
        }
    }

    public static async Task<ApiResponse<T>> Deserialize<T>(HttpResponseMessage res,
        CancellationToken cancellationToken = default)
    {
        if (!res.IsSuccessStatusCode) return ApiResponse<T>.Fail(res.ReasonPhrase ?? "响应失败", (int)res.StatusCode);

        try
        {
            var obj = await res.Content.ReadFromJsonAsync(typeof(T), CC98JsonContext.Default, cancellationToken);
            return ApiResponse<T>.Success((T?)obj!);
        }
        catch (JsonException ex)
        {
            return ApiResponse<T>.Fail(ex.Message);
        }
    }

}
//需要逐步迁移
public static class ValidationHelper
{
    public static async Task CopyStreamToRandomAccessStream(Stream input, IRandomAccessStream output)
    {
        var buffer = new byte[16 * 1024];
        int bytesRead;

        // 获取输出流的写入器
        using var outputStream = output.GetOutputStreamAt(0);
        using var writer = new DataWriter(outputStream);
        while ((bytesRead = await input.ReadAsync(buffer)) > 0)
        {
            writer.WriteBytes(buffer.AsSpan(0, bytesRead).ToArray());
            await writer.StoreAsync();
            await outputStream.FlushAsync();
        }

        await writer.FlushAsync();
    }


    public static string GetValue(ApplicationDataContainer container, string key)
    {
        if (container.Values.TryGetValue(key, out var token))
            if (token != null)
            {
                var tokenString = token.ToString();
                if (!string.IsNullOrEmpty(tokenString)) return tokenString;
            }

        return "0";
    }


   

    public static string GetValue(NameValueCollection collection, string key)
    {
        if (collection.AllKeys.Contains(key))
        {
            var value = collection[key];
            if (value is string str) return str;
        }

        return "0";
    }
}


//需要逐步迁移
public static class LinkAnalyzer
{
    public static KeyValuePair<string, string> Parse(string link)
    {
        if (!string.IsNullOrEmpty(link))
        {
            if (link.Contains("user/name"))
            {
                var pattern = @"https:\/\/api\.cc98\.org\/user\/name\/([^\/\s]+)";
                var matches = Regex.Matches(link, pattern);
                if (matches.Count == 1)
                {
                    var username = matches[0].Groups[1].Value;
                    return new("user", username);
                }

                return new("null", link);
            }

            if (link.Contains("/topic/") && !link.Contains("#"))
            {
                var pattern = @"\/topic\/([^\/\s]+)";
                var matches = Regex.Matches(link, pattern);
                if (matches.Count == 1)
                {
                    var pid = matches[0].Groups[1].Value;
                    return new("topic", pid);
                }

                return new("null", link);
            }

            if (link.Contains("#"))
            {
                var match = Regex.Match(link, @"/topic/(\d{7})/(\d+)#(\d+)");
                if (match.Success)
                {
                    var numberAfterHash = match.Groups[1].Value;
                    return new("anchor", link); //返回索引楼层
                }

                return new("null", link);
            }

            if (link.Contains("https://www.bilibili.com/video")) return new("backlink", "bili");

            if (link.Contains("board"))
            {
                var match = Regex.Match(link, @"\/board\/(\d{2,3})");
                if (match.Success)
                {
                    var boardId = match.Groups[1].Value;
                    return new("board", boardId);
                }

                return new("null", link);
            }

            return new("null", link);
        }

        return new("null", link);
    }
}