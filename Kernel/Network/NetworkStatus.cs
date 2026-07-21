namespace CC98.Kernel.Network;

public enum NetworkStatus
{
    InCampus = 0, //在校园网内
    NotInCampus = 1,
    TicketMissing = 2, // Ticket令牌缺失
    TicketParseFailed = 3, // Ticket解析失败
    CredentialsError = 4, // 凭据错误或VPN欠费
    CredentialsIncomplete = 5, // 凭据不完整
    UnknownError = 6, // 未知错误,有可能是vpn凭据过期，也可能是MirrorError
    CookieNotSave = 7, // Cookie保存失败
    NoConnection = 8, //无网络
    MirrorError = 9, //IP被镜像站拦截访问
    VpnExpired = 10, // VPN凭据过期
}