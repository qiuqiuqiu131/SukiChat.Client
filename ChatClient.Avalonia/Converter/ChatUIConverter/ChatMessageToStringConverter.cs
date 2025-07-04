using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using ChatClient.Tool.Data.ChatMessage;
using ChatServer.Common.Protobuf;

namespace ChatClient.Avalonia.Converter.ChatUIConverter;

public class ChatMessageToStringConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<ChatMessageDto> chatMessages)
        {
            StringBuilder sb = new();
            foreach (var chatMessage in chatMessages)
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
                    case ChatMessage.ContentOneofCase.SystemMessage:
                        SystemMessDto systemMessDto = (SystemMessDto)chatMessage.Content;
                        foreach (var block in systemMessDto.Blocks)
                        {
                            sb.Append(block.Text);
                            sb.Append(" ");
                        }

                        break;
                    case ChatMessage.ContentOneofCase.CardMess:
                        sb.Append($"[{((CardMessDto)chatMessage.Content).Title}]");
                        break;
                    case ChatMessage.ContentOneofCase.VoiceMess:
                        sb.Append("[语音]");
                        break;
                    case ChatMessage.ContentOneofCase.CallMess:
                        CallMessDto callMessDto = (CallMessDto)chatMessage.Content;
                        sb.Append("[");
                        sb.Append(callMessDto.IsTelephone ? "语音" : "视频");
                        sb.Append("通话] ");
                        sb.Append(((CallMessDto)chatMessage.Content).Message);
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

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}