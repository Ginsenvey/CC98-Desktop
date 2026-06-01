using Duende.IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//Todo
//是否需要使用独立的HttpClient来实现登录：实际上只需要获取Bearer令牌的HttpClient就足够了，登录成功后可以将令牌注入到VpnService的HttpClient中。这样可以避免在登录过程中对VpnService的HttpClient进行不必要的配置和状态管理，保持职责单一。
//是否需要使用DI
//如何进行令牌过期的检测和刷新
namespace CC98.Kernel.Authorize;
/// <summary>
/// CC98论坛的登录接口，适配OpenId Connect协议，提供密码登录、OAuth登录和刷新令牌功能。存储和处理令牌不应由此服务负责。
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// 密码登录
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<TokenResponse?> LoginWithPasswordAsync(string userName, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// OAuth登录
    /// </summary>
    /// <param name="verifer"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public  Task<TokenResponse?> LoginWithCodeAsync(string verifer, string code, CancellationToken cancellationToken = default);

}
