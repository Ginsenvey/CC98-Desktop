using CC98.Services;
using Duende.IdentityModel.Client;
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
    private string _accessToken = PasswordManager.RetrievePassword("Access");
    private string _refreshToken = PasswordManager.RetrievePassword("Refresh");
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
        var tag = Random.Shared.Next();
        Debug.WriteLine($"{tag}正在进行一次刷新。模式是{((appConfig.IsPasswordMode)?"密码模式":"桌面模式")}, 刷新令牌是{RefreshToken}");
        var httpClient = httpClientFactory.CreateClient("IdentityServer");
        var response= await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = ApiEndpoints.OpenId.TokenEndpoint(),
            ClientId = appConfig.IsPasswordMode ? AppConfig.WebClientId : AppConfig.DesktopClientId,
            ClientSecret = appConfig.IsPasswordMode ? AppConfig.WebClientSecret : null,
            RefreshToken = RefreshToken,
            GrantType= "refresh_token"
        }, cancellationToken: cancellationToken);
        Debug.WriteLine("正在检验有效性。");
        try
        {
            if (response.IsError)
            {
                OnAuthenticationFailed($"刷新令牌失败: {response.Error}", response.Exception);
                Debug.WriteLine($"标记为{tag}的刷新失败");
            }
            else
            {
                SetTokens(response);
                Debug.WriteLine($"标记为{tag}的刷新成功");
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
            PasswordManager.SavePassword(_accessToken, "Access");
            PasswordManager.SavePassword(_refreshToken, "Refresh");
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