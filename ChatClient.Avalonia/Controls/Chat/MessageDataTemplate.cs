using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Avalonia.Controls.Chat;

public class MessageDataTemplate : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new Dictionary<string, IDataTemplate>();

    public Control? Build(object? param)
    {
        if (param is ChatData chatData)
        {
            if (chatData.IsRetracted)
                return AvailableTemplates["Retracted"].Build(param);
            if (chatData.IsUser)
                return AvailableTemplates["User"].Build(param);
            return AvailableTemplates["Friend"].Build(param);
        }

        if (param is GroupChatData groupChatData)
        {
            if (groupChatData.IsSystem)
                return AvailableTemplates["System"].Build(param);
            if (groupChatData.IsRetracted)
                return AvailableTemplates["Retracted"].Build(param);
            if (groupChatData.IsUser)
                return AvailableTemplates["User"].Build(param);
            return AvailableTemplates["Member"].Build(param);
        }

        return null;
    }

    public bool Match(object? data)
    {
        if (data is ChatData || data is GroupChatData)
            return true;
        return false;
    }
}