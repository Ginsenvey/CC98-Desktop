using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CC98.Objects;

public class MediaContent
{
    [JsonPropertyName("thumbnail")]
    public List<string> Thumbnail { get; set; } = new();
}
