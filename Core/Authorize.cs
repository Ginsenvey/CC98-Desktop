using CC98.Kernel.ApiScope;
using CC98.Kernel.Network;
using Windows.Storage;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System;
using CC98.Services;
using System.Text.Json.Serialization;
using CC98.Objects;

namespace CC98.Kernel;

public static class LoginService
{
    public static VpnService vpn = new VpnService();
    static LoginService() { }

    public static async Task<ApiResponse<AuthorizeResult>> LoginAsync(string username, string password)
    {
        string url = ApiEndpoints.OpenID.GetTokenUrl();
        var data = new Dictionary<string, string>()
            {
                {"username",username},
                {"password",password },
                {"client_id","9a1fd200-8687-44b1-4c20-08d50a96e5cd" },
                {"client_secret","8b53f727-08e2-4509-8857-e34bf92b27f2"},
                {"grant_type" ,"password"},
                {"scope","cc98-api openid offline_access" }
            };
        var PostData = new FormUrlEncodedContent(data);
        var result = await RequestSender.Submit<AuthorizeResult>(url, PostData);
        return result;
    }
    public static async Task<ApiResponse<AuthorizeResult>> OAuth(string verify, string code)
    {
        string url = "https://openid.cc98.org/connect/token";
        var data = new Dictionary<string, string>()
            {
                {"grant_type","authorization_code" },
                {"client_id","d47a2448-779f-42f3-164f-08dd8896bbe5" },   
                {"redirect_uri","cc98://callback"},
                {"code_verifier",verify },
                {"code",code }
            };
        var post_data = new FormUrlEncodedContent(data);

        var result=await RequestSender.Submit<AuthorizeResult>(url, post_data);
        return result;
    }
    //IsPassWordLogin:是否由密码登录
    public static async Task<AuthorizeResult?> GetNewToken(string RefreshToken, bool IsPassWordLogin)
    {
        string tokenUrl = ApiEndpoints.OpenID.GetTokenUrl();
        Dictionary<string, string> data;
        if (IsPassWordLogin)
        {
            data = new Dictionary<string, string>()
                {
                    {"client_id","9a1fd200-8687-44b1-4c20-08d50a96e5cd" },
                    {"client_secret","8b53f727-08e2-4509-8857-e34bf92b27f2"},
                    {"grant_type" ,"refresh_token"},
                    {"refresh_token",RefreshToken }
                };
        }
        else
        {
            data = new Dictionary<string, string>()
                {
                    {"client_id","d47a2448-779f-42f3-164f-08dd8896bbe5" },
                    {"grant_type" ,"refresh_token"},
                    {"refresh_token",RefreshToken },
                };
        }

        //这里省去了scope,服务器应按照授权码的范围发放ACT.
        var PostData = new FormUrlEncodedContent(data);
        try
        {
            var result = await RequestSender.Submit<AuthorizeResult>(tokenUrl, PostData);
            //响应未成功,判断错误类型。这里通常不是因为授权问题，而是请求未成功到达授权服务器，或者其他网络/json解析错误。
            if (!result.IsSuccess)
            {
                return null;
            }
            //响应成功，但是解析出空对象。这是不太可能的，除非代码逻辑有问题。
            var tokenResult = result.Data;
            if (tokenResult == null)
            {
                return null;
            }
            return tokenResult;
        }
        //外层捕捉到错误。这通常是代码问题。
        catch (Exception ex)
        {
            return null;
        }
    }

    public static async Task<string> RefreshToken()
    {
        //刷新函数检查登录方式，在不同的模式下使用不同的刷新方法。
        string IsActive = ValidationHelper.GetValue(ApplicationData.Current.LocalSettings, "IsActive");
        bool mode = IsActive == "2";
        string rft = PasswordManager.RetrievePassword("Refresh");
        if (!string.IsNullOrEmpty(rft))
        {
            var result = await LoginService.GetNewToken(rft, mode);
            if (result == null) return "0:请求失败";
            if (result.IsValid)
            {
                PasswordManager.SavePassword(result.AccessToken, "Access");
                if (!mode)
                {
                    //密码登录使用不变刷新令牌
                    PasswordManager.SavePassword(result.RefreshToken, "Refresh");
                }
                LoginService.vpn.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                return "1";

            }
            //else(result.StatusCode == "2")//返回了错误而不是令牌，一般是失效
            else
            {
                return $"2:{result.Message}";//检测到此问题时，必须弹出登录
            }
        }
        else
        {
            return "0:未保存刷新令牌";
        }


    }
}

public class AuthorizeResult
{
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    // 添加错误信息属性
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    public bool IsValid =>
        !string.IsNullOrEmpty(RefreshToken) &&
        !string.IsNullOrEmpty(AccessToken) &&
        string.IsNullOrEmpty(Error); // 同时要求没有错误

    [JsonIgnore]
    public string Message =>
        IsValid ? "授权成功" :
        $"{Error}: {ErrorDescription}" ?? "未知授权错误";
}