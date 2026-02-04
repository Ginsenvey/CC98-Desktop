namespace CC98.Kernel.Network;
public enum NetworkStatus
{
    InCampus = 0,                   //在校园网内
    ByVPN = 1,                      //使用WebVPN连接到内网
    TicketMissing = 2,              // Ticket令牌缺失
    TicketParseFailed = 3,          // Ticket解析失败
    CredentialsError = 4,           // 凭据错误或VPN欠费
    CredentialsIncomplete = 5,      // 凭据不完整
    VpnDisabled = 6,                // VPN未启用
    UnknownError = 7,               // 未知错误
    TicketNotSave = 8,           // Ticket保存失败
    NoConnection = 9,               //无网络
}


public enum VPNLoginStatus
{
    Success = 0,
    NeedCaptcha=1,        //欠费或凭据错误
    NeedConfirm=2,          //需要顶号
    Error=3,               
}