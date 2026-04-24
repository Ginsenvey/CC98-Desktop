using System.Text.Json.Serialization;

namespace CC98.Objects;

/// <summary>
/// 点赞状态
/// </summary>
public class ReactionState
{
    [JsonPropertyName("likeCount")]
    public int LikeCount { get; set; }

    [JsonPropertyName("dislikeCount")]
    public int DislikeCount { get; set; }

    [JsonPropertyName("likeState")]
    public int LikeState { get; set; }
}
