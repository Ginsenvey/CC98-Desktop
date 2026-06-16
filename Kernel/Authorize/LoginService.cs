using CC98.Kernel.Network;
using CC98.Services;
using ColorCode.Compilation.Languages;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
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
    /// <summary>
    /// 使用低级HttpClient直接请求令牌端点
    /// </summary>
    /// <param name="verifer"></param>
    /// <param name="code"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TokenResponse?> LoginWithCodeAsync(string verifer, string code, CancellationToken cancellationToken=default)
    {
        var httpClient = httpClientFactory.CreateClient("IdentityServer");
        var response = await httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = ApiEndpoints.OpenId.TokenEndpoint(),
            ClientCredentialStyle=ClientCredentialStyle.PostBody,
            ClientId = AppConfig.DesktopClientId,
            Code = code,
            CodeVerifier = verifer,
            RedirectUri = "cc98://callback",
            GrantType = "authorization_code",
        }, cancellationToken);
        if(!response.IsError) tokenService.SetTokens(response);
        return response;
    }
    /// <summary>
    /// 处理授权码回调，提取令牌并存储。使用封装好的OidcClient库简化流程
    /// </summary>
    /// <param name="callback">回调的完整url</param>
    /// <param name="verifier"></param>
    /// <param name="clientState">由OIDC生成并传递的状态参数，后续被库检验</param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<LoginResult?> LoginWithCodeAsync(string callback,string verifier, string clientState,CancellationToken cancellation=default)
    {
        var oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = ApiEndpoints.OpenId.Endpoint,
            ClientId = AppConfig.DesktopClientId,
            RedirectUri = "cc98://callback",
            Scope = "openid profile cc98-api cc98-card.all offline_access",
        });

        var authorizeState = new AuthorizeState() 
        { 
            CodeVerifier = verifier, 
            State = clientState, 
            RedirectUri = "cc98://callback" 
        };
        
        var result = await oidcClient.ProcessResponseAsync(callback, authorizeState, cancellationToken: cancellation);
        if(!result.IsError) tokenService.SetTokens(result.TokenResponse);
        return result;
    }
    

}