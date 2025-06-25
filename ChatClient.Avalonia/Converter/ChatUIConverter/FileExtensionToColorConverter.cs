using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ChatClient.Avalonia.Converter.ChatUIConverter;

public class FileExtensionToColorConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string extension || string.IsNullOrWhiteSpace(extension))
            return null;
        switch (extension)
        {
            case ".txt":
                return new SolidColorBrush(Colors.Gray);
            case ".pdf":
                return new SolidColorBrush(Colors.Red);
            case ".doc":
            case ".docx":
                return new SolidColorBrush(Colors.RoyalBlue);
            case ".xls":
            case ".xlsx":
            case ".csv":
                return new SolidColorBrush(Colors.Green);
            case ".ppt":
            case ".pptx":
                return new SolidColorBrush(Colors.OrangeRed);
            case ".zip":
            case ".rar":
                return new SolidColorBrush(Colors.Gray);
            default:
                return new SolidColorBrush(Colors.Gray);
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}