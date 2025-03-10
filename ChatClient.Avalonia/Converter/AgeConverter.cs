using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ChatClient.Avalonia.Converter;

public class AgeConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly dateTime)
        {
            var age = DateTime.Now.Year - dateTime.Year;
            if (DateTime.Now.Month < dateTime.Month ||
                DateTime.Now.Month == dateTime.Month && DateTime.Now.Day < dateTime.Day)
            {
                age--;
            }

            return age;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}