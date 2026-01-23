using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CC98.Kernel;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace CC98.Objects;
/// <summary>
/// 收藏夹
/// </summary>
public class Favorites
{
    //收藏夹名称
    [JsonProperty("name")]
    public required string Name { get; set; }
    //收藏夹ID
    [JsonProperty("id")]
    public required int Id { get; set; }
}

/// <summary>
/// 主题帖数据
/// </summary> 
public class  TopicMetaData
{
    [JsonProperty("title")]
    public required string Title { get; set; } = "";
    [JsonProperty("favoriteCount")]
    public required int FavoriteCount { get; set; } = 0;
    [JsonProperty("replyCount")]
    public required int ReplyCount { get; set; } = 0;
    [JsonProperty("hitCount")]
    public required int HitCount { get; set; } = 0;
    [JsonProperty("time")]
    public required DateTime Time{ get; set; }
    [JsonProperty("isVote")]
    public bool IsVote { get; set; }
    
    [JsonIgnore]
    public bool IsFavorite { get; set; } = false;

    //内置时间转换函数
    public string FormattedTime => Time.ToString("yyyy-MM-dd HH:mm:ss");
}

/// <summary>
/// 
public class RandomPost
{
    [JsonProperty("userId")]
    public required string UserId { get; set; }

    [JsonProperty("title")]
    public required string Title { get; set; }

    [JsonProperty("replyCount")]
    public required string ReplyCount { get; set; }

    [JsonProperty("hitCount")]
    public required string HitCount { get; set; }

    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("time")]
    public required DateTime Time { get; set; }

    public string FormattedTime => Time.ToString("yyyy-MM-dd HH:mm:ss");
}

/// <summary>
/// 用于单个版面页面的简单帖子
/// </summary>
public class SimplePost
{
    [JsonProperty("id")]
    public required string Id { get; set; }
    [JsonProperty("title")]
    public required string Title { get; set; }

    [JsonProperty("userName")]
    public required string UserName { get; set; }
    [JsonProperty("replyCount")]
    public required string ReplyCount { get; set; }
    [JsonProperty("hitCount")]
    public required string HitCount { get; set; }
    [JsonProperty("time")]
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

    [JsonProperty("boardMasters")]
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
 
    [JsonProperty("id")]
    public required int Id
    {
        get=> _id;
        set=> SetProperty(ref _id, value);
    }    
    
    [JsonProperty("name")]
    public required string Name
    {
        get=> _name;
        set => SetProperty(ref _name, value);
    }

    
    
    [JsonProperty("description")]
    public required string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

   
    
    [JsonProperty("bigPaper")]
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
    
    [JsonProperty("todayCount")]
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
   
    [JsonProperty("topicCount")]
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