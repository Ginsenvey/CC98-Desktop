using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Windows.Media.Core;
using Windows.Storage.Streams;

namespace CC98.Kernel.Network;

public sealed partial class VpnService
{
    /// <summary>
    /// 协调器对象。
    /// </summary>
    private Coordinator Coordinator { get; } = Coordinator.Instance;

    public async Task<MediaSource?> GetSourceAsync(string url)
    {
        if (!IsLoggedIn && IsVpnEnabled)
            throw new("WebVPN未连接");
        try
        {
            var targeturl = LoginService.Vpn.IsVpnEnabled ? ConvertUrl(url) : url;
            using var res = await LoginService.Vpn.HttpClient.GetAsync(targeturl, HttpCompletionOption.ResponseHeadersRead);
            if (res.IsSuccessStatusCode)
            {
                var memoryStream = new InMemoryRandomAccessStream();
                using (var contentStream = await res.Content.ReadAsStreamAsync())
                {
                    await ValidationHelper.CopyStreamToRandomAccessStream(contentStream, memoryStream);
                }
                var source = MediaSource.CreateFromStream(memoryStream, res.Content.Headers.ContentType?.MediaType);
                return source;
            }
            else if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var r = await Coordinator.SafeSilentAuth();
                if (!r) return null;
                using var res1 = await LoginService.Vpn.HttpClient.GetAsync(targeturl, HttpCompletionOption.ResponseHeadersRead);
                if (!res1.IsSuccessStatusCode) return null;
                var memoryStream = new InMemoryRandomAccessStream();
                using (var contentStream = await res1.Content.ReadAsStreamAsync())
                {
                    await ValidationHelper.CopyStreamToRandomAccessStream(contentStream, memoryStream);
                }
                var source = MediaSource.CreateFromStream(memoryStream, res1.Content.Headers.ContentType?.MediaType);
                return source;
            }
        }
        catch { }
        return null;

    }
    public async Task<byte[]> GetByteArrayAsync(string url)
    {
        if (!IsLoggedIn && IsVpnEnabled)
            throw new("WebVPN未连接");
        var targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        try
        {
            var res = await HttpClient.GetAsync(targetUrl);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var r = await Coordinator.SafeSilentAuth();
                if (r)
                {
                    res = await HttpClient.GetAsync(targetUrl);
                    return await res.Content.ReadAsByteArrayAsync();
                }
            }
            else if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadAsByteArrayAsync();
            }
            else
            {
                await App.Logger.WriteAsync("Http", "获取字节数据失败", $"状态码：{res.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("Http", "获取字节数据失败", ex.Message);
        }
        return null;

    }
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var res = await SendRequestAsync(HttpMethod.Get, url, null);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSilentAuth();
            if (r)
            {
                return await SendRequestAsync(HttpMethod.Get, url, null);
            }
        }
        return res;
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        var res = await SendRequestAsync(HttpMethod.Post, url, content);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSilentAuth();
            if (r)
            {
                return await SendRequestAsync(HttpMethod.Post, url, content);
            }
        }
        return res;
    }
    public async Task<HttpResponseMessage> SendAsync(string url, HttpRequestMessage request)
    {
        if (!IsLoggedIn && IsVpnEnabled)
            throw new("WebVPN未连接");
        var targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        request.RequestUri = new(targetUrl);
        var res = await HttpClient.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSilentAuth();
            if (r)
            {
                return await HttpClient.SendAsync(CloneRequest(request));
            }
        }
        return res;
    }
    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        if (!IsLoggedIn && IsVpnEnabled)
            throw new("WebVPN未连接");
        var targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        using var request = new HttpRequestMessage(HttpMethod.Delete, targetUrl);
        var res = await HttpClient.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSilentAuth();
            if (r)
            {
                return await HttpClient.SendAsync(CloneRequest(request));
            }
        }
        return res;
    }
    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent? content)
    {
        if (!IsLoggedIn && IsVpnEnabled)
            throw new("WebVPN未连接");
        var targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        using var request = new HttpRequestMessage(HttpMethod.Put, targetUrl);
        request.Content = content;
        var res = await HttpClient.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSilentAuth();
            if (r)
            {
                return await HttpClient.SendAsync(CloneRequest(request));
            }
        }
        return res;
    }
    /// <summary>
    /// 请求发送核心函数。
    /// </summary>
    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string url,
        HttpContent content)
    {
        if (!IsLoggedIn && IsVpnEnabled)
            throw new("WebVPN未连接");
        var targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        using var request = new HttpRequestMessage(method, targetUrl);
        if (method == HttpMethod.Post && content != null)
        {
            request.Content = content;
        }

        var res = await HttpClient.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSilentAuth();
            if (r)
            {
                return await HttpClient.SendAsync(CloneRequest(request));
            }
        }
        return res;
    }
    private static HttpRequestMessage CloneRequest(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        // 复制内容
        if (original.Content != null)
        {
            var ms = new MemoryStream();
            original.Content.CopyToAsync(ms).Wait();
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            // 复制内容头
            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // 复制请求头
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 复制属性
        foreach (var property in original.Options)
        {
            clone.Options.TryAdd(property.Key, property.Value);
        }

        return clone;
    }
}
