using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia.Markup.Xaml;

namespace ChatClient.Avalonia.Converter;

public class EqualityConverter : MarkupExtension, IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return false;

        return values[0] != null && values[0].Equals(values[1]);
    }

    public object ConvertBack(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}