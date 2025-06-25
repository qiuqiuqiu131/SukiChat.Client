using System.Text;
using ChatClient.Tool.Data.ChatMessage;
using ChatServer.Common.Protobuf;
using Google.Protobuf.Collections;

namespace ChatClient.Tool.Tools;

public static class ChatMessageTool
{
    /// <summary>
    /// 加密聊天消息
    /// </summary>
    /// <param name="chatMessages"></param>
    /// <returns></returns>
    public static string EncruptChatMessage(RepeatedField<ChatMessage> chatMessages)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var chatMessage in chatMessages)
        {
            switch (chatMessage.ContentCase)
            {
                case ChatMessage.ContentOneofCase.None: break;
                case ChatMessage.ContentOneofCase.TextMess:
                    stringBuilder.Append((int)chatMessage.ContentCase);
                    stringBuilder.Append(chatMessage.TextMess.Text);
                    stringBuilder.Append("1\n\t3\n\t1\n\t");
                    break;
                case ChatMessage.ContentOneofCase.ImageMess:
                    stringBuilder.Append((int)chatMessage.ContentCase);
                    stringBuilder.Append(chatMessage.ImageMess.FilePath);
                    stringBuilder.Append("_____");
                    stringBuilder.Append(chatMessage.ImageMess.FileSize);
                    stringBuilder.Append("1\n\t3\n\t1\n\t");
                    break;
                case ChatMessage.ContentOneofCase.VoiceMess:
                    stringBuilder.Append((int)chatMessage.ContentCase);
                    stringBuilder.Append(chatMessage.VoiceMess.FilePath);
                    stringBuilder.Append("_____");
                    stringBuilder.Append(chatMessage.VoiceMess.FileSize);
                    stringBuilder.Append("1\n\t3\n\t1\n\t");
                    break;
                case ChatMessage.ContentOneofCase.FileMess:
                    stringBuilder.Append((int)chatMessage.ContentCase);
                    stringBuilder.Append(chatMessage.FileMess.FileName);
                    stringBuilder.Append("_____");
                    stringBuilder.Append(chatMessage.FileMess.FileSize);
                    stringBuilder.Append("_____");
                    stringBuilder.Append(chatMessage.FileMess.FileType);
                    stringBuilder.Append("1\n\t3\n\t1\n\t");
                    break;
                case ChatMessage.ContentOneofCase.SystemMessage:
                    stringBuilder.Append((int)chatMessage.ContentCase);
                    foreach (var systemBlock in chatMessage.SystemMessage.Blocks)
                    {
                        stringBuilder.Append(systemBlock.Text);
                        stringBuilder.Append("_____");
                        stringBuilder.Append(systemBlock.Bold ? "1" : "0");
                        stringBuilder.Append("5\n\t7\n\t5\n\t");
                    }

                    break;
                case ChatMessage.ContentOneofCase.CardMess:
                    stringBuilder.Append((int)chatMessage.ContentCase);
                    stringBuilder.Append(chatMessage.CardMess.IsUser ? "1" : "0");
                    stringBuilder.Append("_____");
                    stringBuilder.Append(chatMessage.CardMess.Id);
                    stringBuilder.Append("1\n\t3\n\t1\n\t");
                    break;
                case ChatMessage.ContentOneofCase.CallMess:
                    stringBuilder.Append((int)chatMessage.ContentCase);
                    stringBuilder.Append(chatMessage.CallMess.Failed ? "1" : "0");
                    stringBuilder.Append("_____");
                    stringBuilder.Append(chatMessage.CallMess.IsTelephone ? "1" : "0");
                    stringBuilder.Append("_____");
                    stringBuilder.Append(chatMessage.CallMess.CallTime.ToString());
                    stringBuilder.Append("1\n\t3\n\t1\n\t");
                    break;
            }
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 解密聊天消息
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static RepeatedField<ChatMessage> DecruptChatMessage(string message)
    {
        // 分割消息并过滤空字符串
        List<string> messages = message.Split("1\n\t3\n\t1\n\t")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        RepeatedField<ChatMessage> chatMessages = new RepeatedField<ChatMessage>();

        foreach (var item in messages)
        {
            if (string.IsNullOrEmpty(item)) continue;

            // 获取第一个字符作为类型
            int type = (int)char.GetNumericValue(item[0]);
            string content = item.Substring(1);

            switch ((ChatMessage.ContentOneofCase)type)
            {
                case ChatMessage.ContentOneofCase.TextMess:
                    var textMess = new ChatMessage
                    {
                        TextMess = new TextMess { Text = content }
                    };
                    chatMessages.Add(textMess);
                    break;
                case ChatMessage.ContentOneofCase.ImageMess:
                    string[] image_spliter = content.Split("_____");
                    var imageMess = new ChatMessage
                    {
                        ImageMess = new ImageMess
                        {
                            FilePath = image_spliter[0],
                            FileSize = int.Parse(image_spliter[1])
                        }
                    };
                    chatMessages.Add(imageMess);
                    break;
                case ChatMessage.ContentOneofCase.VoiceMess:
                    string[] voice_spliter = content.Split("_____");
                    var voiceMess = new ChatMessage
                    {
                        VoiceMess = new VoiceMess
                        {
                            FilePath = voice_spliter[0],
                            FileSize = int.Parse(voice_spliter[1])
                        }
                    };
                    chatMessages.Add(voiceMess);
                    break;
                case ChatMessage.ContentOneofCase.FileMess:
                    string[] file_spliter = content.Split("_____");
                    var fileMess = new ChatMessage
                    {
                        FileMess = new FileMess
                        {
                            FileName = file_spliter[0],
                            FileSize = int.Parse(file_spliter[1]),
                            FileType = file_spliter[2]
                        }
                    };
                    chatMessages.Add(fileMess);
                    break;
                case ChatMessage.ContentOneofCase.SystemMessage:
                    string[] system_spliter = content.Split("5\n\t7\n\t5\n\t");
                    SystemMessage systemMessage = new SystemMessage();
                    foreach (var system in system_spliter)
                    {
                        if (string.IsNullOrWhiteSpace(system)) continue;
                        string[] block_spliter = system.Split("_____");
                        systemMessage.Blocks.Add(new SystemMessageBlock
                        {
                            Text = block_spliter[0],
                            Bold = block_spliter[1].Equals("0") ? false : true
                        });
                    }

                    chatMessages.Add(new ChatMessage { SystemMessage = systemMessage });
                    break;
                case ChatMessage.ContentOneofCase.CardMess:
                    string[] card_spliter = content.Split("_____");
                    var cardMess = new ChatMessage
                    {
                        CardMess = new CardMess
                        {
                            IsUser = card_spliter[0].Equals("1"),
                            Id = card_spliter[1]
                        }
                    };
                    chatMessages.Add(cardMess);
                    break;
                case ChatMessage.ContentOneofCase.CallMess:
                    string[] call_spliter = content.Split("_____");
                    var callMess = new ChatMessage
                    {
                        CallMess = new CallMess
                        {
                            Failed = call_spliter[0].Equals("1"),
                            IsTelephone = call_spliter[1].Equals("1"),
                            CallTime = int.Parse(call_spliter[2])
                        }
                    };
                    chatMessages.Add(callMess);
                    break;
            }
        }

        return chatMessages;
    }

    public static List<ChatMessageDto> DecruptChatMessageDto(string message)
    {
        // 分割消息并过滤空字符串
        List<string> messages = message.Split("1\n\t3\n\t1\n\t")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        List<ChatMessageDto> chatMessages = new List<ChatMessageDto>();

        foreach (var item in messages)
        {
            if (string.IsNullOrEmpty(item)) continue;

            // 获取第一个字符作为类型
            int type = (int)char.GetNumericValue(item[0]);
            string content = item.Substring(1);

            switch ((ChatMessage.ContentOneofCase)type)
            {
                case ChatMessage.ContentOneofCase.TextMess:
                    var textMess = new ChatMessageDto
                    {
                        Type = (ChatMessage.ContentOneofCase)type,
                        Content = new TextMessDto
                        {
                            Text = content
                        }
                    };
                    chatMessages.Add(textMess);
                    break;
                case ChatMessage.ContentOneofCase.ImageMess:
                    string[] image_spliter = content.Split("_____");
                    var imageMess = new ChatMessageDto
                    {
                        Type = (ChatMessage.ContentOneofCase)type,
                        Content = new ImageMessDto
                        {
                            FilePath = image_spliter[0],
                            FileSize = int.Parse(image_spliter[1])
                        }
                    };
                    chatMessages.Add(imageMess);
                    break;
                case ChatMessage.ContentOneofCase.VoiceMess:
                    string[] voice_spliter = content.Split("_____");
                    var voiceMess = new ChatMessageDto
                    {
                        Type = (ChatMessage.ContentOneofCase)type,
                        Content = new VoiceMessDto
                        {
                            FilePath = voice_spliter[0],
                            FileSize = int.Parse(voice_spliter[1])
                        }
                    };
                    chatMessages.Add(voiceMess);
                    break;
                case ChatMessage.ContentOneofCase.FileMess:
                    string[] file_spliter = content.Split("_____");

                    var fileMess = new ChatMessageDto
                    {
                        Type = (ChatMessage.ContentOneofCase)type,
                        Content = new FileMessDto
                        {
                            FileName = file_spliter[0],
                            FileSize = int.Parse(file_spliter[1]),
                            FileType = file_spliter[2]
                        }
                    };
                    chatMessages.Add(fileMess);
                    break;
                case ChatMessage.ContentOneofCase.SystemMessage:
                    string[] system_spliter = content.Split("5\n\t7\n\t5\n\t");
                    SystemMessDto systemMessage = new SystemMessDto();
                    foreach (var system in system_spliter)
                    {
                        if (string.IsNullOrWhiteSpace(system)) continue;
                        string[] block_spliter = system.Split("_____");
                        systemMessage.Blocks.Add(new SystemMessBlockDto
                        {
                            Text = block_spliter[0],
                            Bold = block_spliter[1].Equals("0") ? false : true
                        });
                    }

                    chatMessages.Add(new ChatMessageDto
                    {
                        Content = systemMessage,
                        Type = (ChatMessage.ContentOneofCase)type
                    });
                    break;
                case ChatMessage.ContentOneofCase.CardMess:
                    string[] card_spliter = content.Split("_____");
                    var cardMess = new ChatMessageDto
                    {
                        Type = (ChatMessage.ContentOneofCase)type,
                        Content = new CardMessDto
                        {
                            IsUser = card_spliter[0].Equals("1"),
                            Id = card_spliter[1]
                        }
                    };
                    chatMessages.Add(cardMess);
                    break;
                case ChatMessage.ContentOneofCase.CallMess:
                    string[] call_spliter = content.Split("_____");
                    var callMess = new ChatMessageDto
                    {
                        Type = (ChatMessage.ContentOneofCase)type,
                        Content = new CallMessDto
                        {
                            Failed = call_spliter[0].Equals("1"),
                            IsTelephone = call_spliter[1].Equals("1"),
                            CallTime = int.Parse(call_spliter[2])
                        }
                    };
                    chatMessages.Add(callMess);
                    break;
            }
        }

        return chatMessages;
    }
}