using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CC98.Objects;

/// <summary>
///     关注的人和被关注的人的详细信息，用于好友列表
/// </summary>
public partial class Friend : ObservableObject
{
    [JsonPropertyName("fanCount")]
    [ObservableProperty]
    public partial int FanCount { get; set; }


    [JsonPropertyName("id")]
    [ObservableProperty]
    public partial int Id { get; set; }

    [JsonPropertyName("postCount")]
    [ObservableProperty]
    public partial int PostCount { get; set; }

    [JsonPropertyName("name")]
    [ObservableProperty]
    public partial string Name { get; set; }


    [JsonPropertyName("portraitUrl")]
    [ObservableProperty]
    public partial string PortraitUrl { get; set; }
}