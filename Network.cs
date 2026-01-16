
using CC98.Kernel;
using Duende.IdentityModel.Client;
using HtmlAgilityPack;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.Devices.PointOfService;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;


namespace CC98.Kernel.Network;

public class VpnService : IDisposable
{
    private const string LoginAuthUrl = "https://webvpn.zju.edu.cn/login";
    private const string LoginPswUrl = "https://webvpn.zju.edu.cn/do-login";
    private const string LogoutUrl = "https://webvpn.zju.edu.cn/logout";
    public HttpClient client;
    public CookieContainer Jar;
    public bool Logined = false;//可以强行修改这个值来避开检验。由于从缓存中读取凭据不经过Login函数，需要在读取时手动修改这个值。
    public bool IsVpnEnabled = false;
    private bool _disposed = false;
    public Cookie Ticket => Jar.GetCookies(new Uri("https://webvpn.zju.edu.cn"))["wengine_vpn_ticketwebvpn_zju_edu_cn"] ?? new Cookie();
    public Cookie Route => Jar.GetCookies(new Uri("https://webvpn.zju.edu.cn"))["route"] ?? new Cookie();
    public VpnService()
    {
        Jar = new CookieContainer();
        //在此处启用Proxy以开始调试，否则流量不通过外部代理
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = Jar,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            //Proxy=new WebProxy("127.0.0.1:9000"), //启用系统代理
        };
        
        client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("Referer", "https://webvpn.zju.edu.cn/");
        client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
    }
    public async Task<string> LoginAsync(string username, string password, CancellationToken cts = default)
    {
        try
        {
            var res = await client.GetAsync(LoginAuthUrl);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                var html = await res.Content.ReadAsStringAsync();
                var param = GetRandCode(html);
                string encrpted_password = BuildPassword("wrdvpnisawesome!", password);
                var formData = new Dictionary<string, string>
            {
                {"_csrf", param.csrf},
                {"auth_type", param.auth_type},
                {"sms_code", ""},
                {"captcha","" },
                {"needCaptcha", "false"},
                {"captcha_id", param.captcha},
                {"username",username},
                {"password",encrpted_password }
            };
                var content = new FormUrlEncodedContent(formData);
                var login_res = await client.PostAsync(LoginPswUrl, content);
                if (login_res.StatusCode != HttpStatusCode.OK) return $"{login_res.StatusCode}:登录请求失败";
                string text = await login_res.Content.ReadAsStringAsync();
                if (ParseLoginResult(text))
                {
                    Logined = true;
                    return "1";
                }
                else
                {
                    return text;
                }
            }
            else
            {
                return "404:获取CSRF失败";
            }
        }
        catch (Exception ex)
        {
            return $"404:{ex.Message}";
        }
    }

    /// <summary>
    /// 注销会话
    /// </summary>
    /// <returns></returns>
    public async Task Logout()
    {
        try
        {
            var res = await client.GetAsync(LogoutUrl);
        }
        catch { }
    }
    /// <summary>
    /// 标准URL转换函数
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    public static string ConvertUrl(string origin)
    {
        var uri = new Uri(origin);
        string scheme = uri.Scheme;
        //处理协议和端口
        int port = uri.Port;
        string host = uri.Host;
        bool is_special_port = port > 0 &&
            !(uri.Scheme == "http" && port == 80) &&
            !(uri.Scheme == "https" && port == 443);
        string property = is_special_port ? $"{scheme}-{port}" : scheme;
        //处理路径和查询字符
        string suffix = uri.PathAndQuery;
        int qm = suffix.IndexOf('?');
        string path = qm >= 0 ? suffix[..qm] : suffix;
        string query = qm >= 0 ? suffix[qm..] : "";
        var pathSb = new System.Text.StringBuilder("/");
        foreach (var seg in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            pathSb.Append(Uri.EscapeDataString(seg)).Append('/');
        if (pathSb.Length > 1) pathSb.Length--;   // 去掉末尾多余 /
        string newPathAndQuery = pathSb.ToString() + query;

        string vpn_scheme = "https";
        string vpn_host = "webvpn.zju.edu.cn";
        string[] pathSegments = new[]
        {
            property,
            BuildPassword("wrdvpnisthebest!",host),
        };
        var builder = new UriBuilder(vpn_scheme, vpn_host);
        var sb = new System.Text.StringBuilder();
        foreach (var seg in pathSegments)
            sb.Append('/').Append(Uri.EscapeDataString(seg));
        builder.Path = sb.ToString();
        Uri fullUri = builder.Uri;
        string prifix = fullUri.ToString();
        return prifix + newPathAndQuery;
    }

    /// <summary>
    /// 检查是否内网环境。
    /// </summary>
    /// <param name="UseVpn"></param>
    /// <returns></returns>
    public async Task<string> CheckNetwork(bool UseVpn)
    {
        string Mirror_Url = "https://mirrors.zju.edu.cn/api/is_campus_network";
        string target_uri = UseVpn ? ConvertUrl(Mirror_Url) : Mirror_Url;
        
        try
        {
            var response = await client.GetAsync(target_uri);
            if (response.IsSuccessStatusCode)
            {
                string res_text = await response.Content.ReadAsStringAsync();
                if (res_text == "0")
                {
                    return "0";
                }
                else if (res_text == "1" || res_text == "2")
                {
                    return "1";
                }
                else
                {
                    ValidationHelper.Log("网络检查出错", $"非法返回内容：{res_text}");
                    return "404:非法返回";
                }
            }
            else
            {
                return "404:请求失败";
            }
        }
        catch (Exception ex)
        {
            return $"404:{ex.Message}";
        }


    }
    public static bool ParseLoginResult(string json)
    {
        if (string.IsNullOrEmpty(json)) return false;
        var dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        if (dic == null) return false;
        if (dic.TryGetValue("success", out var r))
        {
            if (r is bool _r)
            {
                return _r;
            }
        }
        return false;
    }
    public static (string csrf, string captcha, string auth_type) GetRandCode(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var csrf_node = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='_csrf']");
        var captcha_node = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='captcha_id']");
        var auth_type_node = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='auth_type']");
        string csrf = csrf_node?.GetAttributeValue("value", string.Empty) ?? string.Empty;
        string captcha = captcha_node?.GetAttributeValue("value", string.Empty) ?? string.Empty;
        string auth_type = auth_type_node?.GetAttributeValue("value", string.Empty) ?? string.Empty;
        return (csrf, captcha, auth_type);
    }
    /// <summary>
    /// 拼接密钥。需要指明截取长度，并默认IV,Key和前缀一致。
    /// </summary>
    /// <param name="Prefix"></param>
    /// <param name="PlainText"></param>
    /// <returns></returns>
    public static string BuildPassword(string Prefix, string PlainText)
    {
        //裁剪长度为2倍明文长度
        int SliceLength = 2 * PlainText.Length;
        string prifix_hex = StringToAscll(Prefix);
        string full_core = EncryptStringToHex(PlainText, Prefix, Prefix);
        string core = full_core[..Math.Min(full_core.Length, SliceLength)];
        return $"{prifix_hex}{core}";
    }
    /// <summary>
    /// 核心加密实现。
    /// </summary>
    /// <param name="PlainText"></param>
    /// <param name="Key"></param>
    /// <param name="IV"></param>
    /// <returns></returns>
    public static string EncryptStringToHex(string PlainText, string Key, string IV)
    {
        byte[] iv = Encoding.UTF8.GetBytes(IV.PadRight(16, ' ')[..16]);
        byte[] key = Encoding.UTF8.GetBytes(Key.PadRight(16, ' ')[..16]);
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CFB;   // CFB 模式
            aes.Padding = PaddingMode.None; // 允许任意长度明文
            aes.FeedbackSize = 128;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                //使用提前填充
                byte[] plainBytes = PadWithZeros(PlainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
                return Convert.ToHexString(ms.ToArray()).ToLower();
            }
        }
    }
    /// <summary>
    /// 将输入字符串按 UTF-8 编码后补足到 16 字节整数倍，不足部分补 0x00。
    /// </summary>
    public static byte[] PadWithZeros(string plainText)
    {
        if (plainText == null) throw new ArgumentNullException(nameof(plainText));

        byte[] raw = Encoding.UTF8.GetBytes(plainText);
        int len = raw.Length;
        int pad = 16 - (len & 15);          // 计算需要补多少字节
        if (pad == 16) pad = 0;             // 刚好 16 的倍数时不补

        byte[] padded = new byte[len + pad];
        Array.Copy(raw, 0, padded, 0, len); // 原始数据
                                            // 剩余部分默认为 0，无需再写
        return padded;
    }
    /// <summary>
    /// 将字符串分别转化为ACSLL码。
    /// </summary>
    /// <param name="Origin"></param>
    /// <returns></returns>
    public static string StringToAscll(string Origin)
    {

        byte[] asciiBytes = Encoding.ASCII.GetBytes(Origin);
        var sb = new StringBuilder(asciiBytes.Length * 2);
        foreach (byte b in asciiBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
    public async Task<MediaSource> GetSourceAsync(string url)
    {
        try
        {
            string targeturl = CCloginservice.vpn.IsVpnEnabled ? VpnService.ConvertUrl(url) : url;
            using (var res = await CCloginservice.vpn.client.GetAsync(targeturl, HttpCompletionOption.ResponseHeadersRead))
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
                    if (r)
                    {
                        using (var res1 = await CCloginservice.vpn.client.GetAsync(targeturl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            if (res1.IsSuccessStatusCode)
                            {
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
                
            }


        }
        catch{}
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
                    var _res= await client.GetAsync(targetUrl);
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
    public async Task<HttpResponseMessage> GetAsync(string url, bool webvpn = false)
    {
        var res= await SendRequestAsync(HttpMethod.Get, url, null);
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
        var res= await SendRequestAsync(HttpMethod.Post, url,content);
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
        var res= await client.SendAsync(request);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            var r=await Coordinator.SafeSlientAuth();
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
        using var request = new HttpRequestMessage(HttpMethod.Delete,targetUrl);
        var res= await client.SendAsync(request);
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
    public async Task<HttpResponseMessage> PutAsync(string url,StringContent content )
    {
        if (!Logined && IsVpnEnabled)
            throw new Exception("WebVPN未连接");
        string targetUrl = IsVpnEnabled ? ConvertUrl(url) : url;
        using var request = new HttpRequestMessage(HttpMethod.Put, targetUrl);
        request.Content = content;
        var res=await client.SendAsync(request);
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
    public static HttpRequestMessage CloneRequest(HttpRequestMessage original)
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
            clone.Options.TryAdd(property.Key,property.Value);
        }

        return clone;
    }
    

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                client?.Dispose();
            }
            _disposed = true;
        }
    }

}


public class Coordinator
{
    // 使用Lazy<Task<bool>>确保线程安全的单次执行和结果共享
    private static Lazy<Task<bool>> _refreshTask;

    // 用于协调刷新的锁对象
    private static readonly object _lock = new object();

    // 刷新函数
    private static async Task<bool> SilentAuth()
    {
        try
        {
            var r = await CCloginservice.RefreshToken();
            if (r == "1")
            {
                return true;
            }
            else if (r.StartsWith("2"))
            {
                ApplicationData.Current.LocalSettings.Values["IsActive"] = 0;
                AppNotification notification = new AppNotificationBuilder()
                .AddText("登录过期")
                .AddText("请重新登录。")
                .BuildNotification();
                AppNotificationManager.Default.Show(notification);
                Application.Current.Exit();
                return false;
            }
            else
            {
                return false;
            }
        }
        finally
        {
            // 重置刷新状态，允许下次刷新
            lock (_lock)
            {
                _refreshTask = null;
            }
        }
    }

    // 公开的安全调用接口
    public static Task<bool> SafeSlientAuth()
    {
        lock (_lock)
        {
            // 如果当前没有进行中的刷新任务，创建新任务
            if (_refreshTask == null || _refreshTask.Value.IsCompleted)
            {
                _refreshTask = new Lazy<Task<bool>>(() =>SilentAuth());
            }

            return _refreshTask.Value;
        }
    }
}
