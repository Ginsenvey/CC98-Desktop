using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CC98.Objects;

public class SectionCard
{
    public required string SectionName { get; set; }
    public required string HexColor { get; set; }
    public List<IndexTopic> IndexTopics { get; set; } = [];
}

public partial class IndexTopic : ObservableObject
{
    public string Title
    {
        get => field?.Trim() ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    // 只有热门话题此项不为 null
    public string? BoardName
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    [ObservableProperty] public partial int Id { get; set; }

    [JsonIgnore] [ObservableProperty] public partial bool IsHotTopic { get; set; }
}