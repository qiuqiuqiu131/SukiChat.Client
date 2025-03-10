using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using ChatClient.Desktop.UIEntity;

namespace ChatClient.Avalonia.Converter;

public class SexConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Sex sex)
        {
            return sex switch
            {
                Sex.Female => "女",
                Sex.Male => "男",
            };
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}