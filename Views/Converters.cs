using CC98.Kernel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using Windows.Storage;

namespace CC98.Services.Converters;

public partial class UBBTextConverter : IValueConverter
{
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
    {
        if (value != null)
        {
            string input = value as string ?? string.Empty;
            if (!string.IsNullOrEmpty(input))
            {
                return UBBConverter.Convert(input, ValidationHelper.GetValue(Set, "IsImageVisible") == "1");
            }
            else
            {
                return string.Empty;
            }

        }
        else
        {
            return string.Empty;
        }
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
public partial class AlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ?
            HorizontalAlignment.Right :
            HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}