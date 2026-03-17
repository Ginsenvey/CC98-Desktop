using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.Streams;

namespace CC98.Kernel.Network;

public partial class VpnService
{
    public async Task<MediaSource?> GetSourceAsync(string url)
    {
        try
        {
            string targeturl = LoginService.vpn.IsVpnEnabled ? VpnService.ConvertUrl(url) : url;
            using (var res = await LoginService.vpn.client.GetAsync(targeturl, HttpCompletionOption.ResponseHeadersRead))
            {
                if (res.IsSuccessStatusCode)
                {
                    var memory_stream = new InMemoryRandomAccessStream();
                    using (var content_stream = await res.Content.ReadAsStreamAsync())
                    {
                        await ValidationHelper.CopyStreamToRandomAccessStream(content_stream, memory_stream);
                    }
                    var source = MediaSource.CreateFromStream(memory_stream, res.Content.Headers.ContentType?.MediaType);
                    return source;
                }
                else if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var r = await Coordinator.SafeSlientAuth();
                    if (!r) return null;
                    using (var res1 = await LoginService.vpn.client.GetAsync(targeturl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!res1.IsSuccessStatusCode) return null;
                        var memory_stream = new InMemoryRandomAccessStream();
                        using (var content_stream = await res1.Content.ReadAsStreamAsync())
                        {
                            await ValidationHelper.CopyStreamToRandomAccessStream(content_stream, memory_stream);
                        }
                        var source = MediaSource.CreateFromStream(memory_stream, res1.Content.Headers.ContentType?.MediaType);
                        return source;
                    }
                }
            }
        }
        catch {}
        return null;

    }
    public async Task<byte[]> GetByteArrayAsync(string url)
    {
        if (!Logined)
            throw new InvalidOperationException("VPN未登录");

        string targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        try
        {
            var res = await client.GetAsync(targetUrl);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var r = await Coordinator.SafeSlientAuth();
                if (r)
                {
                    var _res = await client.GetAsync(targetUrl);
                    return await _res.Content.ReadAsByteArrayAsync();
                }
            }
            else if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadAsByteArrayAsync();
            }
        }
        catch { }
        return null;

    }
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var res = await SendRequestAsync(HttpMethod.Get, url, null);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSlientAuth();
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
            var r = await Coordinator.SafeSlientAuth();
            if (r)
            {
                return await SendRequestAsync(HttpMethod.Post, url, content);
            }
        }
        return res;
    }
    public async Task<HttpResponseMessage> SendAsync(string url, HttpRequestMessage request)
    {
        if (!Logined && IsVpnEnabled)
            throw new Exception("WebVPN未连接");
        string targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        request.RequestUri = new Uri(targetUrl);
        var res = await client.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSlientAuth();
            if (r)
            {
                return await client.SendAsync(CloneRequest(request));
            }
        }
        return res;
    }
    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        if (!Logined && IsVpnEnabled)
            throw new Exception("WebVPN未连接");
        string targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        using var request = new HttpRequestMessage(HttpMethod.Delete, targetUrl);
        var res = await client.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSlientAuth();
            if (r)
            {
                return await client.SendAsync(CloneRequest(request));
            }
        }
        return res;
    }
    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent? content)
    {
        if (!Logined && IsVpnEnabled)
            throw new Exception("WebVPN未连接");
        string targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        using var request = new HttpRequestMessage(HttpMethod.Put, targetUrl);
        request.Content = content;
        var res = await client.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSlientAuth();
            if (r)
            {
                return await client.SendAsync(CloneRequest(request));
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
        if (!Logined && IsVpnEnabled)
            throw new Exception("WebVPN未连接");
        string targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        using var request = new HttpRequestMessage(method, targetUrl);
        if (method == HttpMethod.Post && content != null)
        {
            request.Content = content;
        }

        var res = await client.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r = await Coordinator.SafeSlientAuth();
            if (r)
            {
                return await client.SendAsync(CloneRequest(request));
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
