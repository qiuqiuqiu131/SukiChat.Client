using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ChatClient.Avalonia.Converter.ChatUIConverter;

public class FileSizeConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || !(value is int || value is long))
        {
            return "Invalid size";
        }

        long sizeInBits = (long)value;
        double sizeInBytes = sizeInBits;

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;

        while (sizeInBytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            sizeInBytes /= 1024;
        }

        return string.Format("{0:0.##} {1}", sizeInBytes, sizes[order]);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}