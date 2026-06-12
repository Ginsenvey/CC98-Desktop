using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Network;
//Todo:将VPN的HTTP客户端与登录服务分离
public interface IVpnService
{
    /// <summary>
    /// 获取或设置 VPN 是否启用。
    /// </summary>
    public bool IsEnabled { get; set; }
    public string Domain { get; set; }

    /// <summary>
    /// 获取或设置当前是否已登录。
    /// </summary>
    public bool IsLoggedIn { get; set; }

    public string ConvertUrl(string originalUrl);

    public Task<NetworkStatus> CheckNetworkAsync(bool useVpn,CancellationToken cancellation=default);

    public Task<VpnLoginResult> LoginAsync(string userName, string password,CancellationToken cancellation=default);

    public Task<VpnLoginResult> ConfirmAsync(CancellationToken cancellation=default);
}
