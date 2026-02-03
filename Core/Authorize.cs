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

namespace CC98.Kernel;

public static class LoginService
{
    public static VpnService vpn = new VpnService();
    static LoginService() { }

    public static async Task<string> LoginAsync(string username, string password)
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
        var response = await vpn.PostAsync(url, PostData);
        return await ValidationHelper.AutoResponse(response);
    }
    public static async Task<string> OAuth(string verify, string code)
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
        var res = await vpn.PostAsync(url, post_data);
        return await ValidationHelper.AutoResponse(res);
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
            var response = await vpn.PostAsync(tokenUrl, PostData);
            string NewAccessText = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK)
            {

                var js = Deserializer.ToDictionary(NewAccessText);
                if (js != null)
                {
                    string access = ValidationHelper.GetKey(js, "access_token");
                    string refresh = ValidationHelper.GetKey(js, "refresh_token");//密码登陆时返回“0”
                    return new AuthorizeResult { StatusCode = "1", AccessToken = access, RefreshToken = refresh, Message = "刷新令牌成功" };
                }
                else
                {
                    return new AuthorizeResult { StatusCode = "0", AccessToken = "", RefreshToken = "", Message = NewAccessText };//返回值不是字典;
                }
            }
            else//令牌过期或者次数超限时状态码不是OK
            {
                return new AuthorizeResult { StatusCode = "2", AccessToken = "", RefreshToken = "", Message = NewAccessText };//令牌作废
            }

        }
        catch (Exception ex)
        {
            return new AuthorizeResult { StatusCode = "3", AccessToken = "", RefreshToken = "", Message = ex.Message };//无网络等
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
    //此处判断令牌合法的逻辑应该加强
    public bool IsValid => RefreshToken!=null&&AccessToken!=null;
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    [JsonIgnore]
    public string Message { get; set; }=string.Empty;
}