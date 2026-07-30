using CC98.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CC98.Kernel.Network;

public class CookieService(CookieContainer cookieContainer) : ICookieService
{
    //持久化到加密存储
    public void SaveCookieHeader(string url)
    {
        PasswordManager.SavePassword(cookieContainer.GetCookieHeader(new Uri(url)), "VpnCookie", url);
    }
    public string GetCookieHeader(string url)
    {
        return PasswordManager.RetrievePassword("VpnCookie", url);
    }
}
