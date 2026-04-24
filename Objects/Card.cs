using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CC98.Objects;

public partial class Card : ObservableObject
{
    private bool _isFlipped = false;

    [JsonPropertyName("imageUri")]
    public string ImageUri
    {
        get => field ?? string.Empty;
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
        get => field ?? string.Empty;
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
