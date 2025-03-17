using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ChatClient.Avalonia.Converter;

public class ListEmptyConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IList list)
        {
            return list.Count == 0;
        }

        if (value is int i)
        {
            return i == 0;
        }

        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}