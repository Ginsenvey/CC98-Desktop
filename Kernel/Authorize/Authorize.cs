using CC98.Kernel.Network;
using CC98.Services;
using Duende.IdentityModel.Client;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace CC98.Kernel.Authorize;

/// <summary>
/// 集成OpenId Connect的登录服务，提供密码登录、OAuth登录和刷新令牌功能。
/// </summary>
public static class LoginService
{
    public static VpnService Vpn = new();
    public static HttpClient HttpClient => Vpn.HttpClient;

    private const string WebClientId = "9a1fd200-8687-44b1-4c20-08d50a96e5cd";
    private const string ClientId = "d47a2448-779f-42f3-164f-08dd8896bbe5";
    private const string WebClientSecret= "8b53f727-08e2-4509-8857-e34bf92b27f2";

    static LoginService() { }

    /// <summary>
    /// 密码登录
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public static async Task<TokenResponse?> LoginWithPasswordAsync(string userName, string password,CancellationToken cancellationToken=default)
    {
        var disco=await HttpClient.GetDiscoveryDocumentAsync(ApiEndpoints.OpenId.Endpoint, cancellationToken: cancellationToken);
        if(disco.IsError)throw new Exception($"无法获取OpenId配置: {disco.Error}");

        return await HttpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = WebClientId,
            ClientSecret = WebClientSecret,
            UserName = userName,
            Password = password,
            Scope = "cc98-api openid offline_access"
        },cancellationToken);
    }
    public static async Task<TokenResponse?> OAuth(string verify, string code,CancellationToken cancellationToken=default)
    {
        var disco = await HttpClient.GetDiscoveryDocumentAsync(ApiEndpoints.OpenId.Endpoint);
        if (disco.IsError) throw new Exception($"无法获取OpenId配置: {disco.Error}");

        return await HttpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = ClientId,
            Code = code,
            CodeVerifier = verify,
            RedirectUri = "cc98://callback"
        },cancellationToken);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <param name="isPassWordLogin"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<TokenResponse?> GetNewTokenAsync(string refreshToken, bool isPassWordLogin, CancellationToken cancellationToken = default)
    {
        var disco = await HttpClient.GetDiscoveryDocumentAsync(ApiEndpoints.OpenId.Endpoint, cancellationToken: cancellationToken);
        if (disco.IsError) throw new Exception($"无法获取OpenId配置: {disco.Error}");

        return await HttpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = isPassWordLogin ? WebClientId : ClientId,
            ClientSecret = isPassWordLogin ? WebClientSecret : null,
            RefreshToken = refreshToken
        }, cancellationToken: cancellationToken);
    }

    public static async Task<string> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        //刷新函数检查登录方式，在不同的模式下使用不同的刷新方法。
        var isActive = ValidationHelper.GetValue(ApplicationData.Current.LocalSettings, "IsActive");
        var mode = isActive == "2";
        var rft = PasswordManager.RetrievePassword("Refresh");
        if (!string.IsNullOrEmpty(rft))
        {
            var result = await GetNewTokenAsync(rft, mode, cancellationToken);
            if (result == null) return "0:请求失败";
            if (result.IsError)
            {
                PasswordManager.SavePassword(result.AccessToken, "Access");
                if (!mode)
                {
                    //密码登录使用不变刷新令牌
                    PasswordManager.SavePassword(result.RefreshToken, "Refresh");
                }
                Vpn.HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", result.AccessToken);
                return "1";

            }
            //else(result.StatusCode == "2")//返回了错误而不是令牌，一般是失效
            else
            {
                return $"2:{result.Error}";//检测到此问题时，必须弹出登录
            }
        }
        else
        {
            return "0:未保存刷新令牌";
        }


    }
}