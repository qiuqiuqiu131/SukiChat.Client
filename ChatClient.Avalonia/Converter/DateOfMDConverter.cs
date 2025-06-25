using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ChatClient.Avalonia.Converter;

public class DateOfMDConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly dateTime)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(dateTime.Month);
            stringBuilder.Append("月");
            stringBuilder.Append(dateTime.Day);
            stringBuilder.Append("日");
            return stringBuilder.ToString();
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}