using Duende.IdentityModel.Client;
using HtmlAgilityPack;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
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
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

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
    private const string Mirror_Url = "https://mirrors.zju.edu.cn/api/is_campus_network";
    //用于加密凭据的IV和Key
    private const string Key0 = "wrdvpnisawesome!";
    //用于转写链接的IV和Key
    private const string Key1 = "wrdvpnisthebest!";
    private string CaptchaUrl(string imageUrl) => $"{Base}/captcha/{imageUrl}";
    private const string RouteCookieName = "route";
    private const string TicketCookieName = "wengine_vpn_ticketwebvpn_zju_edu_cn";
    public HttpClient client;
    public CookieContainer Jar;
    public bool Logined = false;//可以强行修改这个值来避开检验。由于从缓存中读取凭据不经过Login函数，需要在读取时手动修改这个值。
    public bool IsVpnEnabled = false;
    public string CaptchaValue = "";
    public string LastRandCode = "";
    public string LastCaptchaId = "";
    private bool _disposed = false;
    public Cookie Ticket => Jar.GetCookies(new Uri(Base))[TicketCookieName] ?? new Cookie();
    public Cookie Route => Jar.GetCookies(new Uri(Base))[RouteCookieName] ?? new Cookie();
    public VpnService()
    {
        Jar = new CookieContainer();
        //在此处启用Proxy以开始调试
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = Jar,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            //Proxy=new WebProxy("127.0.0.1:9000")
        };
        
        client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("Referer", Base);
        client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
    }
    public async Task<VpnLoginResult> LoginAsync(string username, string password,CancellationToken cts = default)
    {
        try
        {
            
            if (CaptchaValue == "")
            {
                var res = await client.GetAsync(LoginAuthUrl);
                if (res.StatusCode != HttpStatusCode.OK)
                {
                    return VpnLoginResult.Failure($"网络请求失败:{res.StatusCode}");
                }
                var html = await res.Content.ReadAsStringAsync();
                var param = GetRandCode(html);
                if (param.csrf == "" || param.captcha == "")
                {
                    return VpnLoginResult.Failure("获取登录参数失败");
                }
                LastRandCode = param.csrf;
                LastCaptchaId = param.captcha;
            }
            string csrf = LastRandCode;
            string captchaId = LastCaptchaId;
            string encrpted_password = BuildPassword(Key0, password);
            var formData = new Dictionary<string, string>
            {
                {"_csrf", csrf},
                {"auth_type", "local"},
                {"sms_code", ""},
                {"captcha",CaptchaValue },
                {"needCaptcha", "false"},
                {"captcha_id", captchaId},
                {"username",username},
                {"password",encrpted_password }
            };
            var content = new FormUrlEncodedContent(formData);
            var login_res = await client.PostAsync(LoginPswUrl, content);
            if (login_res.StatusCode != HttpStatusCode.OK)
            {
                return VpnLoginResult.Failure($"网络请求失败:{login_res.StatusCode}");
            }
            string text = await login_res.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<VpnLoginResult>(text);
            if (result == null)
            {
                return VpnLoginResult.Failure("登录结果为空");
            }
            if (!result.IsSuccess)
            {
                if (result.NeedConfirm)
                {
                    return VpnLoginResult.ConfirmRequired();
                }
                result.Description = LastCaptchaId;
                result.Status = VPNLoginStatus.NeedCaptcha;
                return result;
            }
            return VpnLoginResult.Success();
        }
        catch (HttpRequestException ex)
        {
            return VpnLoginResult.Failure($"网络问题:{ex.Message}");
        }
        catch(JsonException ex)
        {
            return VpnLoginResult.Failure($"解析登录结果失败:{ex.Message}");
        }
        catch (Exception ex)
        {
            return VpnLoginResult.Failure($"登录出错:{ex.Message}");
        }
    }
    public async Task<VpnLoginResult> Confirm()
    {
        try
        {
            var res = await client.PostAsync(ConfirmUrl, null);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                return VpnLoginResult.Failure($"网络请求失败:{res.StatusCode}");
            }
            var content = await res.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<VpnLoginResult>(content);
            if (result == null)
            {
                return VpnLoginResult.Failure("登录结果为空");
            }
            if (result.IsSuccess)
            {
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
            BuildPassword(Key1,host),
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
    public async Task<NetworkStatus> CheckNetwork(bool UseVpn)
    {
        
        string target_uri = UseVpn ? ConvertUrl(Mirror_Url) : Mirror_Url;
        
        try
        {
            var response = await client.GetAsync(target_uri);
            string res_text = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                if (res_text == "0")
                {
                    return NetworkStatus.NotInCampus;
                }
                else if (res_text == "1" || res_text == "2")
                {
                    return UseVpn?NetworkStatus.ByVPN:NetworkStatus.InCampus;
                }
                else
                {
                    //vpn过期时会返回非常长的html
                    if (res_text.Length > 256)
                    {
                        await App.Logger.WriteAsync("网络检查", "VPN凭据过期", $"{res_text.Substring(0,32)}");
                        return NetworkStatus.VpnDisabled;
                    }
                    await App.Logger.WriteAsync("网络检查", "镜像站返回了意外的内容。请查看正文", $"{res_text}");
                    return NetworkStatus.UnknownError;
                }
            }
            else
            {
                await App.Logger.WriteAsync("网络检查", "访问镜像站失败", $"{response.StatusCode}:{response.ReasonPhrase??""},响应正文{res_text}");
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
        string prifix_hex = Crypto.StringToAscll(Prefix);
        string full_core = Crypto.EncryptStringToHex(PlainText, Prefix, Prefix);
        string core = full_core[..Math.Min(full_core.Length, SliceLength)];
        return $"{prifix_hex}{core}";
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

