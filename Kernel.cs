using App3;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;
using FluentIcons.Common;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.Security.AccessControl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.AppBroadcasting;
using Windows.Media.Core;
using Windows.Media.Protection.PlayReady;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.WebUI;
using static App3.Profile;
using static App3.Topic;
namespace CCkernel
{
    //管理登录状态
    
    public static class CCloginservice
    {
        
        
        public static VpnService vpn=new VpnService();
        static CCloginservice(){ }
       
        public static async Task<string> LoginAsync(string username, string password)
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
            var response = await vpn.PostAsync(LoginUrl, PostData);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return "0";
            }
        }
        public static async Task<string> OAuth(string verify,string code)
        {
            string url = "https://openid.cc98.org/connect/token";
            var data = new Dictionary<string, string>()
            {
                {"grant_type","authorization_code" },
                {"client_id","d47a2448-779f-42f3-164f-08dd8896bbe5" },
                {"redirect_uri","cc98://callback" },
                {"code_verifier",verify },
                {"code",code }
            };
            var post_data=new FormUrlEncodedContent(data);
            var res= await vpn.PostAsync(url, post_data);
            return await ValidationHelper.AutoResponse(res);
        }
        public static async Task<AuthentificateResult> GetNewToken(string RefreshToken)
        {
            string LoginUrl = "https://openid.cc98.org/connect/token";
            var data = new Dictionary<string, string>()
                {
                    {"client_id","d47a2448-779f-42f3-164f-08dd8896bbe5" },
                    {"grant_type" ,"refresh_token"},
                    {"refresh_token",RefreshToken },
                };
            //这里省去了scope,服务器应按照授权码的范围发放ACT.
            var PostData = new FormUrlEncodedContent(data);
            try
            {
                var response = await vpn.PostAsync(LoginUrl, PostData);
                string NewAccessText = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var js = Deserializer.ToDictionary(NewAccessText);
                    if (js != null)
                    {
                        string access = ValidationHelper.GetKey(js, "access_token");
                        string refresh = ValidationHelper.GetKey(js, "refresh_token");
                        return new AuthentificateResult { StatusCode = "1", Access = access, Refresh = refresh, Message = "刷新令牌成功" };
                    }
                    else
                    {
                        return new AuthentificateResult { StatusCode = "0", Access = "", Refresh = "", Message = NewAccessText };//返回值不是字典;
                    }
                }
                else
                {
                    return new AuthentificateResult { StatusCode = "2", Access = "", Refresh = "", Message = NewAccessText };//令牌作废
                }

            }
            catch (Exception ex)
            {
                return new AuthentificateResult { StatusCode = "3", Access = "", Refresh = "", Message = ex.Message };//无网络等
            }
        }

        public static async Task<string> RefreshToken()
        {

            string rft = PasswordManager.RetrievePassword("Refresh");
            if (!string.IsNullOrEmpty(rft))
            {
                var token = await CCloginservice.GetNewToken(rft);
                if (token.StatusCode == "1")
                {
                    PasswordManager.SavePassword(token.Access, "Access");
                    PasswordManager.SavePassword(token.Refresh, "Refresh");
                    CCloginservice.vpn.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Access);
                    return "1";
                    
                }
                else if (token.StatusCode == "2")//返回了错误而不是令牌，一般是失效
                {
                    return $"2:{token.Message}";//检测到此问题时，必须弹出登录
                }
                else
                {
                    return $"0:{token.Message}";
                }
            }
            else
            {
                return "0:未保存刷新令牌";
            }
            
            
        }
        public static async Task<string> DeepAuthService(string id, string pass)
        {
            string url = "https://openid.cc98.org/Account/LogOn?returnUrl=%2F";
            var res = await vpn.GetAsync(url);
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
                    var response = await vpn.PostAsync(url, PostData);
                    if (response.StatusCode == HttpStatusCode.Redirect)
                    {
                        List<string> keys = new();
                        foreach (Cookie c in CCloginservice.vpn.Jar.GetAllCookies())
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
        

    }
    public class AuthentificateResult
    {
        public required string StatusCode {  get; set; }
        public required string Refresh { get; set; }
        public required string Access { get; set; }
        public required string Message { get; set; }
    }
    //约定：请求总是返回json字符串,或者错误代码。
    //发送到解析器的文本总是不为空。
    //帖子、版面、个人信息核心操作
    public static class RequestSender
    {
        //通用的简单请求方法
        public static async Task<string> SimpleRequest(string api)
        {   
            var res = await CCloginservice.vpn.GetAsync(api);
            return await ValidationHelper.AutoResponse(res);
        }
        //获取新帖
        
        public static async Task<string> TopicReply(string uid,  string start)
        {
            try
            {
                
                var Response = await CCloginservice.vpn.GetAsync("https://api.cc98.org/Topic/" + uid + "/post?from=" + start + "&size=10");
                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    string ResponseBody = await Response.Content.ReadAsStringAsync();
                    if(!string.IsNullOrEmpty(ResponseBody))
                    {
                        return ResponseBody;
                    }
                    else
                    {
                        return "404:空返回";
                    }
                }
                else
                {
                    return "404:请求失败";
                }

            }
            catch (Exception ex)
            {
                return "404:" + ex.Message;
            }

        }
        //以"id=username"的格式作为列表的元素user
        public static async Task<string> SimpleUserInfo(List<string> users)
        {
            string param = string.Join("&", users);
            string url = "https://api.cc98.org/user/basic?" + param;
            
            var PortRes = await CCloginservice.vpn.GetAsync(url);
            if (PortRes.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await PortRes.Content.ReadAsStringAsync();
                if (ValidationHelper.IsValidResponse(content))
                {
                    return content;
                }
                else
                {
                    return "404:空返回";
                }
            }
            else
            {
                return "404:请求失败";
            }
        }

        public static async Task<string> FavoritesList()
        {
            string url = "https://api.cc98.org/me/favorite-topic-group";
            try
            {
                var LikeRes = await CCloginservice.vpn.GetAsync(url);
                return await ValidationHelper.AutoResponse(LikeRes);
            }
            catch(Exception ex)
            {
                return "404:请求失败";
            }
        }
        public static async Task<bool> AddFavorites(string Pid,string GroupId)//话题Id,收藏夹的组Id
        {
            if (!string.IsNullOrEmpty(GroupId)&&(!string.IsNullOrEmpty(Pid)))
            {
                try
                {
                    string favoriteurl = "https://api.cc98.org/me/favorite/" + Pid + "?groupid=" + GroupId;
                    var request = new HttpRequestMessage(HttpMethod.Put, favoriteurl);
                    var content = new StringContent("", Encoding.UTF8, "application/json");
                    request.Content = content;//按照此格式发送空的put请求并设置请求头
                    var res = await CCloginservice.vpn.SendAsync(favoriteurl,request);

                    if (res.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        public static async Task<string> SignIn()
        {
            string SignInUrl = "https://api.cc98.org/me/signin";
            var request = new HttpRequestMessage(HttpMethod.Post, SignInUrl);
            var content = new StringContent("", Encoding.UTF8, "application/json");
            request.Content = content;//按照此格式发送空的post请求并设置请求头
            var res = await CCloginservice.vpn.SendAsync(SignInUrl,request);
            if (res.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                string restext = await res.Content.ReadAsStringAsync();

                if (restext == "has_signed_in_today")
                {
                    return "2";
                }
                else
                {
                    return "1";
                }
            }
            else if(res.StatusCode==HttpStatusCode.OK)
            {
                return "1";
            }
            else
            {
                return "0";
            }

        }
        public static async Task<string> SystemNotice(string type,string start)
        {
            string url = $"https://api.cc98.org/notification/{type}?from={start}&size=10";
            var res = await CCloginservice.vpn.GetAsync(url);
            return await ValidationHelper.AutoResponse(res);
        }
        public static async Task<bool> Like(string mode,string postid)
        {
            string url = "https://api.cc98.org/post/"+postid+"/like";
            var content = new StringContent(mode, Encoding.UTF8, "application/json");
            var response = await CCloginservice.vpn.PutAsync(url, content);
            try
            {
                if(response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public static async Task<Dictionary<string,string>> LikeState(string postid)
        {
            string url = "https://api.cc98.org/post/" + postid + "/like";
            var res = await CCloginservice.vpn.GetAsync(url);
            try
            {
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    string restext = await res.Content.ReadAsStringAsync();
                    if (ValidationHelper.IsValidResponse(restext))
                    {
                        var state = JsonConvert.DeserializeObject<Dictionary<string, object>>(restext);
                        return new Dictionary<string, string>()
                        {
                            {"like",state["likeCount"].ToString() },
                            {"dislike",state["dislikeCount"].ToString() },
                            {"likestate",state["likeState"].ToString() }
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public static async Task<string> SendPost(string board_id, string content,string title, int content_type,bool notify_poster,int post_type,bool is_anonymous)
        {
            string url = "https://api.cc98.org/board/"+board_id+"/topic";
            var post = new Dictionary<string, object>()
            {
                {"clientType",1 },
                {"content",content },
                {"contentType",content_type },
                {"isAnonymous",is_anonymous },
                {"notifyPoster",notify_poster },
                {"title",title },
                {"type",post_type }
            };
            string post_text = JsonConvert.SerializeObject(post);
            var request_body = new StringContent(post_text, Encoding.UTF8, "application/json");
            var r = await CCloginservice.vpn.PostAsync(url, request_body);
            return await ValidationHelper.AutoResponse(r);
        }
        public static async Task<string> SendReplyToTopic(string replyid, string content, bool is_anonymous, bool notify_replier, int content_type,bool canbe_traced,string parent_id)//canbe_traced表明这是一个楼中楼，可以被追踪
        {
            string url = "https://api.cc98.org/topic/" + replyid + "/post";
            var reply = new Dictionary<string, object>();
            if (canbe_traced)
            {
                reply = new Dictionary<string, object>()
            {
                {"clientType",1 },
                {"content",content },
                {"contentType",content_type },
                {"isAnonymous",false },
                {"notifyAllReplier",false },
                {"title","" },
                {"parentId",parent_id }

            };
            }
            else
            {
                reply = new Dictionary<string, object>()
            {
                {"clientType",1 },
                {"content",content },
                {"contentType",content_type },
                {"isAnonymous",false },
                {"notifyAllReplier",false },
                {"title","" }
            };
            }
            string reply_text = JsonConvert.SerializeObject(reply);
            var request_body = new StringContent(reply_text, Encoding.UTF8, "application/json");
            try
            {
                var r = await CCloginservice.vpn.PostAsync(url, request_body);
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await r.Content.ReadAsStringAsync();
                }
                else
                {
                    return "101:"+r.StatusCode.ToString();
                }
            }
            catch(Exception ex)
            {
                return "400:"+ex.Message;
            }
        }
        public static async Task<string> SendPrivateMsg(int receiver_id, string content)
        {
            string url = "https://api.cc98.org/message";
            var post = new Dictionary<string, object>()
            {
                {"receiverId",receiver_id},
                {"content",content}
            };
            string post_text = JsonConvert.SerializeObject(post);
            var request_body = new StringContent(post_text, Encoding.UTF8, "application/json");
            var r = await CCloginservice.vpn.PostAsync(url, request_body);
            if (r.IsSuccessStatusCode)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }
        public static async Task<string> SendVoteResult(string id,List<int> list)
        {
            string url = $"https://api.cc98.org/topic/{id}/vote";
            var post = new Dictionary<string, object>()
            {
                {"items",list}
            };
            string post_text = JsonConvert.SerializeObject(post);
            var request_body = new StringContent(post_text, Encoding.UTF8, "application/json");
            var r = await CCloginservice.vpn.PostAsync(url, request_body);
            if (r.IsSuccessStatusCode)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }
        public static async Task<string> EditFocusList(string mode,string group_id)
        {
            string url = $"https://api.cc98.org/me/custom-board/{group_id}";
            if (mode == "add")
            {
                try
                {
                    var content = new StringContent("", Encoding.UTF8, "application/json");
                    var response = await CCloginservice.vpn.PutAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        return "1";
                    }
                    else
                    {
                        return "404:固定失败";
                    }
                }
                catch (HttpRequestException ex)
                {
                    return $"404:{ex.Message}";
                }

            }
            else
            {
                try
                {

                    var res = await CCloginservice.vpn.DeleteAsync(url);
                    if (res.IsSuccessStatusCode)
                    {
                        return "1";
                    }
                    else
                    {
                        return "404:删除失败";
                    }
                }
                catch (HttpRequestException ex)
                {
                    return $"404:{ex.Message}";
                }
            }
                
        }

        public static async Task<string> Follow(string mode,string id)
        {
            HttpResponseMessage res;
            try
            {
                string url = $"https://api.cc98.org/me/followee/{id}";
                if (mode == "0")//取消关注
                {
                    res = await CCloginservice.vpn.DeleteAsync(url);
                }
                else
                {
                    res = await CCloginservice.vpn.PutAsync(url,null);
                }

                if (res.StatusCode == HttpStatusCode.OK)
                {
                    return "1";
                }
                else
                {
                    return "0";
                }
            }
            catch(Exception ex)
            {
                return $"2:{ex.Message}";
            }
        }
    }

    //将json字符串解析为目标对象,总是返回对象或者null。
    //要获取错误信息，请接收并检验RequestSender的返回值。解析器不会处理错误，所以输入解析器的字符串必须有效。
    public static class Deserializer
    {
        //将文本转为JArray
        public static JArray ToArray(string re)
        {
            try
            {
                var Posts= JsonConvert.DeserializeObject<JArray>(re);
                if (Posts != null && Posts.Count > 0)
                {
                    return Posts; // 返回解析后的JArray
                }
                else
                {
                    return null; // 返回null表示解析失败或无数据
                }
            }
            catch (Exception ex)
            {
                return null; // 返回null表示解析失败
            }
        }

        public static Dictionary<string, string> UserInfoList(string userinfo)
        {
            var PortList = JsonConvert.DeserializeObject<JArray>(userinfo);
            Dictionary<string, string> PortDict = new Dictionary<string, string>();
            if (PortList == null || PortList.Count == 0)
            {
                return PortDict; // 返回空字典
            }
            else
            {
                try
                {
                    foreach (var p in PortList)
                    {
                        var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(p.ToString());
                        if (info != null)
                        {
                            string purl = info["portraitUrl"].ToString();
                            string id = info["id"].ToString();
                            PortDict[id] = purl;
                        }
                    }
                    return PortDict; // 返回包含用户信息的字典
                }
                catch
                {
                    return null;
                }
            }
        }
        public static Dictionary<string, object> ToDictionary(string index)//将本地缓存或者在线数据转化为字典。
        {
            if (!index.StartsWith("10")&&(!index.StartsWith("404")))//10为文件系统错误类型。
            {
                try
                {
                    var IndexContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(index);
                    if (IndexContent != null)
                    {
                        return IndexContent;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static StandardPost ToItem(string TopicText)
        {
            //此函数专用于解析帖子，应对有无MediaType的情况，用于版面主页和新帖页。
            Dictionary<string, object> TopicProperty = JsonConvert.DeserializeObject<Dictionary<string, object>>(TopicText);
            if (TopicProperty != null)
            {
                string pid = "0";
                string uid = "0";
                if (TopicProperty["id"] != null)
                {
                    pid= TopicProperty["id"].ToString();
                }
                string hit = TopicProperty["hitCount"].ToString();
                string title = TopicProperty["title"].ToString();
                string time = TopicProperty["time"].ToString();
                string reply = TopicProperty["replyCount"].ToString();
                string author = "@ 匿名";
                if (ValidationHelper.GetKey(TopicProperty, "userId") != "0")
                {
                    uid = ValidationHelper.GetKey(TopicProperty, "userId");
                }
                List<MediaContent> images = new();
                List<MediaContent> videos = new();
                if (TopicProperty["userName"] != null)
                {
                    author ="@ "+ TopicProperty["userName"].ToString();
                }
               
                if (TopicProperty.TryGetValue("mediaContent", out var value))
                {
                    if (value != null)
                    {
                        if (value.ToString() != null)
                        {
                            var js=JsonConvert.DeserializeObject<Dictionary<string,object>>(value.ToString());
                            if (js != null)
                            {
                                if (js.ContainsKey("thumbnail"))
                                {
                                    if (js["thumbnail"] != null)
                                    {
                                        var MediaList = JsonConvert.DeserializeObject<JArray>(js["thumbnail"].ToString());
                                        foreach (var media in MediaList)
                                        {
                                            string link = media.ToString();
                                            var result = LinkAnalyzer.LinkDefinite(link);
                                            if (result.Value == "image")
                                            {
                                                images.Add(new MediaContent { MediaType = "image", MediaSource = link });
                                            }
                                        }
                                    }

                                }
                            }
                            
                            
                        }
                    }
                }
                return new StandardPost { author = author, reply = reply, hit = hit, time = time, pid = pid, images=images,videos=videos, title = title,rid=uid };

            }
            else
            {
                return null;
            }
        }
    }
    public static class ValidationHelper
    {
        public static async Task CopyStreamToRandomAccessStream(Stream input, IRandomAccessStream output)
        {
            var buffer = new byte[16 * 1024];
            int bytesRead;

            // 获取输出流的写入器
            using (var outputStream = output.GetOutputStreamAt(0))
            using (var writer = new DataWriter(outputStream))
            {
                while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    writer.WriteBytes(buffer.AsSpan(0, bytesRead).ToArray());
                    await writer.StoreAsync();
                    await outputStream.FlushAsync();
                }
                await writer.FlushAsync();
            }
        }
        public static bool IsValidResponse(string response)
        {
            if(string.IsNullOrEmpty(response))
            {
                 return false; // 返回false表示响应内容为空
            }
            else
            {
                if(response.StartsWith("404:"))
                {
                    return false; // 返回false表示响应内容包含错误代码
                }
                else
                {
                    return true; // 返回true表示响应内容有效
                }
            }



        }
        public static async Task<string> AutoResponse(HttpResponseMessage res)
        {
            try
            {
                if (res.IsSuccessStatusCode)
                {
                    string Text = await res.Content.ReadAsStringAsync();
                    if (IsValidResponse(Text))
                    {
                        return Text;
                    }
                    else
                    {
                        return "404:空返回";
                    }
                }
                else
                {
                    return "404:请求失败";
                }
            }
            catch
            {
                return "404:连接出错";
            }
        }
        public static string IsTokenExist(ApplicationDataContainer container,string key)
        {
            if (container.Values.TryGetValue(key,out var token))
            {
                if (token != null)
                {
                    var _token = token.ToString();
                    if (!string.IsNullOrEmpty(_token))
                    {
                        return _token;
                    }
                }
            }
            return "0";
        }
        public static string JsonReader(string path)
        {
            if (File.Exists(path))
            {
                string content=File.ReadAllText(path);
                if(!string.IsNullOrEmpty(content))
                {
                    return content;
                }
                else if(content==null)
                {
                    return "100:null";
                }
                else
                {
                    return "100:空内容";
                }
            }
            else
            {
                return "101:不存在的文件";
            }

        }
        
        public static void JsonWritter(string json,string filename)
        { 
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            string path= cacheFolder.Path + "/" + filename;
            File.WriteAllText(path, json);
            //必须使用同步方法自动关闭流。否则，立刻进行读取将读取到空内容。
        }
        public static void Log(string description,string message)
        {
            var log = new Dictionary<string, object>()
            {
                {"描述",description},
                {"详细信息",message},
                {"时间",DateTime.Now},
            };
            string log_text=JsonConvert.SerializeObject(log);
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            string path = cacheFolder.Path + "/" + "log.json";
            string pre_log=JsonReader(path);
            string new_log = pre_log + "\r\n" + log_text;
            JsonWritter(new_log, "log.json");
        }
        public static string GetKey(Dictionary<string,object> dic, string key)//值不可为"0".
        {
            if (dic == null) return "0";
            if(dic.TryGetValue(key,out var value))
            {
                if (value != null)
                {
                    var _value= value.ToString();
                    if (_value != null)
                    {
                        return _value;
                    }
                }
            }
            return "0";
        }
        public static string GetValue(NameValueCollection collection, string key)
        {
            if (collection.AllKeys.Contains(key))
            {
                var value = collection[key];
                if(value is string _value)
                {
                    return _value;
                }
            }
            return "0";
        }
        public static string GetPropertyAsString(JsonElement root, string key)
        {
            string defaultValue = "0";
            if (root.TryGetProperty(key, out JsonElement element) &&
            element.ValueKind != JsonValueKind.Null)
            {
                return element.ValueKind == JsonValueKind.String
                    ? element.GetString() ?? defaultValue
                    : defaultValue;
            }
            return defaultValue;
        }
        public static int GetPropertyAsInt(JsonElement root, string key)
        {
            int defaultValue= 0;
            if (root.TryGetProperty(key, out JsonElement element) &&
            element.ValueKind != JsonValueKind.Null)
            {
                return element.ValueKind == JsonValueKind.Number
                    ? element.GetInt32()
                    : defaultValue;
            }
            return defaultValue;
        }
    }
    public static class PasswordManager
    {
        /// <summary>
        /// 保存密码到保险库
        /// </summary>
        /// <param name="username">用户名/标识</param>
        /// <param name="password">密码</param>
        /// 用户名均使用"CC98".
        public static void SavePassword(string password, string resource, string username = "CC98")
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("用户名不能为空");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("密码不能为空");
            if (string.IsNullOrEmpty(resource))
                throw new ArgumentException("资源名不能为空");
            var vault = new PasswordVault();

            // 如果凭据已存在则先删除
            RemovePassword(username, resource);

            // 创建并添加新凭据
            var cred = new PasswordCredential(resource, username, password);
            vault.Add(cred);
        }

        /// <summary>
        /// 从保险库检索密码
        /// </summary>
        /// <param name="username">用户名/标识</param>
       
        /// <returns>检索到的密码，未找到返回 null</returns>
        public static string RetrievePassword( string resource, string username = "CC98")
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("用户名不能为空");
            var vault = new PasswordVault();
            try
            {
                // 尝试检索凭据
                var cred = vault.Retrieve(resource, username);
                cred.RetrievePassword(); // 显式获取密码值
                return cred.Password;
            }
            catch (Exception ex) when (ex.HResult == unchecked((int)0x80070490)) // 元素未找到
            {
                return null;
            }
        }

        /// <summary>
        /// 从保险库删除密码
        /// </summary>
        /// <param name="username">用户名/标识</param>
       
        public static void RemovePassword( string resource, string username = "CC98")
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("用户名不能为空");

            var vault = new PasswordVault();
            

            try
            {
                // 尝试查找并删除凭据
                var cred = vault.Retrieve(resource, username);
                vault.Remove(cred);
            }
            catch (Exception ex) when (ex.HResult == unchecked((int)0x80070490)) // 元素未找到
            {
                // 凭据不存在，无需操作
            }
        }

        /// <summary>
        /// 检查是否存在指定凭据
        /// </summary>
        /// <param name="username">用户名/标识</param>
     
        /// <returns>凭据是否存在</returns>
        public static bool PasswordExists(string resource, string username = "CC98")
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("用户名不能为空");

            var vault = new PasswordVault();
            try
            {
                // 尝试检索凭据
                vault.Retrieve(resource, username);
                return true;
            }
            catch (Exception ex) when (ex.HResult == unchecked((int)0x80070490)) // 元素未找到
            {
                return false;
            }
        }

        /// <summary>
        /// 获取所有保存的凭据（当前资源）
        /// </summary>
       
        /// <returns>凭据列表</returns>
        public static IReadOnlyList<PasswordCredential> GetAllCredentials(string resource)
        {
            var vault = new PasswordVault();
            try
            {
                // 检索所有凭据并过滤当前资源的
                return vault.RetrieveAll()
                    .Where(c => c.Resource == resource)
                    .ToList()
                    .AsReadOnly();
            }
            catch
            {
                return new List<PasswordCredential>().AsReadOnly();
            }
        }

        /// <summary>
        /// 清除当前应用的所有凭据
        /// </summary>
      
        public static void ClearAllPasswords(string resource)
        {
            var vault = new PasswordVault();
            try
            {
                // 检索并删除所有当前资源的凭据
                var credentials = vault.RetrieveAll()
                    .Where(c => c.Resource == resource)
                    .ToList();

                foreach (var cred in credentials)
                {
                    vault.Remove(cred);
                }
            }
            catch
            {
                // 忽略错误
            }
        }
        public static void Logout()
        {
            PasswordManager.ClearAllPasswords("Access");
            PasswordManager.ClearAllPasswords("Refresh");
            PasswordManager.ClearAllPasswords("TWFID");
            PasswordManager.ClearAllPasswords("VpnUserName");
            PasswordManager.ClearAllPasswords("VpnPassWord");
        }
    }

    public static class UBBConverter
    {
        public static string Convert(string ubbText, bool IsImageVisible, bool escapeMarkdown = false)
        {
            var text = Preprocess(ubbText);

            // 处理块级元素（优先级从高到低）
            text = ConvertCodeBlocks(text);
            text=ConvertUBBTable(text);
            text = ConvertQuotes(text);
            text = ConvertLists(text);

            // 处理行内元素
            text = ConvertImages(text, IsImageVisible);
            text = ConvertLinks(text);
            text = ConvertEmoji(text);
            text = ConvertColor(text);
            text=ConvertTrimTextStyles(text);
            text = ConvertTextStyles(text);

            return escapeMarkdown ? EscapeMarkdown(text) : text;
        }

        private static string Preprocess(string input)
        {
            return input.Replace("\r\n", "  \n")
                        .Replace("\r", "  \n")
                        .Replace("\n", "  \n")
                        .Replace("<br>", "  \n")
                        .Replace("[line]","  \n  \n---")
                        .Trim();
        }
        //两个空格加\n是markdown控件的换行格式。\n和\r是操作系统回车键的格式。字符串"\n"是cc98传输文本的换行格式。
        private static string ConvertCodeBlocks(string input)
        {
            return Regex.Replace(input,
                @"\[code\](.*?)\[/code\]",
                m => $"```\n{m.Groups[1].Value.Trim()}\n```",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }
        public static string ConvertUBBTable(string input)
        {
            // 匹配UBB表格标签
            var tableRegex = new Regex(@"\[table\](.*?)\[/table\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return tableRegex.Replace(input, ConvertTable);
        }

        private static string ConvertTable(Match tableMatch)
        {
            string tableContent = tableMatch.Groups[1].Value;
            var rowRegex = new Regex(@"\[tr\](.*?)\[/tr\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var rows = new List<List<string>>();

            // 提取所有行
            foreach (Match rowMatch in rowRegex.Matches(tableContent))
            {
                string rowContent = rowMatch.Groups[1].Value;
                var cellRegex = new Regex(@"\[(th|td)\](.*?)\[/\1\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                var rowCells = new List<string>();

                // 提取行内所有单元格
                foreach (Match cellMatch in cellRegex.Matches(rowContent))
                {
                    string cellValue = cellMatch.Groups[2].Value;
                    // 转义Markdown特殊字符 | 和换行符
                    cellValue = cellValue.Replace("|", "\\|").Replace("\r\n", " ").Replace("\n", " ");
                    rowCells.Add(cellValue);
                }
                rows.Add(rowCells);
            }

            if (rows.Count == 0) return string.Empty;

            // 确定最大列数
            int maxCols = rows.Max(row => row.Count);
            if (maxCols == 0) return string.Empty;

            // 补齐空单元格
            foreach (var row in rows)
            {
                while (row.Count < maxCols)
                {
                    row.Add("");
                }
            }

            // 构建Markdown表格
            var markdown = new StringBuilder();
            for (int i = 0; i < rows.Count; i++)
            {
                markdown.Append("| ");
                markdown.Append(string.Join(" | ", rows[i]));
                markdown.AppendLine(" |");

                // 添加表头分隔行
                if (i == 0)
                {
                    markdown.Append("| ");
                    for (int j = 0; j < maxCols; j++)
                    {
                        markdown.Append("---");
                        if (j < maxCols - 1) markdown.Append(" | ");
                    }
                    markdown.AppendLine(" |");
                }
            }

            return "  \n" + markdown.ToString();
        }
        private static string ConvertQuotes(string input)
        {

            string pattern = @"(\[quote\])|(\[/quote\])";

            // 使用栈来处理嵌套层级。实际上栈不起作用，只是为了借用栈的思想。

            StringBuilder output = new StringBuilder();
            int currentLevel = 0;

            // 当前处理的文本
            int lastIndex = 0;

            // 正则匹配标签并进行替换
            foreach (Match match in Regex.Matches(input, pattern))
            {
                // 获取标签的开始位置
                int matchStart = match.Index;
                // 获取标签的结束位置
                int matchEnd = match.Index + match.Length;

                // 先处理标签之前的文本


                if (match.Value == "[quote]")
                {
                    output.Append(input.Substring(lastIndex, matchStart - lastIndex));
                    // 处理 [quote] 标签：增加层级并推入栈

                    currentLevel++;
                    // 添加 Markdown 格式的引用
                    output.Append(new string('>', currentLevel) + " ");
                }
                else if (match.Value == "[/quote]")
                {
                    output.Append(new string(input.Substring(lastIndex, matchStart - lastIndex).Replace("\n", "  \n" + new string('>', currentLevel)).Replace("\r", "  \n" + new string('>', currentLevel)) + "  \n" + new string('>', currentLevel - 1) + "  \n" + new string('>', currentLevel - 1)));
                    // 处理 [/quote] 标签：减少层级并弹出栈.
                    //将系统换行符替换为引用符号非常关键，构造正确的换行结构和引用的续引用。
                    currentLevel--;

                    // 不添加文本，只需要关闭当前层级的引用
                }

                // 更新 lastIndex
                lastIndex = matchEnd;
            }

            // 处理最后一个标签之后的文本
            output.Append("  \n" + input.Substring(lastIndex));


            return output.ToString();
        }

        private static string ConvertLists(string input)
        {
            return Regex.Replace(input,
                @"\[list\](.*?)\[/list\]",
                m => ProcessListItems(m.Groups[1].Value),
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        private static string ProcessListItems(string content)
        {
            var items = Regex.Matches(content, @"\[\*\]([^\[]+)");
            return string.Join("\n", items.Cast<Match>()
                .Select(m => $"* {m.Groups[1].Value.Trim()}"));
        }

        private static string ConvertImages(string input, bool mode)
        {
            if (mode)
            {
                return Regex.Replace(input,
        @"\[img\](.*?)\[/img\]",
        "![#**图片**#]($1)",
        RegexOptions.IgnoreCase);
            }
            else
            {
                return Regex.Replace(input,
        @"\[img\](.*?)\[/img\]",
        "[#**图片**#]($1)",
        RegexOptions.IgnoreCase);
            }


        }
        private static string ConvertColor(string input)
        {
            string pattern = @"\[color=[^\]]*\](.*?)\[/color\]";

            // 循环处理，逐层去除嵌套的 color 标签
            string result = input;
            while (Regex.IsMatch(result, pattern))
            {
                result = Regex.Replace(result, pattern, "$1");
            }
            return result;
        }

        private static string ConvertLinks(string input)
        {
            // 带标题的链接 [url=...]...[/url]
            input = Regex.Replace(input,
            @"\[url=(.*?)\](.*?)\[/url\]",
            "[$2]($1)",
            RegexOptions.IgnoreCase);

            // 无标题链接 [url]...[/url]
            return Regex.Replace(input,
                @"\[url\](.*?)\[/url\]",
                "[$1]($1)",
                RegexOptions.IgnoreCase);
        }
        private static string ConvertEmoji(string input)
        {
            var replacements = new[]
            {
                    (@"\[ac(\d{2,4})\]","![#ac$1#](ms-appx:///Assets/Emoji/ac-white/ac$1.png)"),//ac娘
                    (@"\[em(\d{2})\]","![#em$1#](ms-appx:///Assets/Emoji/em/em$1.gif)"),//经典
                    (@"\[([a-zA-Z]{2})(\d{2})\]","![#$1$2#](ms-appx:///Assets/Emoji/$1/$1$2.png)"),//贴吧，雀魂
                    (@"\[cc98(\d{2})\]","![#cc98$1#](ms-appx:///Assets/Emoji/CC98/CC98$1.png)")//cc98

                };
            foreach (var (pattern, replacement) in replacements)
            {
                input = Regex.Replace(input, pattern, replacement,
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            return input;
        }
        
        private static string ConvertTrimTextStyles(string input)
        {
            var TrimList=new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>(@"\[b\](.*?)\[/b\]","**"),
                new KeyValuePair<string, string>(@"\[i\](.*?)\[/i\]","*"),
                new KeyValuePair<string, string>(@"\[u\](.*?)\[/u\]",""),
                new KeyValuePair<string, string>(@"\[del\](.*?)\[/del\]","~~")
            };
            foreach(var kvp in TrimList)
            {
                input = Regex.Replace(input, kvp.Key, match =>
                {
                    string content = match.Groups[1].Value;
                    // 分割内容为多个段落    
                    string[] paragraphs = content.Split(new[] { "  \n" }, StringSplitOptions.None);

                    // 为每个段落单独添加加粗标记
                    for (int i = 0; i < paragraphs.Length; i++)
                    {
                        // 移除每段前后的空白，但保留内部格式
                        string trimmed = paragraphs[i].Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            paragraphs[i] = $"{kvp.Value}{trimmed}{kvp.Value}";
                        }
                    }

                    // 重新组合段落
                    return string.Join("  \n", paragraphs);
                }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            return input;


            
        }
        private static string ConvertTextStyles(string input)
        {
            var replacements = new[]
        {
            (@"\[color=[^\]]*\](.*?)\[/color\]", "$1"),
            (@"\[font=.*?\](.*?)\[/font\]", "$1"),
            (@"\[size=\d{1,2}\]",""),
            (@"\[/size\]",""),
            (@"\[center\](.*?)\[/center\]","$1"),
            (@"\<center\>(.*?)\</center\>","$1"),
            (@"\[right\](.*?)\[/right\]","$1"),
            (@"\[left\](.*?)\[/left\]","$1"),
            (@"<p[^>]*>(.*?)</p>","$1"),
            (@"\[align=[^\]]+\](.*?)\[/align\]","$1"),
            (@"<img\s+[^>]*src=""([^""]+)""[^>]*>","[#**图片**#]($1)"),
            (@"@(\S+)\s","[@ $1 ](https://api.cc98.org/user/name/$1)"),
            (@"\[audio\](.*?)\[/audio\]","[#**音频**#]($1)"),
            (@"\[video\](.*?)\[/video\]","[#**视频**#]($1)"),
            (@"\[upload\](.*?)\[/upload\]","[#**文件**#]($1)"),
            (@"\[bili\](.*?)\[/bili\]","[#**哔哩**#](https://www.bilibili.com/video/$1/)"),
            
        };

            foreach (var (pattern, replacement) in replacements)
            {
                input = Regex.Replace(input, pattern, replacement,
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }

            return input;
        }

        private static string Cleanup(string input)
        {
            // 合并多余空行
            return Regex.Replace(input, @"\n{3,}", "\n\n");
        }

        private static string EscapeMarkdown(string input)
        {
            var charsToEscape = new[] { '\\', '_', '+', '-', '.' };
            return charsToEscape.Aggregate(input, (current, c) =>
                current.Replace(c.ToString(), $"\\{c}"));
        }
    }

    public static class LinkAnalyzer
    {
        public static KeyValuePair<string, string> LinkDefinite(string link)
        {
            if (!string.IsNullOrEmpty(link))
            {

                if (link.Contains("user/name"))
                {

                    string pattern = @"https:\/\/api\.cc98\.org\/user\/name\/([^\/\s]+)";
                    MatchCollection matches = Regex.Matches(link, pattern);
                    if (matches.Count == 1)
                    {
                        string username = matches[0].Groups[1].Value;
                        return new KeyValuePair<string, string>("user", username);
                    }
                    else
                    {
                        return new KeyValuePair<string, string>("null", link);
                    }

                }
                else if (link.Contains("/topic/") && (!link.Contains("#")))
                {
                    string pattern = @"\/topic\/([^\/\s]+)";
                    MatchCollection matches = Regex.Matches(link, pattern);
                    if (matches.Count == 1)
                    {
                        string pid = matches[0].Groups[1].Value;
                        return new KeyValuePair<string, string>("topic", pid);
                    }
                    else
                    {
                        return new KeyValuePair<string, string>("null", link);
                    }
                }
                else if (link.Contains("#"))
                {
                    Match match = Regex.Match(link, @"/topic/(\d{7})/(\d+)#(\d+)");
                    if (match.Success)
                    {
                        string numberAfterHash = match.Groups[1].Value;
                        return new KeyValuePair<string, string>("anchor", link);//返回索引楼层
                    }
                    return new KeyValuePair<string, string>("null", link);
                }
                else if (link.Contains("file"))
                {
                    string ext = Path.GetExtension(link)?.TrimStart('.').ToLowerInvariant();

                    HashSet<string> picformats = new() { "jpg", "jpeg", "png", "gif", "webp" };
                    HashSet<string> audioformats = new() { "mp3", "wav", "m4a", "ogg", "flac" };
                    HashSet<string> videofromats = new() { "mp4", "avi", "mkv", "mov", "wmv" };

                    if (!string.IsNullOrEmpty(ext))
                    {
                        if (picformats.Contains(ext))
                        {
                            return new KeyValuePair<string, string>("file", "image");
                        }
                        else if (audioformats.Contains(ext))
                        {
                            return new KeyValuePair<string, string>("file", "audio");
                        }
                        else if (videofromats.Contains(ext))
                        {
                            return new KeyValuePair<string, string>("file", "video");
                        }
                        else
                        {
                            return new KeyValuePair<string, string>("file", "doc");
                        }
                    }
                    else
                    {
                        return new KeyValuePair<string, string>("null", link);
                    }

                }
                else if (link.Contains("https://www.bilibili.com/video"))
                {
                    return new KeyValuePair<string, string>("backlink", "bili");
                }
                else if (link.Contains("board"))
                {
                    Match match = Regex.Match(link, @"\/board\/(\d{2,3})");
                    if (match.Success)
                    {
                        string board_id = match.Groups[1].Value;
                        return new KeyValuePair<string, string>("board",board_id );
                    }
                    return new KeyValuePair<string, string>("null", link);
                }
                else
                {
                    return new KeyValuePair<string, string>("null", link);
                }
            }
            else
            {
                return new KeyValuePair<string, string>("null", link);
            }

        }
    }
}


