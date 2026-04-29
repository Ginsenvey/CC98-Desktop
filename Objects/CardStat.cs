using CommunityToolkit.Mvvm.ComponentModel;

namespace CC98.Objects;

/// <summary>
///     抽卡数据
/// </summary>
public partial class CardStat : ObservableObject
{
    [ObservableProperty] public partial int Wealth { get; set; }

    [ObservableProperty] public partial int DrawCount { get; set; }

    [ObservableProperty] public partial int TotalCost { get; set; }

    [ObservableProperty] public partial int TotalBonus { get; set; }

    [ObservableProperty] public partial int CardCount { get; set; }
}