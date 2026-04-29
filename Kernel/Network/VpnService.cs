using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using CC98.Objects;
using CC98.Services;

using HtmlAgilityPack;

namespace CC98.Kernel.Network;

/// <summary>
///     WebVPN服务类,Http请求的封装。
/// </summary>
/// <remarks>
///     此层面不处理错误，只负责请求发送和响应接收。
///     尽可能不依赖于CC98.Kernel的其他部分
/// </remarks>
public sealed partial class VpnService : IDisposable
{
    /// <summary>
    ///     加密密码使用的密钥。
    /// </summary>
    private const string PasswordEncryptKey = "wrdvpnisawesome!";

    /// <summary>
    ///     加密域名所用的密钥。
    /// </summary>
    private const string HostEncryptKey = "wrdvpnisthebest!";


    private const string RouteCookieName = "route";
    private const string TicketCookieName = "wengine_vpn_ticketwebvpn_zju_edu_cn";

    public VpnService()
    {
        CookieContainer = new();
        //在此处启用Proxy以开始调试
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = CookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            //Proxy=new WebProxy("127.0.0.1:9000")
        };

        HttpClient = new(handler)
        {
            BaseAddress = new(BaseUrl)
        };

        HttpClient.DefaultRequestHeaders.Add("Referer", BaseUrl);
        HttpClient.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        HttpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
    }

    /// <summary>
    ///     提供 HTTP 服务。
    /// </summary>
    public HttpClient HttpClient { get; }

    /// <summary>
    ///     Cookie 容器。
    /// </summary>
    public CookieContainer CookieContainer { get; }

    public bool IsLoggedIn { get; set; }
    public bool IsVpnEnabled { get; set; }

    public string CaptchaValue { get; set; } = "";
    private string LastRandCode { get; set; } = "";
    public string LastCaptchaId { get; set; } = "";
    private bool IsDisposed { get; set; }

    public Cookie Ticket => CookieContainer.GetCookies(new(BaseUrl))[TicketCookieName] ?? new Cookie();
    public Cookie Route => CookieContainer.GetCookies(new(BaseUrl))[RouteCookieName] ?? new Cookie();

    public async Task<VpnLoginResult> LoginAsync(string username, string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoginCoreAsync(username, password, cancellationToken);
        }

        catch (InvalidOperationException ex)
        {
            return VpnLoginResult.Failure(ex.Message);
        }

        catch (HttpRequestException ex)
        {
            return VpnLoginResult.Failure($"网络问题:{ex.Message}");
        }
        catch (JsonException ex)
        {
            return VpnLoginResult.Failure($"解析登录结果失败:{ex.Message}");
        }
        catch (Exception ex)
        {
            return VpnLoginResult.Failure($"登录出错:{ex.Message}");
        }
    }

    /// <summary>
    ///     更新随机代码和验证码ID的核心方法。
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task UpdateCodeCoreAsync(CancellationToken cancellationToken = default)
    {
        var res = await HttpClient.GetAsync(LoginAuthUrl, cancellationToken);
        if (res.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"网络请求失败:{res.StatusCode}");
        var html = await res.Content.ReadAsStringAsync(cancellationToken);
        var (csrfToken, captcha, _) = GetRandCode(html);
        if (csrfToken == "" || captcha == "") throw new InvalidOperationException("获取登录参数失败");
        LastRandCode = csrfToken;
        LastCaptchaId = captcha;
    }

    /// <summary>
    ///     执行 VPN 登录的核心方法。
    /// </summary>
    /// <param name="username">登录的用户名。</param>
    /// <param name="password">登录的密码。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。任务结果为登录结果。</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<VpnLoginResult> LoginCoreAsync(string username, string password,
        CancellationToken cancellationToken = default)
    {
        if (CaptchaValue == "") await UpdateCodeCoreAsync(cancellationToken);
        var csrf = LastRandCode;
        var captchaId = LastCaptchaId;
        var encryptedPassword = EncryptString(password, PasswordEncryptKey);
        var formData = new Dictionary<string, string>
        {
            { "_csrf", csrf },
            { "auth_type", "local" },
            { "sms_code", "" },
            { "captcha", CaptchaValue },
            { "needCaptcha", "false" },
            { "captcha_id", captchaId },
            { "username", username },
            { "password", encryptedPassword }
        };
        var content = new FormUrlEncodedContent(formData);
        var loginRes = await HttpClient.PostAsync(LoginPswUrl, content, cancellationToken);
        if (loginRes.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"网络请求失败:{loginRes.StatusCode}");
        var result = await loginRes.Content.ReadFromJsonAsync(Cc98JsonContext.Default.VpnLoginResult, cancellationToken);
        if (result == null) throw new InvalidOperationException("登录结果为空");
        if (!result.IsSuccess)
        {
            if (result.NeedConfirm) return VpnLoginResult.ConfirmRequired();
            result.Description = LastCaptchaId;
            result.Status = VpnLoginStatus.NeedCaptcha;
            return result;
        }

        IsLoggedIn = true;
        return VpnLoginResult.Success();
    }

    public async Task<VpnLoginResult> ConfirmAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await HttpClient.PostAsync(ConfirmUrl, null, cancellationToken);
            if (res.StatusCode != HttpStatusCode.OK) return VpnLoginResult.Failure($"网络请求失败:{res.StatusCode}");

            var result = await res.Content.ReadFromJsonAsync(Cc98JsonContext.Default.VpnLoginResult, cancellationToken);

            if (result == null) return VpnLoginResult.Failure("登录结果为空");
            if (result.IsSuccess)
            {
                IsLoggedIn = true;
                return VpnLoginResult.Success();
            }

            return VpnLoginResult.Failure(result.Error ?? "确认登录失败");
        }
        catch (HttpRequestException ex)
        {
            return VpnLoginResult.Failure($"网络问题:{ex.Message}");
        }
        catch (JsonException ex)
        {
            return VpnLoginResult.Failure($"解析登录结果失败:{ex.Message}");
        }
        catch (Exception ex)
        {
            return VpnLoginResult.Failure($"登录出错:{ex.Message}");
        }
    }

    /// <summary>
    ///     注销会话
    /// </summary>
    /// <returns></returns>
    public async Task Logout(CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await HttpClient.GetAsync(LogoutUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            Trace.TraceError("注销时发生错误，Message = {0}", ex.Message);
        }
    }

    /// <summary>
    ///     标准URL转换函数
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    public static string ConvertUrl(string origin)
    {
        var uri = new Uri(origin);

        var isDefaultPort = uri switch
        {
            { Scheme: "http", Port: 80 } => true,
            { Scheme: "https", Port: 443 } => true,
            _ => false
        };

        var schemaAndPort = isDefaultPort ? uri.Scheme : $"{uri.Scheme}-{uri.Port}";

        //处理路径和查询字符
        var suffix = uri.PathAndQuery;
        var qm = suffix.IndexOf('?');
        var path = qm >= 0 ? suffix[..qm] : suffix;
        var query = qm >= 0 ? suffix[qm..] : "";
        var pathSb = new StringBuilder("/");
        foreach (var seg in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            pathSb.Append(Uri.EscapeDataString(seg)).Append('/');
        if (pathSb.Length > 1) pathSb.Length--; // 去掉末尾多余 /
        var newPathAndQuery = pathSb + query;

        var vpnScheme = "https";
        var vpnHost = "webvpn.zju.edu.cn";
        string[] pathSegments =
        [
            schemaAndPort,
            EncryptString(uri.Host, HostEncryptKey)
        ];
        var builder = new UriBuilder(vpnScheme, vpnHost);
        var sb = new StringBuilder();
        foreach (var seg in pathSegments)
            sb.Append('/').Append(Uri.EscapeDataString(seg));
        builder.Path = sb.ToString();
        var fullUri = builder.Uri;
        var prefix = fullUri.ToString();
        return prefix + newPathAndQuery;
    }

    /// <summary>
    ///     检查是否内网环境。
    /// </summary>
    /// <param name="useVpn"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<NetworkStatus> CheckNetworkAsync(bool useVpn, CancellationToken cancellationToken = default)
    {
        var targetUri = useVpn ? ConvertUrl(MirrorUrl) : MirrorUrl;

        try
        {
            var response = await HttpClient.GetAsync(targetUri, cancellationToken);
            var resText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                switch (resText)
                {
                    case "0":
                        return NetworkStatus.NotInCampus;
                    case "1" or "2":
                        return useVpn ? NetworkStatus.ByVpn : NetworkStatus.InCampus;
                }

                //vpn过期时会返回非常长的html
                if (resText.Length > 256)
                {
                    await App.Logger.WriteAsync("网络检查", "VPN凭据过期", $"{resText[..32]}");
                    return NetworkStatus.VpnDisabled;
                }

                await App.Logger.WriteAsync("网络检查", "镜像站返回了意外的内容。请查看正文", $"{resText}");
                return NetworkStatus.UnknownError;
            }

            await App.Logger.WriteAsync("网络检查", "访问镜像站失败",
                $"{response.StatusCode}:{response.ReasonPhrase ?? ""},响应正文{resText}");
            return NetworkStatus.MirrorError;
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("网络检查", "错误", $"{ex.Message}");
            return NetworkStatus.NoConnection;
        }
    }


    private static (string CsrfToken, string Captcha, string AuthType) GetRandCode(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var csrfNode = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='_csrf']");
        var captchaNode = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='captcha_id']");
        var authTypeNode = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='auth_type']");
        var csrf = csrfNode.GetAttributeValue("value", string.Empty);
        var captcha = captchaNode.GetAttributeValue("value", string.Empty);
        var authType = authTypeNode.GetAttributeValue("value", string.Empty);
        return (csrf, captcha, authType);
    }

    /// <summary>
    ///     使用 AES 进行数据加密的核心方法。
    /// </summary>
    /// <param name="plainText">要加密的数据。</param>
    /// <param name="key">加密使用的密钥字节序列。</param>
    /// <param name="iv">加密使用的初始化向量字节序列。</param>
    /// <returns>加密后的数据。</returns>
    private static byte[] EncryptDataWithAes(ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv)
    {
        using var aes = Aes.Create();
        aes.Key = key.ToArray();

        return aes.EncryptCfb(plainText, iv, PaddingMode.Zeros, 128);
    }

    /// <summary>
    ///     拼接密钥。需要指明截取长度，并默认IV,Key和前缀一致。
    /// </summary>
    /// <param name="text"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private static string EncryptString(string text, string key)
    {
        // 转换为字节序列
        var inputData = Encoding.UTF8.GetBytes(text);
        var keyData = Encoding.UTF8.GetBytes(key);

        // 加密并提取结果
        var encryptedData = EncryptDataWithAes(inputData, keyData, keyData);

        // 将加密头和加密结果链接，其中要求 VPN 实现时加密结果被裁剪为最多原始字符串长度的两倍。
        var prefixHex = Convert.ToHexStringLower(keyData);
        var bodyHex = Convert.ToHexStringLower(encryptedData).Cut(text.Length * 2);
        return $"{prefixHex}{bodyHex}";
    }

    #region URL 地址

    public const string BaseUrl = "https://webvpn.zju.edu.cn";
    private const string LoginAuthUrl = "/login";
    private const string LoginPswUrl = "/do-login";
    private const string LogoutUrl = "/logout";
    private const string ConfirmUrl = "/do-confirm-login";
    private const string MirrorUrl = "https://mirrors.zju.edu.cn/api/is_campus_network";

    #endregion

    #region 析构方法相关

    ~VpnService()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     释放对象所占用的资源。可以选择是否释放托管资源。
    /// </summary>
    /// <param name="disposing"></param>
    private void Dispose(bool disposing)
    {
        if (disposing) HttpClient.Dispose();
    }

    #endregion
}