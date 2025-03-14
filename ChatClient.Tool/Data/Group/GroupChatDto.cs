using System.Collections.Specialized;
using Avalonia.Collections;
using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.Data.Group;

public class GroupChatDto : BindableBase
{
    public string? GroupId { get; set; }

    private GroupRelationDto? _groupRelationDto;

    public GroupRelationDto? GroupRelationDto
    {
        get => _groupRelationDto;
        set => SetProperty(ref _groupRelationDto, value);
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
        set => SetProperty(ref unReadMessageCount, value);
    }

    public bool IsSelected { get; set; }

    // 用户输入的消息
    public AvaloniaList<object> InputMessages { get; set; } = new();

    public event Action<GroupChatDto> OnLastChatMessagesChanged;

    public GroupChatDto()
    {
        ChatMessages.CollectionChanged += ChatMessagesOnCollectionChanged;
    }

    private void ChatMessagesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateChatMessages();

        if (IsSelected) return;

        // 新添加的消息
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex != 0)
        {
            foreach (var newItem in e.NewItems)
            {
                var chatData = (GroupChatData)newItem;
                if (!chatData.IsUser)
                    UnReadMessageCount++;
            }
        }
    }

    public void UpdateChatMessages()
    {
        LastChatMessages = ChatMessages?.LastOrDefault(predicate: d => !d.IsWriting);
    }
}