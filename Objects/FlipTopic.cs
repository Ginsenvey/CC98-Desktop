using CommunityToolkit.Mvvm.ComponentModel;

namespace CC98.Objects;

public partial class FlipTopic : ObservableObject
{
    public string Title
    {
        get => field?.Trim() ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    public string Url
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    public string Time
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    public string Content
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }
}