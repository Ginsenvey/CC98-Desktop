using CC98.Kernel.Network;
using CC98.Services;
using ColorCode.Compilation.Languages;
using Duende.IdentityModel.Client;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace CC98.Kernel.Authorize;
//Todo:提供从密码管理器提取令牌的功能
/// <summary>
/// 集成OpenId Connect的登录服务，提供密码登录、OAuth登录和刷新令牌功能。
/// </summary>
public class LoginService(IHttpClientFactory httpClientFactory,ITokenService tokenService) : ILoginService
{
    /// <summary>
    /// 参数不可为空，前端需检查
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TokenResponse?> LoginWithPasswordAsync(string userName, string password, CancellationToken cancellationToken=default)
    {
        var httpClient = httpClientFactory.CreateClient("IdentityServer");
        var response = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
        {
            Address = ApiEndpoints.OpenId.TokenEndpoint(),
            ClientId =AppConfig.WebClientId,
            ClientSecret = AppConfig.WebClientSecret,
            UserName = userName,
            Password = password,
            Scope = "cc98-api openid offline_access"
        }, cancellationToken);
        
        if(!response.IsError) tokenService.SetTokens(response);
        Debug.WriteLine("登录成功");
        Debug.WriteLine(response.RefreshToken);
        return response;
    }

    public async Task<TokenResponse?> LoginWithCodeAsync(string verifer, string code, CancellationToken cancellationToken)
    {
        var httpsClient = httpClientFactory.CreateClient("IdentityServer");
        var response = await httpsClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = ApiEndpoints.OpenId.TokenEndpoint(),
            ClientId = AppConfig.DesktopClientId,
            Code = code,
            CodeVerifier = verifer,
            RedirectUri = "cc98://callback"
        }, cancellationToken);
        if(!response.IsError) tokenService.SetTokens(response);
        return response;
    }

    

}