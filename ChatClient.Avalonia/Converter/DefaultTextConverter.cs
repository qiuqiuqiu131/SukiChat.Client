using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ChatClient.Avalonia.Converter;

public class DefaultTextConverter : MarkupExtension, IValueConverter
{
    public string DefaultText { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return DefaultText;
            else
                return text;
        }

        return DefaultText;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}