using CC98.Objects;
using CC98.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC98.Kernel;
public class AppConfig
{
    #region 常量
    public const string WebClientId = "9a1fd200-8687-44b1-4c20-08d50a96e5cd";
    public const string DesktopClientId = "d47a2448-779f-42f3-164f-08dd8896bbe5";
    public const string WebClientSecret = "8b53f727-08e2-4509-8857-e34bf92b27f2";
    public const string OpenIdCallbackUrL = "cc98://callback";
    #endregion
    public bool IsPasswordMode
    {
        get =>AppSettings.Current.ActiveMode == (int)ActiveMode.Password;
    }
}
