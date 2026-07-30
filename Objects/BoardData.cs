using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CC98.Objects;

/// <summary>
///     版面信息
/// </summary>
public partial class BoardData : ObservableObject
{
    private string _bigPaper = string.Empty;
    private List<string> _boardMasters = [];
    private string _description = string.Empty;
    private int _id;
    private string _name = string.Empty;
    private int _todayCount;
    private int _topicCount;

    [JsonPropertyName("boardMasters")]
    public List<string> BoardMasters
    {
        get => _boardMasters;
        set
        {
            if (SetProperty(ref _boardMasters, value)) OnPropertyChanged(nameof(BoardMastersString));
        }
    }

    [JsonPropertyName("id")]
    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    [JsonPropertyName("description")]
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    [JsonPropertyName("bigPaper")]
    public string BigPaper
    {
        get => _bigPaper;
        set => SetProperty(ref _bigPaper, value);
    }

    [JsonPropertyName("todayCount")]
    public int TodayCount
    {
        get => _todayCount;
        set
        {
            if (SetProperty(ref _todayCount, value)) OnPropertyChanged(nameof(TodayCountString));
        }
    }

    [JsonPropertyName("topicCount")]
    public int TopicCount
    {
        get => _topicCount;
        set
        {
            if (SetProperty(ref _topicCount, value)) OnPropertyChanged(nameof(TopicCountString));
        }
    }

    // 计算属性
    public string BoardMastersString => $"版主:{string.Join(',', BoardMasters)}";
    public string TodayCountString => $"今日帖数：{TodayCount}";
    public string TopicCountString => $"总话题数：{TopicCount}";
}