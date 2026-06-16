using CC98.Services;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Authorize;
public class TokenService(IHttpClientFactory httpClientFactory,AppConfig appConfig) : ITokenService
{
    #region 属性
    private readonly Lock _lock = new ();
    private string _accessToken = PasswordManager.RetrievePassword("AccessToken");
    private string _refreshToken = PasswordManager.RetrievePassword("RefreshToken");
    private DateTime _expireAt = AppSettings.Current.TokenExpireAt;


    public event EventHandler<AuthenticationFailedEventArgs>? AuthenticationFailed;

    public string AccessToken
    {
        get => _accessToken ?? string.Empty;         // 无锁
    }

    public string RefreshToken
    {
        get => _refreshToken ?? string.Empty;
    }

    private DateTime ExpireAt
    {
        get => _expireAt;
    }

    #endregion

    private void OnAuthenticationFailed(string reason, Exception? ex = null)
    {
        AuthenticationFailed?.Invoke(this, new AuthenticationFailedEventArgs
        {
            Reason = reason,
            Exception = ex
        });
    }


    public async Task<TokenResponse?> GetNewTokenAsync(CancellationToken cancellationToken = default)
    {

        var httpClient = httpClientFactory.CreateClient("IdentityServer");
        var response= await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = ApiEndpoints.OpenId.TokenEndpoint(),
            ClientId = appConfig.IsPasswordMode ? AppConfig.WebClientId : AppConfig.DesktopClientId,
            ClientSecret = appConfig.IsPasswordMode ? AppConfig.WebClientSecret : null,
            RefreshToken = RefreshToken,
            GrantType= "refresh_token"
        }, cancellationToken: cancellationToken);
        try
        {
            if (response.IsError)
            {
                OnAuthenticationFailed($"刷新令牌失败: {response.Error}", response.Exception);
            }
            else
            {
                SetTokens(response);
            }
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        return response;
    }

    public bool SetTokens(TokenResponse? tokenResponse)
    {
        if (tokenResponse == null || tokenResponse.IsError)
        {
            return false;
        }
        lock (_lock)
        {
            _accessToken = tokenResponse.AccessToken!;
            _refreshToken = tokenResponse.RefreshToken!;
            //这里的时间是UTC时间。提供1分钟的保留时间
            _expireAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
            PasswordManager.SavePassword(_accessToken, "AccessToken");
            PasswordManager.SavePassword(_refreshToken, "RefreshToken");
            AppSettings.Current.TokenExpireAt = _expireAt;
        }
        return true;
    }

    
    public bool IsTokenExpired()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(_accessToken)) return true;
            return DateTime.UtcNow >= _expireAt;
        }         
    }
}