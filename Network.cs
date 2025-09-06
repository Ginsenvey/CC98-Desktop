using CCkernel;
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
using System.Threading.Tasks;
using System.Xml;
using Windows.Devices.PointOfService;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using static System.Net.WebRequestMethods;


public class VpnService : IDisposable
{
    
    private const string LoginAuthUrl = "https://webvpn.zju.edu.cn/por/login_auth.csp?apiversion=1";
    private const string LoginPswUrl = "https://webvpn.zju.edu.cn/por/login_psw.csp?anti_replay=1&encrypt=1&apiversion=1";
    public bool IsVpnEnabled=false;
    public HttpClient client;
    public CookieContainer Jar;
    private bool _disposed = false;
    public bool Logined = false;
    public bool AutoDirect = true;
   
    public Cookie TWFID => Jar.GetCookies(new Uri("https://webvpn.zju.edu.cn"))["TWFID"]??new Cookie();

    public VpnService()
    {
        Jar = new CookieContainer();
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = AutoDirect,
            CookieContainer = Jar,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            
        };

        client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("Referer", "https://webvpn.zju.edu.cn/portal/");
        client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
    }

    public async Task<string> LoginAsync(string username, string password)
    {
        var authResponse = await client.GetAsync(LoginAuthUrl);
        authResponse.EnsureSuccessStatusCode();
        var authXml = await authResponse.Content.ReadAsStringAsync();
        var (csrfRandCode, encryptKey, encryptExp) = ParseAuthXml(authXml);
        string encryptedPassword = EncryptPassword($"{password}_{csrfRandCode}", encryptKey, encryptExp);
        var formData = new Dictionary<string, string>
        {
            {"mitm_result", ""},
            {"svpn_req_randcode", csrfRandCode},
            {"svpn_name", username},
            {"svpn_password", encryptedPassword},
            {"svpn_rand_code", ""}
        };

        var content = new FormUrlEncodedContent(formData);
        var loginResponse = await client.PostAsync(LoginPswUrl, content);
        var loginXml = await loginResponse.Content.ReadAsStringAsync();
        if (VerifyLoginResult(loginXml) == "1")
        {
            Logined=true;
        }
        return VerifyLoginResult(loginXml);
       
    }

    private (string csrf, string key, string exp) ParseAuthXml(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var csrf = doc.SelectSingleNode("//CSRF_RAND_CODE")?.InnerText
            ?? throw new Exception("CSRF_RAND_CODE not found");
        var key = doc.SelectSingleNode("//RSA_ENCRYPT_KEY")?.InnerText
            ?? throw new Exception("RSA_ENCRYPT_KEY not found");
        var exp = doc.SelectSingleNode("//RSA_ENCRYPT_EXP")?.InnerText
            ?? throw new Exception("RSA_ENCRYPT_EXP not found");

        return (csrf, key, exp);
    }

    private string EncryptPassword(string plainText, string modulusHex, string exponentDec)
    {
        // 将十六进制字符串转换为字节数组
        byte[] modulus = HexStringToByteArray(modulusHex);
        byte[] exponent = DecimalToByteArray(exponentDec);//注意，webvpn返回十进制指数，而非10001.

        // 创建RSA参数
        var rsaParams = new RSAParameters
        {
            Modulus = modulus,
            Exponent = exponent
        };

        // 使用RSA加密
        using var rsa = new RSACryptoServiceProvider();
        rsa.ImportParameters(rsaParams);

        byte[] data = Encoding.UTF8.GetBytes(plainText);
        byte[] encrypted = rsa.Encrypt(data, false);

        // 返回十六进制小写字符串
        return BitConverter.ToString(encrypted).Replace("-", "").ToLower();
    }

    private string VerifyLoginResult(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var result = doc.SelectSingleNode("//Result")?.InnerText;
        var message = doc.SelectSingleNode("//Message")?.InnerText ?? "Unknown error";

        if (result == "1")
        {
            Logined = true;
            return "1";
        }
        else
        {
            Logined = false;
            return $"400:{message}";
        }
           
        
    }
    public async Task<string> CheckNetwork(bool UseVpn)
    {
        string Mirror_Url =  "https://mirrors.zju.edu.cn/api/is_campus_network";
        string target_uri = UseVpn ? ConvertUrl(Mirror_Url) : Mirror_Url;
        try
        {
            var response = await client.GetAsync(Mirror_Url);
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
            throw new InvalidOperationException("Not logged in");

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
        foreach (var property in original.Properties)
        {
            clone.Properties.Add(property);
        }

        return clone;
    }
    public static string ConvertUrl(string originalUrl)
    {
        var uri = new Uri(originalUrl);
        string hostname = uri.Host.Replace('.', '-');

        // 处理HTTPS
        if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            hostname += "-s";

        // 处理非标准端口
        if (uri.Port > 0 &&
            !(uri.Scheme == "http" && uri.Port == 80) &&
            !(uri.Scheme == "https" && uri.Port == 443))
            hostname += $"-{uri.Port}-p";

        // 构建WebVPN URL
        return $"http://{hostname}.webvpn.zju.edu.cn:8001{uri.PathAndQuery}";
    }
    
    private static byte[] HexStringToByteArray(string hex)
    {
        // 确保十六进制字符串长度为偶数
        if (hex.Length % 2 != 0)
        {
            hex = "0" + hex; // 在开头添加0使长度变为偶数
        }

        int length = hex.Length;
        byte[] bytes = new byte[length / 2];

        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }
    public static byte[] DecimalToByteArray(string decimalNumber)
    {
        // 使用 BigInteger 处理大数
        BigInteger bigInt = BigInteger.Parse(decimalNumber);

        // 转换为字节数组
        byte[] byteArray = bigInt.ToByteArray();
        return byteArray;
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
