using CC98.Kernel;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CC98.Objects;
/// <summary>
/// 收藏夹
/// </summary>
public class Favorites
{
    //收藏夹名称
    [JsonPropertyName("name")]
    public string Name { get; set; }=string.Empty;
    //收藏夹ID
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

/// <summary>
/// 主题帖数据
/// </summary> 
public partial class TopicInfo:ObservableObject
{
    public int Id
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public string UserName
    {
        get => field ?? "匿名";
        set => SetProperty(ref field, value);
    }
    public int? UserId
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public string Title
    {
        get => field??string.Empty;
        set => SetProperty(ref field, value);
    }
 
    public int FavoriteCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    
    public int ReplyCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    
    public int HitCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public bool IsAnonymous
    {
        get => field;
        set=>SetProperty(ref field, value);
    }
 
    public DateTime Time
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public bool IsMe
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public bool IsVote
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    
    [JsonIgnore]
    public bool IsFavorite
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    [JsonPropertyName("mediaContent")]
    public MediaContent MediaContent
    {
        get => field ?? new MediaContent();
        set => SetProperty(ref field, value);
    }

    
}





/// <summary>
/// 版面信息
/// </summary>
public partial class BoardData:ObservableObject
{
    //约定：私有字段在前，使用驼峰命名法，公共属性在后，使用帕斯卡命名法
    //使用SetProperty以启用通知，不使用[ObservableProperty]，保持AOT兼容性
    //使用OnPropertyChanged(nameof(类型名))更新计算属性
    //在计算属性中处理转换器、格式化等逻辑，尽量避免使用Converter
    private List<string> _boardMasters = [];
    public int _id = 0;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _bigPaper = string.Empty;
    private int _todayCount = 0;
    private int _topicCount = 0;

    [JsonPropertyName("boardMasters")]
    public required List<string> BoardMasters
    {
        get=> _boardMasters;
        set
        {
            if(SetProperty(ref _boardMasters, value))
            {
                OnPropertyChanged(nameof(BoardMastersString));
            }
        }
    }
 
    [JsonPropertyName("id")]
    public required int Id
    {
        get=> _id;
        set=> SetProperty(ref _id, value);
    }    
    
    [JsonPropertyName("name")]
    public required string Name
    {
        get=> _name;
        set => SetProperty(ref _name, value);
    }

    
    
    [JsonPropertyName("description")]
    public required string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

   
    
    [JsonPropertyName("bigPaper")]
    public required string BigPaper
    {
        get => _bigPaper;
        set 
        { 
            if(SetProperty(ref _bigPaper, value))
            {
                OnPropertyChanged(nameof(BannerText));
            }
        }
    }
    
    [JsonPropertyName("todayCount")]
    public required int TodayCount
    {
        get=> _todayCount;
        set
        {
            if (SetProperty(ref _todayCount, value))
            {
                OnPropertyChanged(nameof(TodayCountString));
            }
        }
    }
   
    [JsonPropertyName("topicCount")]
    public required int TopicCount
    {
        get => _topicCount;
        set
        {
            if(SetProperty(ref _topicCount, value))
            {
                OnPropertyChanged(nameof(TopicCountString));
            }
        }
    }
    //计算属性
    public string BoardMastersString => $"版主:{string.Join(",", BoardMasters)}";
    public string TodayCountString=> $"今日帖数：{TodayCount}";
    public string TopicCountString=> $"总话题数：{TopicCount}";

    public string BannerText => UBBConverter.Convert(BigPaper, true);
}
/// <summary>
/// 关注的人和被关注的人的详细信息，用于好友列表
/// </summary>
public class Friend : ObservableObject
{

    [JsonPropertyName("fanCount")]
    public int FanCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    [JsonPropertyName("userId")]
    public int UserId
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    [JsonPropertyName("postCount")]
    public int PostCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    [JsonPropertyName("name")]
    public string Name
    {
        get => field??string.Empty;
        set => SetProperty(ref field, value);
    }
    [JsonPropertyName("portraitUrl")]
    public string PortraitUrl
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
}
/// <summary>
/// 基础信息，用于从id获取头像
/// </summary>
public class BasicUserInfo
{
    public int Id { get; set;  }
    public string Name { get; set; } = string.Empty;
    public string PortraitUrl { get; set; } = string.Empty;
}

public class FavoritesInfo 
{
    [JsonPropertyName("data")]                                                                                                                                         
    public List<Favorites> FavoriteTopicGroups { get; set; } = new List<Favorites>();
}
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
/// <summary>
/// 详细个人信息
/// </summary>
public partial class UserInfo : ObservableObject
{
    public string Name
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
    public string SignatureCode
    {
        get => field ?? "[i]该用户还没设置签名档[i]";
        set => SetProperty(ref field, value);
    }
    public int Id
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int PostCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int Wealth
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public DateTime LastLogOnTime
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public string PortraitUrl
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }
    public int Popularity
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int FanCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int FollowCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public DateTime RegisterTime
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    [JsonIgnore]
    public bool IsOthers
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public bool IsFollowing
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public List<int> CustomBoards
    {
        get => field??[];
        set => SetProperty(ref field, value);
    }
}
/// <summary>
/// 用于单个版面页和个人主页的简单展示帖
/// </summary>
public partial class SimpleTopicInfo : ObservableObject
{
    public string UserName
    {
        get => field ?? "匿名";
        set => SetProperty(ref field, value);
    }

    public string Title
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
    public int Id
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public int BoardId
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    [JsonIgnore]
    public int SortId
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public string BoardName
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
    public int HitCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int ReplyCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public string Time
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
    public FluentIcons.Common.Symbol symbol
    {
        get => field;
        set => SetProperty(ref field, value);
    }

}


public partial class Reply : ObservableObject
{
    public string UserName
    {
        get => field??"";
        set => SetProperty(ref field, value);
    }
    public string PortraitUrl
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }
    public int? UserId
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public string Content
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }
    public int LikeCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int DislikeCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int Id
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int LikeState
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int Floor
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public bool IsMe
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public bool IsDeleted
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public bool IsAnonymous
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    
    public DateTime Time
    {
        get => field;
        set=>SetProperty(ref field, value);
    }

    
}

public class MediaContent
{
    [JsonPropertyName("thumbnail")]
    public List<string> Thumbnail {  get; set; }= new List<string>();

}

public partial class ChatMessage : ObservableObject
{
    public string Time
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    public string Content
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }


    [JsonPropertyName("id")]
    public int MessageId
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    [JsonPropertyName("receiverId")]
    public int ReceiverId
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    [JsonIgnore]
    public bool IsMe
    {
        get => field;
        set => SetProperty(ref field, value);
    }

}
public partial class ChatInfo : ObservableObject
{

    public string LastContent
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    public int UserId
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public string Time
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);

    }
    [JsonIgnore]
    public string Name
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    [JsonIgnore]
    public string PortraitUrl
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
}



public class VoteInfo
{
    public List<int> MyRecord { get; set; } = [];
    public List<VoteItem> VoteItems { get; set; } = [];
    //能否继续投票
    public bool CanVote {  get; set; }
    //是否过期
    public bool IsAvailable {  get; set; }
    //票数限制
    public int MaxVoteCount {  get; set; }
    public int expiredTime { get; set; }
    //总投票人数
    public int VoteUserCount {  get; set; }

}
public class VoteItem
{
    public int Id { get; set; }
    public int Count { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UnreadMessageInfo
{
    public int MessageCount { get; set; }
    public int ReplyCount { get; set; }
    public int AtCount { get; set; }
    public int SystemCount {  get; set; }
}

public class Notice
{
    [JsonPropertyName("title")]
    public string? Title {  get; set; }

    [JsonPropertyName("topicId")]
    public int? TopicId { get; set; }

    [JsonPropertyName("postId")]
    public int? PostId { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("postBasicInfo")]
    public PostBasicInfo? PostBasicInfo { get; set; }

    [JsonPropertyName("id")]
    public int Type { get; set;}
    [JsonPropertyName("content")]
    public string? Content {  get; set; }
}

public class PostBasicInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("floor")]
    public int Floor { get; set; }

    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; }=string.Empty;
}
/// <summary>
/// 抽卡数据
/// </summary>
public partial class CardStat : ObservableObject
{

    public int Wealth
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int DrawCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int TotalCost
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int TotalBonus
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    public int CardCount
    {
        get => field;
        set => SetProperty(ref field, value);
    }
}

public partial class Card : ObservableObject
{
    private bool _isFlipped=false;
    [JsonPropertyName("imageUri")]
    public string ImageUri
    {
        get => field??string.Empty;
        set => SetProperty(ref field, value);
    }
    [JsonIgnore]
    public bool IsFlipped
    {
        get => _isFlipped;
        set => SetProperty(ref _isFlipped, value);
    }
    public string Name
    {
        get => field??string.Empty;
        set => SetProperty(ref field, value);
    }

}
public class CardStatInfoPair
{
    public required string StatItem { get; set; }
    public int Value { get; set; }
}
/// <summary>
/// 抽卡概率信息
/// </summary>
public class GachaInfo
{
    public string Rank { get; set; } = string.Empty;
    public string Probability { get; set; } = string.Empty;
}

public class FlipTopic : ObservableObject
{
    public string Title
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    public string Url
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
    public string Time
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
    public string Content
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
}

public class SectionCard
{
    public required string SectionName { get; set; }
    public required string HexColor { get; set; }
    public List<IndexTopic> IndexTopics { get; set; } = [];
}
public class IndexTopic : ObservableObject
{
    public string Title
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
    public string BoardName
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
    public int TopicId
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public bool IsHotTopic
    {
        get => field;
        set => SetProperty(ref field, value);
    }
}
/// <summary>
/// 精华帖
/// </summary>
public class BoardBest
{
    public int Count { get; set; }
    public List<SimpleTopicInfo> Topics { get; set; } = [];
}