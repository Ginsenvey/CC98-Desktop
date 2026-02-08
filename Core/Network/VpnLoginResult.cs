using System.Net;
using System.Text.Json.Serialization;

namespace CC98.Kernel.Network;

public class VpnLoginResult
{
    [JsonIgnore]
    public VPNLoginStatus Status { get; set; }

    [JsonIgnore]
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public bool NeedConfirm => Error == "NEED_CONFIRM";
    [JsonIgnore]
    public bool CaptchaFail => Error == "CAPTCHA_FAILED";//验证码错误

    [JsonPropertyName("success")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; } = string.Empty;

    
    [JsonPropertyName("message")]
    public string? Message { get; set; } = string.Empty;
    [JsonPropertyName("error")]
    public string? Error { get; set; } = string.Empty;

    [JsonIgnore]
    public HttpStatusCode? HttpStatusCode { get; set; }

    public static VpnLoginResult Success(string? url = null, string? message = null) =>
        new() { Status=VPNLoginStatus.Success };
    
    public static VpnLoginResult Failure(string description) =>
        new() { Status=VPNLoginStatus.Error, Description=description };

    public static VpnLoginResult ConfirmRequired() =>
        new() { Status = VPNLoginStatus.NeedConfirm, Description = "需要确认登录" };
   

}