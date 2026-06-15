using System.Net;
using System.Text.Json.Serialization;

namespace CC98.Kernel.Network;

public record VpnLoginResult
{
    [JsonIgnore] public VpnLoginStatus Status
    {
        get
        {
            if (Success) return VpnLoginStatus.Success;
            if (NeedConfirm) return VpnLoginStatus.NeedConfirm;
            if (CaptchaFail) return VpnLoginStatus.NeedCaptcha;
            return VpnLoginStatus.Fail;
        }
    }

    [JsonIgnore] public bool NeedConfirm => Error == "NEED_CONFIRM";

    [JsonIgnore] public bool CaptchaFail => Error == "CAPTCHA_FAILED"; //验证码错误

    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; } = string.Empty;


    [JsonPropertyName("message")] public string? Message { get; set; } = string.Empty;

    [JsonPropertyName("error")] public string? Error { get; set; } = string.Empty;
  
}