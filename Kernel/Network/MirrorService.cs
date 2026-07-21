using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Network;
/// <summary>
/// 与浙江大学镜像站通信，判断当前是否需要使用VPN服务的类
/// </summary>
public class MirrorService(IHttpClientFactory httpClientFactory)
{
    private const string MirrorUrl = "https://mirrors.zju.edu.cn/api/is_campus_network";

    public HttpClient HttpClient = httpClientFactory.CreateClient("MirrorClient");
    /// <summary>
    /// 检查是否内网环境。
    /// </summary>
    /// <param name="useVpn"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<NetworkStatus> CheckNetworkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.GetAsync(MirrorUrl, cancellationToken);
            var resText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                switch (resText)
                {
                    case "0":
                        return NetworkStatus.NotInCampus;
                    case "1" or "2":
                        return NetworkStatus.InCampus;
                }

                //vpn过期时会返回非常长的html
                if (resText.Length > 256)
                {
                    Debug.WriteLine("VPN凭据过期", $"{resText[..32]}");
                    return NetworkStatus.VpnExpired;
                }

                Debug.WriteLine("镜像站返回了意外的内容。请查看正文", $"{resText}");
                return NetworkStatus.UnknownError;
            }

            Debug.WriteLine("访问镜像站失败",
                 $"{response.StatusCode}:{response.ReasonPhrase ?? ""},响应正文{resText}");
            return NetworkStatus.MirrorError;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("错误", $"{ex.Message}");
            return NetworkStatus.NoConnection;
        }
    }
}
