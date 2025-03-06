using System.Collections.ObjectModel;

namespace ChatClient.Tool.Data;

public class ChatData : BindableBase
{
    // 是否显示时间条
    private bool _showTime = false;

    public bool ShowTime
    {
        get => _showTime;
        set => SetProperty(ref _showTime, value);
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

    private bool _isReaded;

    public bool IsReaded
    {
        get => _isReaded;
        set => SetProperty(ref _isReaded, value);
    }


    private List<ChatMessageDto> _chatMessages = new();

    public List<ChatMessageDto> ChatMessages
    {
        get => _chatMessages;
        set => SetProperty(ref _chatMessages, value);
    }

    public void Clear()
    {
        ChatMessages = new List<ChatMessageDto> { ChatMessages.Last() };
    }
}