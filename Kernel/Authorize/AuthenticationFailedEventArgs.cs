using System;
using System.Collections.Generic;
using System.Text;

namespace CC98.Kernel.Authorize;

public class AuthenticationFailedEventArgs : EventArgs
{
    public required string Reason { get; set; }
    public Exception? Exception { get; set; }
}
