using CC98.Objects;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CC98.Services;

namespace CC98.Kernel.Network;
/// <summary>
/// WebVPN服务类,Http请求的封装。
/// </summary>
/// <remarks>
/// 此层面不处理错误，只负责请求发送和响应接收。
/// 尽可能不依赖于CC98.Kernel的其他部分
/// </remarks>
public partial class VpnService : IDisposable
{
    public const string Base = "https://webvpn.zju.edu.cn";
    private const string LoginAuthUrl = $"{Base}/login";
    private const string LoginPswUrl = $"{Base}/do-login";
    private const string LogoutUrl = $"{Base}/logout";
    private const string ConfirmUrl = $"{Base}/do-confirm-login";
    private const string MirrorUrl = "https://mirrors.zju.edu.cn/api/is_campus_network";
    //用于加密凭据的IV和Key
    private const string Key0 = "wrdvpnisawesome!";
    //用于转写链接的IV和Key
    private const string Key1 = "wrdvpnisthebest!";
    private static string CaptchaUrl(string imageUrl) => $"{Base}/captcha/{imageUrl}";
    private const string RouteCookieName = "route";
    private const string TicketCookieName = "wengine_vpn_ticketwebvpn_zju_edu_cn";
    public HttpClient Client;
    public CookieContainer Jar;
    public bool Logined = false;//可以强行修改这个值来避开检验。由于从缓存中读取凭据不经过Login函数，需要在读取时手动修改这个值。
    public bool IsVpnEnabled = false;
    public string CaptchaValue = "";
    public string LastRandCode = "";
    public string LastCaptchaId = "";
    private bool _disposed = false;
    public Cookie Ticket => Jar.GetCookies(new(Base))[TicketCookieName] ?? new Cookie();
    public Cookie Route => Jar.GetCookies(new(Base))[RouteCookieName] ?? new Cookie();
    public VpnService()
    {
        Jar = new();
        //在此处启用Proxy以开始调试
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = Jar,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            //Proxy=new WebProxy("127.0.0.1:9000")
        };

        Client = new(handler);
        Client.DefaultRequestHeaders.Add("Referer", Base);
        Client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
    }
    public async Task<VpnLoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
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

    private async Task<VpnLoginResult> LoginCoreAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (CaptchaValue == "")
        {
            var res = await Client.GetAsync(LoginAuthUrl, cancellationToken);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"网络请求失败:{res.StatusCode}");
            }
            var html = await res.Content.ReadAsStringAsync(cancellationToken);
            var param = GetRandCode(html);
            if (param.csrf == "" || param.captcha == "")
            {
                throw new InvalidOperationException("获取登录参数失败");
            }
            LastRandCode = param.csrf;
            LastCaptchaId = param.captcha;
        }
        var csrf = LastRandCode;
        var captchaId = LastCaptchaId;
        var encrptedPassword = BuildPassword(Key0, password);
        var formData = new Dictionary<string, string>
            {
                {"_csrf", csrf},
                {"auth_type", "local"},
                {"sms_code", ""},
                {"captcha",CaptchaValue },
                {"needCaptcha", "false"},
                {"captcha_id", captchaId},
                {"username",username},
                {"password",encrptedPassword }
            };
        var content = new FormUrlEncodedContent(formData);
        var loginRes = await Client.PostAsync(LoginPswUrl, content, cancellationToken);
        if (loginRes.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"网络请求失败:{loginRes.StatusCode}");
        }
        var text = await loginRes.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerialize.Deserialize<VpnLoginResult>(text);
        if (result == null)
        {
            throw new InvalidOperationException("登录结果为空");
        }
        if (!result.IsSuccess)
        {
            if (result.NeedConfirm)
            {
                return VpnLoginResult.ConfirmRequired();
            }
            result.Description = LastCaptchaId;
            result.Status = VpnLoginStatus.NeedCaptcha;
            return result;
        }
        Logined = true;
        return VpnLoginResult.Success();
    }

    public async Task<VpnLoginResult> ConfirmAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await Client.PostAsync(ConfirmUrl, null);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                return VpnLoginResult.Failure($"网络请求失败:{res.StatusCode}");
            }

            var result = await res.Content.ReadFromJsonAsync(Cc98JsonContext.Default.VpnLoginResult, cancellationToken);

            if (result == null)
            {
                return VpnLoginResult.Failure("登录结果为空");
            }
            if (result.IsSuccess)
            {
                Logined = true;
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
    /// 注销会话
    /// </summary>
    /// <returns></returns>
    public async Task Logout(CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await Client.GetAsync(LogoutUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            Trace.TraceError("注销时发生错误，Message = {0}", ex.Message);
        }
    }
    /// <summary>
    /// 标准URL转换函数
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    public static string ConvertUrl(string origin)
    {
        var uri = new Uri(origin);

        var isDefaultProt = uri.Port > 0 &&
                            !(uri.Scheme == "http" && uri.Port == 80) &&
                            !(uri.Scheme == "https" && uri.Port == 443);

        var schemaAndPort = isDefaultProt ? $"{uri.Scheme}-{uri.Port}" : uri.Scheme;

        //处理路径和查询字符
        var suffix = uri.PathAndQuery;
        var qm = suffix.IndexOf('?');
        var path = qm >= 0 ? suffix[..qm] : suffix;
        var query = qm >= 0 ? suffix[qm..] : "";
        var pathSb = new System.Text.StringBuilder("/");
        foreach (var seg in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            pathSb.Append(Uri.EscapeDataString(seg)).Append('/');
        if (pathSb.Length > 1) pathSb.Length--;   // 去掉末尾多余 /
        var newPathAndQuery = pathSb.ToString() + query;

        var vpnScheme = "https";
        var vpnHost = "webvpn.zju.edu.cn";
        string[] pathSegments =
        [
            schemaAndPort,
            BuildPassword(Key1,uri.Host),
        ];
        var builder = new UriBuilder(vpnScheme, vpnHost);
        var sb = new System.Text.StringBuilder();
        foreach (var seg in pathSegments)
            sb.Append('/').Append(Uri.EscapeDataString(seg));
        builder.Path = sb.ToString();
        var fullUri = builder.Uri;
        var prifix = fullUri.ToString();
        return prifix + newPathAndQuery;
    }

    /// <summary>
    /// 检查是否内网环境。
    /// </summary>
    /// <param name="useVpn"></param>
    /// <returns></returns>
    public async Task<NetworkStatus> CheckNetwork(bool useVpn)
    {

        var targetUri = useVpn ? ConvertUrl(MirrorUrl) : MirrorUrl;

        try
        {
            var response = await Client.GetAsync(targetUri);
            var resText = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                if (resText == "0")
                {
                    return NetworkStatus.NotInCampus;
                }
                else if (resText == "1" || resText == "2")
                {
                    return useVpn ? NetworkStatus.ByVpn : NetworkStatus.InCampus;
                }
                else
                {
                    //vpn过期时会返回非常长的html
                    if (resText.Length > 256)
                    {
                        await App.Logger.WriteAsync("网络检查", "VPN凭据过期", $"{resText[..32]}");
                        return NetworkStatus.VpnDisabled;
                    }
                    await App.Logger.WriteAsync("网络检查", "镜像站返回了意外的内容。请查看正文", $"{resText}");
                    return NetworkStatus.UnknownError;
                }
            }
            else
            {
                await App.Logger.WriteAsync("网络检查", "访问镜像站失败", $"{response.StatusCode}:{response.ReasonPhrase ?? ""},响应正文{resText}");
                return NetworkStatus.MirrorError;
            }
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("网络检查", "错误", $"{ex.Message}");
            return NetworkStatus.NoConnection;
        }


    }


    public static (string csrf, string captcha, string auth_type) GetRandCode(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var csrfNode = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='_csrf']");
        var captchaNode = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='captcha_id']");
        var authTypeNode = doc.DocumentNode.SelectSingleNode("//input[@type='hidden' and @name='auth_type']");
        var csrf = csrfNode?.GetAttributeValue("value", string.Empty) ?? string.Empty;
        var captcha = captchaNode?.GetAttributeValue("value", string.Empty) ?? string.Empty;
        var authType = authTypeNode?.GetAttributeValue("value", string.Empty) ?? string.Empty;
        return (csrf, captcha, authType);
    }
    /// <summary>
    /// 拼接密钥。需要指明截取长度，并默认IV,Key和前缀一致。
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public static string BuildPassword(string prefix, string plainText)
    {
        //裁剪长度为2倍明文长度
        var sliceLength = 2 * plainText.Length;
        var prifixHex = Crypto.StringToAscll(prefix);
        var fullCore = Crypto.EncryptStringToHex(plainText, prefix, prefix);
        var core = fullCore[..Math.Min(fullCore.Length, sliceLength)];
        return $"{prifixHex}{core}";
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
                Client?.Dispose();
            }
            _disposed = true;
        }
    }

}

