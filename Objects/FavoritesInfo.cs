using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CC98.Objects;

public class FavoritesInfo
{
    [JsonPropertyName("data")]
    public List<Favorites> FavoriteTopicGroups { get; set; } = [];
}
