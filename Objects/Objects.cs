using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CC98.Kernel;
using CommunityToolkit.Mvvm.ComponentModel;

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
public partial class TopicInfo:ObservableObject
{
    public string UserName
    {
        get => field ?? "匿名";
        set => SetProperty(ref field, value);
    }
    public int UserId
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
 
    public  DateTime Time
    {
        get => field;
        set
        {
            if(SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(FormattedTime));
            }
        } 
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

    //内置时间转换函数
    public string FormattedTime => Time.ToString("yyyy-MM-dd HH:mm:ss");
}

/// <summary>
/// 
public class RandomPost
{
    [JsonPropertyName("userId")]
    public required string UserId { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("replyCount")]
    public required string ReplyCount { get; set; }

    [JsonPropertyName("hitCount")]
    public required string HitCount { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("time")]
    public required DateTime Time { get; set; }

    public string FormattedTime => Time.ToString("yyyy-MM-dd HH:mm:ss");
}

/// <summary>
/// 用于单个版面页面（Board）的简单帖子
/// </summary>
public class SimplePost
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("userName")]
    public required string UserName { get; set; }
    [JsonPropertyName("replyCount")]
    public required string ReplyCount { get; set; }
    [JsonPropertyName("hitCount")]
    public required string HitCount { get; set; }
    [JsonPropertyName("time")]
    public required DateTime Time { get; set; }
    public string FormattedTime => Time.ToString("yyyy-MM-dd HH:mm:ss");
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
    [JsonPropertyName("userName")]
    public string UserName
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
    [JsonPropertyName("id")]
    public int Id { get; set;  }

    [JsonPropertyName("portraitUrl")]
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
    public string LastLogOnTime
    {
        get => field??string.Empty;
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

    public string RegisterTime
    {
        get => field??string.Empty;
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
    public int UserId
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
    
    public required DateTime Time
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(FormattedTime));
            }
        }
    }

    public string FormattedTime => Time.ToString("yyyy-MM-dd HH:mm:ss");
}

public class MediaContent
{
    [JsonPropertyName("thumbnail")]
    public List<string> Thumbnail {  get; set; }= new List<string>();

}
