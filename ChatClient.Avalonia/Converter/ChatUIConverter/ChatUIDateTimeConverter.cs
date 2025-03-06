using System.Globalization;
using Avalonia.Data.Converters;

namespace ChatClient.Avalonia.Converter;

public class ChatUIDateTimeConverter : IValueConverter
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
                return "昨天 " + time.ToString("hh:mm");
            }
            else if (time.Date >= DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek))
            {
                return culture.DateTimeFormat.GetDayName(time.DayOfWeek) + " " + time.ToString("hh:mm");
            }
            else if (time.Year == DateTime.Today.Year)
            {
                return time.ToString("MM/dd hh:mm");
            }
            else
            {
                return time.ToString("yyyy/M/d/ hh:mm");
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}