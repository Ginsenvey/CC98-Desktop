using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace CC98.Kernel.Network;
public interface IVpnService
{
    /// <summary>
    /// 获取或设置 VPN 是否启用。
    /// </summary>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// 获取或设置当前是否已登录。
    /// </summary>
    public bool IsLoggedIn { get; set; }
    public HttpClient HttpClient { get; }
    IEnumerable<string> GetCookies();

    public string ConvertUrl(string originalUrl);

}
