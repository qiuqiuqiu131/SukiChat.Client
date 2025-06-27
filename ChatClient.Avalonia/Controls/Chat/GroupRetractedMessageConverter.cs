using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Avalonia.Controls.Chat;

public class GroupRetractedMessageConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GroupChatData groupChatData)
        {
            if (groupChatData.IsUser)
                return "你";
            else
                return groupChatData.Owner?.NickName ?? "某人";
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}