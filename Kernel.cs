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
    //管理登录状态
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
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return "0";
            }
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
                {"grant_type" ,"refresh_token"},
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
        public async Task<string> DeepAuthService(string id, string pass)
        {
            string url = "https://openid.cc98.org/Account/LogOn?returnUrl=%2F";
            var res = await client.GetAsync(url);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                var content = await res.Content.ReadAsStringAsync();
                doc.LoadHtml(content);
                var input = doc.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']");
                if (input != null)
                {
                    string token = input.GetAttributeValue("value", "");
                    var data = new Dictionary<string, string>()
                    {
                        {"__RequestVerificationToken",token},
                        {"UserName",id },
                        {"Password",pass},
                        {"ValidTime",""}
                    };
                    var PostData = new FormUrlEncodedContent(data);
                    var response = await client.PostAsync(url, PostData);
                    if (response.StatusCode == HttpStatusCode.Redirect)
                    {
                        List<string> keys = new();
                        foreach (Cookie c in Jar.GetAllCookies())
                        {
                            keys.Add(c.Name);
                        }
                        if (keys.Contains("idsrv"))
                        {
                            return "1";
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
                    HttpResponseMessage response = await client.GetAsync("https://api.cc98.org/Topic/"+uid+"/post?from="+start+"&size=10");
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        return responseBody;
                    }
                    else
                    {
                        return "404:请求失败";
                    }


                }
                catch (HttpRequestException ex)
                {
                    return "404:"+ex.Message;
                }
            }
            
        }

    }
    //约定：请求总是返回json字符串,或者错误代码。
    //帖子、版面、个人信息核心操作
    public class RequestSender
    {

    }
    //将json字符串解析为目标对象,总是返回对象或者null。
    public class Deserializer
    {
        private void StandardPostConverter(string js)
        {
            if (js.StartsWith("4"))
            {
                return;
            }
            else
            {
                try
                {
                    return;
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }
    }
    //调试类
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

