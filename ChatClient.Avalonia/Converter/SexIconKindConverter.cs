using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ChatClient.Desktop.UIEntity;
using Material.Icons;
using Material.Icons.Avalonia;

namespace ChatClient.Avalonia.Converter;

public class SexIconKindConverter : MarkupExtension, IValueConverter
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
                Sex.Female => new MaterialIcon()
                {
                    Width = 15, Height = 15, Foreground = new SolidColorBrush { Color = Colors.HotPink },
                    Kind = MaterialIconKind.GenderFemale
                },
                Sex.Male => new MaterialIcon()
                {
                    Width = 15, Height = 15, Foreground = new SolidColorBrush { Color = Colors.DodgerBlue },
                    Kind = MaterialIconKind.GenderMale
                },
            };
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}