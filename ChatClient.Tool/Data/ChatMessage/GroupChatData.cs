using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.Data;

public class GroupChatData : BindableBase, IDisposable
{
    // 是否显示时间条
    private bool _showTime = false;

    public bool ShowTime
    {
        get => _showTime;
        set => SetProperty(ref _showTime, value);
    }

    private bool isSystem;

    public bool IsSystem
    {
        get => isSystem;
        set => SetProperty(ref isSystem, value);
    }

    private int _chatId;

    public int ChatId
    {
        get => _chatId;
        set => SetProperty(ref _chatId, value);
    }

    private DateTime _time;

    public DateTime Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }

    private bool _isUser;

    public bool IsUser
    {
        get => _isUser;
        set => SetProperty(ref _isUser, value);
    }

    private bool _isWriting;

    public bool IsWriting
    {
        get => _isWriting;
        set => SetProperty(ref _isWriting, value);
    }

    private bool _isError;

    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    // 消息发送者
    private GroupMemberDto? owner;

    public GroupMemberDto? Owner
    {
        get => owner;
        set => SetProperty(ref owner, value);
    }

    private List<ChatMessageDto> _chatMessages = new();

    public List<ChatMessageDto> ChatMessages
    {
        get => _chatMessages;
        set => SetProperty(ref _chatMessages, value);
    }

    public void Dispose()
    {
        owner = null;

        foreach (var chatMessage in ChatMessages)
            chatMessage.Dispose();
        _chatMessages.Clear();
    }
}