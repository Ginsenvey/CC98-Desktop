using Duende.IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Authorize;
public interface ITokenService
{

    /// <summary>
    /// 刷新令牌
    /// </summary>
    /// <returns></returns>
    public Task<TokenResponse?> GetNewTokenAsync(CancellationToken cancellationToken = default);
    public bool SetTokens(TokenResponse? tokenResponse);

    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
