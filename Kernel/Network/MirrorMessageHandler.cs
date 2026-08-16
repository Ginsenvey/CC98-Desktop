using CC98.Kernel.Network;
using CC98.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Authorize;
/// <summary>
/// 提供基于 VPN 服务的 HTTP 请求转发工具。
/// </summary>
public partial class MirrorVpnMessageHandler(IVpnService vpnService, ICookieService cookieService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var targetUrl = vpnService.ConvertUrl(request.RequestUri!.ToString());
        request.RequestUri = new Uri(targetUrl);

        var cookieHeader = cookieService.GetCookieHeader($"https://{vpnService.Domain}");
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
