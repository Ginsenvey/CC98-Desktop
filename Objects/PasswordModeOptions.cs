using System;
using System.Collections.Generic;
using System.Text;

namespace CC98.Objects;

public record PasswordModeOptions
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string Scope { get; init; }
}
