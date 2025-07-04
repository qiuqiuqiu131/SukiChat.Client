using System.Collections.Specialized;
using Avalonia.Collections;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Tool.Data.Group;

public class GroupChatDto : BindableBase, IDisposable
{
    public string GroupId { get; set; }

    private GroupRelationDto? _groupRelationDto;

    public GroupRelationDto? GroupRelationDto
    {
        get => _groupRelationDto;
        set
        {
            SetProperty(ref _groupRelationDto, value);
            if (_groupRelationDto != null)
                _groupRelationDto.OnGroupRelationChanged += delegate { OnLastChatMessagesChanged?.Invoke(this); };
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
    private AvaloniaList<GroupChatData> _chatMessages = new();

    public AvaloniaList<GroupChatData> ChatMessages
    {
        get => _chatMessages;
        set => SetProperty(ref _chatMessages, value);
    }

    // 最后一条聊天消息
    private GroupChatData? _lastChatMessages;

    public GroupChatData? LastChatMessages
    {
        get => _lastChatMessages;
        set
        {
            if (SetProperty(ref _lastChatMessages, value))
                OnLastChatMessagesChanged?.Invoke(this);
        }
    }

    // 未读的聊天消息
    private int unReadMessageCount;

    public int UnReadMessageCount
    {
        get => unReadMessageCount;
        set
        {
            if (SetProperty(ref unReadMessageCount, value))
            {
                OnUnReadMessageCountChanged?.Invoke();
            }
        }
    }

    public bool IsSelected { get; set; }

    // 用户输入的消息
    public AvaloniaList<object> InputMessages { get; set; } = new();

    public event Action<GroupChatDto> OnLastChatMessagesChanged;
    public event Action OnUnReadMessageCountChanged;

    public GroupChatDto()
    {
        ChatMessages.CollectionChanged += ChatMessagesOnCollectionChanged;
    }

    private void ChatMessagesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateChatMessages();
    }

    public void UpdateChatMessages()
    {
        LastChatMessages = ChatMessages?.LastOrDefault(predicate: d => !d.IsWriting);
    }

    public void Dispose()
    {
        OnLastChatMessagesChanged = null;
        OnUnReadMessageCountChanged = null;

        InputMessages.Clear();
        InputMessages = null;

        ChatMessages.CollectionChanged -= ChatMessagesOnCollectionChanged;
        ChatMessages.Clear();
        ChatMessages = null;

        _groupRelationDto = null;
        _lastChatMessages = null;
    }
}