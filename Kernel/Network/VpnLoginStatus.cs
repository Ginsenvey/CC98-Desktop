namespace CC98.Kernel.Network;

public enum VpnLoginStatus
{
    Success = 0,
    NeedCaptcha=1,        //欠费或凭据错误
    NeedConfirm=2,          //需要顶号
    Error=3,               
}