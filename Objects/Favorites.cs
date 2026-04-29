using System.Text.Json.Serialization;

namespace CC98.Objects;

/// <summary>
///     收藏夹
/// </summary>
public class Favorites
{
    //收藏夹名称
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    //收藏夹ID
    [JsonPropertyName("id")] public int Id { get; set; }
}