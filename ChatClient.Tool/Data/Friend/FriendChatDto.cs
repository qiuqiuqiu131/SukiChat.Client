using System.Collections.ObjectModel;
using Avalonia.Collections;

namespace ChatClient.Tool.Data;

public class FriendChatDto : BindableBase
{
    public string UserId { get; set; }

    // 用户信息
    private FriendRelationDto? _friendRelationDto;

    public FriendRelationDto? FriendRelatoinDto
    {
        get => _friendRelationDto;
        set
        {
            if (SetProperty(ref _friendRelationDto, value))
            {
                if (FriendRelatoinDto != null && FriendRelatoinDto.UserDto != null)
                    FriendRelatoinDto.UserDto.OnUserOnlineChanged += () =>
                    {
                        if (FriendRelatoinDto?.UserDto.IsOnline == false)
                            IsWriting = false;
                    };
            }
        }
    }

    // 是否存在更多历史聊天记录
    private bool _hasMoreMessage = true;

    public bool HasMoreMessage
    {
        get => _hasMoreMessage;
        set => SetProperty(ref _hasMoreMessage, value);
    }

    // 聊天消息记录
    private AvaloniaList<ChatData> _chatMessages = new();

    public AvaloniaList<ChatData> ChatMessages
    {
        get => _chatMessages;
        set =>
            SetProperty(ref _chatMessages, value);
    }

    // 最后一条聊天消息
    private ChatData? _lastChatMessages;

    public ChatData? LastChatMessages
    {
        get => _lastChatMessages;
        set
        {
            if (SetProperty(ref _lastChatMessages, value))
                OnLastChatMessagesChanged?.Invoke(this);
        }
    }

    // 用户正在输入
    private bool _isWriting;

    public bool IsWriting
    {
        get => _isWriting;
        set => SetProperty(ref _isWriting, value);
    }

    // 未读的聊天消息
    private int unReadMessageCount;

    public int UnReadMessageCount
    {
        get => unReadMessageCount;
        set => SetProperty(ref unReadMessageCount, value);
    }

    // 用户输入的消息
    public AvaloniaList<object> InputMessages { get; set; } = new();

    public event Action<FriendChatDto> OnLastChatMessagesChanged;

    public FriendChatDto()
    {
        ChatMessages.CollectionChanged += delegate { UpdateChatMessages(); };
    }

    public void UpdateChatMessages()
    {
        LastChatMessages = ChatMessages?.LastOrDefault(predicate: d => !d.IsWriting);
    }
}