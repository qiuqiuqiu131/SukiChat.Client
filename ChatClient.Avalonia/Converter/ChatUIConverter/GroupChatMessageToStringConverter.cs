using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;
using Microsoft.Extensions.Primitives;

namespace ChatClient.Avalonia.Converter;

public class GroupChatMessageToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GroupChatData groupChatData)
        {
            StringBuilder sb = new();
            sb.Append(groupChatData.Owner?.NickName ?? "  ");
            sb.Append(": ");
            foreach (var chatMessage in groupChatData.ChatMessages)
            {
                switch (chatMessage.Type)
                {
                    case ChatMessage.ContentOneofCase.TextMess:
                        TextMessDto textMess = (TextMessDto)chatMessage.Content;
                        sb.Append(textMess.Text);
                        break;
                    case ChatMessage.ContentOneofCase.ImageMess:
                        sb.Append("[图片]");
                        break;
                    case ChatMessage.ContentOneofCase.FileMess:
                        sb.Append("[文件]");
                        break;
                }
            }

            return sb.ToString();
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}