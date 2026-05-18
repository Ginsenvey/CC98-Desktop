using System;
using System.Collections.Generic;
using System.Text;

namespace CC98.Objects;
public record OpenIdOptions
{
    public const string SectionName = "OpenId";
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; } 
    public  required string RedirectUri { get; init; } 
}
