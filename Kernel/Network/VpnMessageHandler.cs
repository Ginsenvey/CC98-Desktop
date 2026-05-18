using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Network;

/// <summary>
/// 提供基于 VPN 服务的 HTTP 请求转发工具。
/// </summary>
public partial class VpnMessageHandler(IVpnService vpnService) : DelegatingHandler
{
   
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        if (vpnService.IsEnabled)
        {
            var targetUrl = vpnService.ConvertUrl(request.RequestUri!.ToString());
            request.RequestUri = new Uri(targetUrl);
            //问题：cookieContainer会自动管理新加入的cookie;登录时，handler也会添加cookie，这样是重复的。
            //是否由委托处理器来管理cookie？
            foreach (var cookie in vpnService.GetCookies())
            {
                request.Headers.Add("Cookie", cookie);
            }
        }
        return base.SendAsync(request, cancellationToken);
    }

}