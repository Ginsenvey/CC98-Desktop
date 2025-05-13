using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using System.Threading.Tasks;
namespace CCkernel
{
    public class CCloginservice
    {
        public HttpClient client;
        private readonly CookieContainer Jar = new CookieContainer();
        public HttpClientHandler handler;
        public CCloginservice()
        {
            handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = Jar,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            client = new HttpClient(handler);

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.58");
            client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
            
        }
        public async Task<string> LoginAsync(string username, string password)
        {
            string LoginUrl = "https://openid.cc98.org/connect/token";
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
            var response = await client.PostAsync(LoginUrl, PostData);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> RefreshToken(string rft)
        {
            if(!string.IsNullOrEmpty(rft))
            {
                string LoginUrl = "https://openid.cc98.org/connect/token";
                var data = new Dictionary<string, string>()
            {
                {"client_id","9a1fd200-8687-44b1-4c20-08d50a96e5cd" },
                {"client_secret","8b53f727-08e2-4509-8857-e34bf92b27f2"},
                {"grant_type" ,"password"},
                {"refresh_token",rft },
                {"scope","cc98-api openid offline_access" }
            };
                var PostData = new FormUrlEncodedContent(data);
                var response = await client.PostAsync(LoginUrl, PostData);
                if(response.StatusCode == HttpStatusCode.OK)
                {
                    string NewAccessText= await response.Content.ReadAsStringAsync();
                    var js=JsonConvert.DeserializeObject<Dictionary<string,object>>(NewAccessText);
                    if(js.TryGetValue("access_token",out var token))
                    {
                        if (token.ToString() != null)
                        {
                            return token.ToString();
                        }
                        else
                        {
                            return "0";
                        }
                    }
                    else
                    {
                        return "0";
                    }

                    
                }
                else
                {
                    return "0";
                }
            }
            else
            {
                return "0";
            }
        }
        public async Task<string> GetTopic(string uid, string access,string start)
        {
            using (HttpClient client = new HttpClient() { })
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
                    // 发送 GET 请求
                    HttpResponseMessage response = await client.GetAsync("https://api.cc98.org/Topic/"+uid+"/post?from="+start+"&size=10");

                    // 确保响应成功
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
                catch (HttpRequestException ex)
                {
                    return "404:"+ex.Message;
                }
            }
            
        }

    }
    class Program
    {
        static async Task Main(string[] args)
        {

            var loginService = new CCloginservice();
            string result = await loginService.LoginAsync("安希礼", "byqzkyy.");
            if (result.Contains("access_token"))
            {
                var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
                string access = js["access_token"] as string;
                string token_type = js["token_type"] as string;
                int expire = Convert.ToInt16(js["expires_in"]);
                string refresh = js["refresh_token"] as string;
                await loginService.GetTopic("1", access,"0");
            }
            Console.WriteLine(result);
        }
    }
}

