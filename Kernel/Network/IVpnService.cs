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
    public string Domain { get; set; }
    public string ConvertUrl(string originalUrl);


    public Task<VpnLoginResult?> LoginAsync(string userName, string password, CancellationToken cancellation = default);

    public Task<VpnLoginResult?> ConfirmAsync(CancellationToken cancellation = default);
}
