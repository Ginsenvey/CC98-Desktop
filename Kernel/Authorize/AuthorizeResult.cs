using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CC98.Kernel.Authorize;

/// <summary>
/// 表示登录授权的结果。
/// </summary>
public record AuthorizeResult
{
    /// <summary>
    /// 刷新令牌。
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 授权令牌。
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// 登录的错误信息。
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 登录的错误信息的描述。
    /// </summary>
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// 表示当前登录结果是否为成功状态。
    /// </summary>
    [MemberNotNullWhen(true, nameof(AccessToken))]
    [MemberNotNullWhen(true, nameof(RefreshToken))]
    public bool IsSucceeded =>
        !string.IsNullOrEmpty(RefreshToken) &&
        !string.IsNullOrEmpty(AccessToken) &&
        string.IsNullOrEmpty(Error); // 同时要求没有错误

    [JsonIgnore]
    public string Message =>
        IsSucceeded ? "授权成功" :
            $"{Error}: {ErrorDescription}";
}