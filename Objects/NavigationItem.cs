using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;

namespace CC98.Objects;

public partial class CategoryBase : ObservableObject { }

public partial class NavigationItem : CategoryBase
{
    public string Name
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }

    [ObservableProperty]
    public partial Symbol IconSymbol { get; set; }

    public string Tag
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }

    [ObservableProperty]
    public partial bool IsEditable { get; set; }
}

public partial class NavigationGroup : CategoryBase
{
    public string Name
    {
        get => field ?? "";
        set => SetProperty(ref field, value);
    }

    [ObservableProperty]
    public partial bool IsEditable { get; set; }
}
