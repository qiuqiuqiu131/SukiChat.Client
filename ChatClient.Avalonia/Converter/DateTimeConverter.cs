using System.Globalization;
using Avalonia.Data.Converters;

namespace ChatClient.Avalonia.Converter;

public class DateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime time)
        {
            if (time.Date == DateTime.Today)
            {
                return time.ToString("tt h:mm");
            }
            else if (time.Date == DateTime.Today.AddDays(-1))
            {
                return "昨天";
            }
            else if (time.Date >= DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek))
            {
                return culture.DateTimeFormat.GetDayName(time.DayOfWeek);
            }
            else if (time.Year == DateTime.Today.Year)
            {
                return time.ToString("M月d日");
            }
            else
            {
                return time.ToString("yyyy年M月d日");
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}