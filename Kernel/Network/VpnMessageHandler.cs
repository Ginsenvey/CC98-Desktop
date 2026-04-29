using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Network;

/// <summary>
///     提供基于 VPN 服务的 HTTP 请求转发工具。
/// </summary>
public class VpnMessageHandler : DelegatingHandler
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

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     将原始 URL 地址转换为基于 VPN 的地址。
    /// </summary>
    /// <param name="uri">要转换的 URL 地址。</param>
    /// <returns>转换后的地址。</returns>
    private static Uri ConvertUrl(Uri uri)
    {
        var schemaAndPort = uri.IsDefaultPort ? uri.Scheme : $"{uri.Scheme}-{uri.Port}";

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
        return new (prefix + newPathAndQuery);
    }

    /// <summary>
    ///     将原始 URL 地址转换为基于 VPN 的地址。
    /// </summary>
    /// <param name="url">要转换的 URL 地址。</param>
    /// <returns>转换后的地址。</returns>
    private static string ConvertUrl(string url)
    {
        var uri = new Uri(url);

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
/// WebVPN 使用的加密字符串的核心方法。
/// </summary>
/// <param name="text">要加密的字符串。</param>
/// <param name="key">加密使用的密钥字符串。</param>
/// <returns>加密后的字符串。</returns>
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
}