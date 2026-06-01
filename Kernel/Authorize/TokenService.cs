using CC98.Services;
using Duende.IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Authorize;
public class TokenService(IHttpClientFactory httpClientFactory,AppConfig appConfig) : ITokenService
{
    #region 属性

    public string AccessToken
    {
        get
        {
            return PasswordManager.RetrievePassword("Access") ?? string.Empty;
        }
        set
        {
            PasswordManager.SavePassword(value, "Access");
        }
    }

    public string RefreshToken
    {
        get
        {
            return PasswordManager.RetrievePassword("Refresh") ?? string.Empty;
        }
        set
        {
            PasswordManager.SavePassword(value, "Refresh");
        }
    }



    #endregion

    public async Task<TokenResponse?> GetNewTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient("IdentityServer");
        var response= await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = ApiEndpoints.OpenId.TokenEndpoint(),
            ClientId = appConfig.IsPasswordMode ? AppConfig.WebClientId : AppConfig.DesktopClientId,
            ClientSecret = appConfig.IsPasswordMode ? AppConfig.WebClientSecret : null,
            RefreshToken = RefreshToken
        }, cancellationToken: cancellationToken);
        if(!response.IsError)SetTokens(response);
        return response;
    }

    public bool SetTokens(TokenResponse? tokenResponse)
    {
        if (tokenResponse == null || tokenResponse.IsError)
        {
            return false;
        }
        AccessToken = tokenResponse.AccessToken!;
        RefreshToken = tokenResponse.RefreshToken!;
        return true;
    }
}