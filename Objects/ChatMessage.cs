using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CC98.Objects;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty] public partial DateTime Time { get; set; }

    public string Content
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("id")]
    [ObservableProperty]
    public partial int MessageId { get; set; }

    [JsonPropertyName("receiverId")]
    [ObservableProperty]
    public partial int ReceiverId { get; set; }

    [JsonIgnore] [ObservableProperty] public partial bool IsMe { get; set; }
}