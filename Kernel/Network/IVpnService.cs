using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Network;
public interface IVpnService
{
    public string Domain { get; set; }
    //将原始URL转换为VPN代理后的URL
    public string ConvertUrl(string originalUrl);
    //对于不同的VPN实现，只需要修改参数组的项即可，外部接口不需要修改
    public VpnParameterGroup ParameterGroup { get; set; }

    public Task<VpnLoginResult?> LoginAsync(string userName, string password, CancellationToken cancellation = default);
    //顶号机制。如果此VPN实现不支持顶号机制，可以去掉对应的逻辑，或者直接返回Success.
    public Task<VpnLoginResult?> ConfirmAsync(CancellationToken cancellation = default);


}
