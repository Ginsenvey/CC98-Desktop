using CC98.Objects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CC98.Services;

public partial class NavigationTemplateSelector : DataTemplateSelector
{
    public DataTemplate GroupTemplate { get; set; }
    public DataTemplate PinnedItemTemplate { get; set; }
    public DataTemplate ItemTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is NavigationItem n)
        {
            if (n.IsEditable == true)
            {
                return PinnedItemTemplate;
            }
            else
            {
                return ItemTemplate;
            }
        }
        else
        {
            return GroupTemplate;
        }
    }
}

public partial class NoticeTemplateSelector : DataTemplateSelector
{
    public DataTemplate SystemTemplate { get; set; }
    public DataTemplate ReplyOrAtTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is Notice n)
        {
            if (n.Type == (int)NoticeType.System)
            {
                return SystemTemplate;
            }
        }
        return ReplyOrAtTemplate;
    }
}