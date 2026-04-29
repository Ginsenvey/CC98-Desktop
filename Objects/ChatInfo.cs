using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CC98.Objects;

public partial class ChatInfo : ObservableObject
{
    public string LastContent
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    [ObservableProperty] public partial int UserId { get; set; }

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