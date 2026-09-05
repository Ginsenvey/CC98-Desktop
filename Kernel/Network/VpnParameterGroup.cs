using System;
using System.Collections.Generic;
using System.Text;

namespace CC98.Kernel.Network;
/// <summary>
/// 用于暴露给外部接口和记录状态的参数组
/// </summary>
public class VpnParameterGroup
{
    public string CaptchaValue { get; set; } = "";
    //上一次获取的随机代码，用于登录请求
    public string LastRandCode { get; set; } = "";
    //上一次获取的验证码ID，用于获得图形验证码
    public string LastCaptchaId { get; set; } = "";
}
