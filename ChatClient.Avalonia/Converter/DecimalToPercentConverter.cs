using AutoMapper;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ChatClient.Avalonia.Converter;

public class DecimalToPercentConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return $"{decimalValue * 100:N0}%";
        }
        else if (value is double doubleValue)
        {
            return $"{doubleValue * 100:N0}%";
        }
        else if (value is float floatValue)
        {
            return $"{floatValue * 100:N0}%";
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}