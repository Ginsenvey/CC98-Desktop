namespace CC98.Kernel.Network;
public enum NetworkStatus
{
    InCampus = 0,                   //在校园网内
    NotInCampus = 1,
    ByVPN = 2,                      //使用WebVPN连接到内网
    TicketMissing = 3,              // Ticket令牌缺失
    TicketParseFailed = 4,          // Ticket解析失败
    CredentialsError = 5,           // 凭据错误或VPN欠费
    CredentialsIncomplete = 6,      // 凭据不完整
    VpnDisabled = 7,                // VPN未启用
    UnknownError = 8,               // 未知错误,有可能是vpn凭据过期，也可能是MirrorError
    TicketNotSave = 9,           // Ticket保存失败
    NoConnection = 10,               //无网络
    MirrorError=11,                  //IP被镜像站拦截访问
}


public enum VPNLoginStatus
{
    Success = 0,
    NeedCaptcha=1,        //欠费或凭据错误
    NeedConfirm=2,          //需要顶号
    Error=3,               
}