using System.Net;
using System.Text.Json.Serialization;

namespace CC98.Kernel.Network;

public record VpnLoginResult
{
    [JsonIgnore]
    public VpnLoginStatus Status
    {
        get
        {
            if (Success) return VpnLoginStatus.Success;
            if (NeedConfirm) return VpnLoginStatus.NeedConfirm;
            if (AccounInvalid) return VpnLoginStatus.AccoutInvalid;
            if (CaptchaFail) return VpnLoginStatus.CaptchaFail;

            return VpnLoginStatus.Fail;
        }
    }

    [JsonIgnore] public bool NeedConfirm => Error == "NEED_CONFIRM";
    /// <summary>
    /// 针对输入错误图形验证码的情况。刷新图形验证码。
    /// </summary>
    [JsonIgnore] public bool AccounInvalid => Error == "INVALID_ACCOUNT";

    [JsonIgnore]
    public bool CaptchaFail => Error == "CAPTCHA_FAILED";

    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; } = string.Empty;


    [JsonPropertyName("message")] public string? Message { get; set; } = string.Empty;

    [JsonPropertyName("error")] public string? Error { get; set; } = string.Empty;

}