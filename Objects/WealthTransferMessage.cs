using System.Collections.Generic;

namespace CC98.Objects;

public class WealthTransferMessage
{
    public int Wealth { get; set; }
    public List<string> UserNames { get; set; } = [];
    public string Reason { get; set; } = "";
}
