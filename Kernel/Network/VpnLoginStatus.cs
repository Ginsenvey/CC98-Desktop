namespace CC98.Kernel.Network;

public enum VpnLoginStatus
{
    Success = 0,
    AccoutInvalid = 1, //欠费或凭据错误
    CaptchaFail =2,//验证码错误
    NeedConfirm = 3, //需要顶号
    Fail = 4
}