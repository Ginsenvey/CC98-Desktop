using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;

namespace CC98.Objects;

/// <summary>
///     用于单个版面页和个人主页的简单展示帖
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

    [ObservableProperty] public partial int Id { get; set; }

    [ObservableProperty] public partial int BoardId { get; set; }

    [JsonIgnore] [ObservableProperty] public partial int SortId { get; set; }

    public string BoardName
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    [ObservableProperty] public partial int HitCount { get; set; }

    [ObservableProperty] public partial int ReplyCount { get; set; }

    [ObservableProperty] public partial DateTime Time { get; set; }

    [ObservableProperty] public partial Symbol Symbol { get; set; }
}