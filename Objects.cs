using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CC98.Objects;
/// <summary>
/// 收藏夹
/// </summary>
public class Favorites
{
    //收藏夹名称
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    //收藏夹ID
    [JsonPropertyName("id")]
    public required int Id { get; set; }
}

/// <summary>
/// 主题帖数据
/// </summary> 
public class  TopicMetaData
{
    [JsonPropertyName("title")]
    public required string Title { get; set; } = "";
    [JsonPropertyName("favoriteCount")]
    public required int FavoriteCount { get; set; } = 0;
    [JsonPropertyName("replyCount")]
    public required int ReplyCount { get; set; } = 0;
    [JsonPropertyName("hitCount")]
    public required int HitCount { get; set; } = 0;
    [JsonPropertyName("time")]
    public required string Time{ get; set; }
    [JsonPropertyName("isVote")]
    public bool IsVote { get; set; }
    
    [JsonIgnore]
    public bool IsFavorite { get; set; } = false;

    //内置时间转换函数
    public string FormattedTime => DateTime.Parse(Time).ToString("yyyy-MM-dd HH:mm:ss");
}

/// <summary>
/// 
