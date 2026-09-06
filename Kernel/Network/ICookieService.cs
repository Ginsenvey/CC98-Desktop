using System;
using System.Collections.Generic;
using System.Text;

namespace CC98.Kernel.Network;

public interface ICookieService
{
    void SaveCookieHeader(string url);
    string? GetCookieHeader(string url);
}
