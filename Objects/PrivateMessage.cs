using System.Text.Json.Serialization;

namespace CC98.Objects;

public record PrivateMessage
{
    [JsonPropertyName("receiverId")] public int ReceiverId { get; init; }

    [JsonPropertyName("content")] public required string Content { get; init; }
}