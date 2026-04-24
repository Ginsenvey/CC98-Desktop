using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace CC98.Objects;

public partial class Reply : ObservableObject
{
    public string UserName
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }

    [JsonIgnore]
    public string PortraitUrl
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }

    [ObservableProperty]
    public partial int? UserId { get; set; }

    public string Content
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }

    [ObservableProperty]
    public partial int LikeCount { get; set; }

    [ObservableProperty]
    public partial int DislikeCount { get; set; }

    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial int LikeState { get; set; }

    [ObservableProperty]
    public partial int Floor { get; set; }

    [ObservableProperty]
    public partial bool IsMe { get; set; }

    [ObservableProperty]
    public partial int ContentType { get; set; }

    [ObservableProperty]
    public partial bool IsDeleted { get; set; }

    [ObservableProperty]
    public partial bool IsAnonymous { get; set; }

    [ObservableProperty]
    public partial DateTime Time { get; set; }
}
