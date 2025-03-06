using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ChatClient.Avalonia.Converter;

public class FileExtensionToIconConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string extension || string.IsNullOrWhiteSpace(extension))
            return null;
        switch (extension)
        {
            case ".txt":
                return "FileDocument";
            case ".pdf":
                return "PdfFileIcon";
            case ".doc":
            case ".docx":
                return "FileWord";
            case ".xls":
            case ".xlsx":
                return "FileExcel";
            case ".ppt":
            case ".pptx":
                return "FilePowerpoint";
            case ".zip":
            case ".rar":
                return "ZipBox";
            default:
                return "File";
        }
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