namespace ChatClient.Tool.Data;

public class UserDetailDto : BindableBase, IDisposable
{
    public string Id { get; set; }

    public UserDto UserDto { get; set; }

    public string? Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    private string? password;

    private DateTime lastReadFriendMessageTime;

    public DateTime LastReadFriendMessageTime
    {
        get => lastReadFriendMessageTime;
        set => SetProperty(ref lastReadFriendMessageTime, value);
    }

    private DateTime lastReadGroupMessageTime;

    public DateTime LastReadGroupMessageTime
    {
        get => lastReadGroupMessageTime;
        set => SetProperty(ref lastReadGroupMessageTime, value);
    }

    private DateTime lastDeleteFriendMessageTime;

    public DateTime LastDeleteFriendMessageTime
    {
        get => lastDeleteFriendMessageTime;
        set => SetProperty(ref lastDeleteFriendMessageTime, value);
    }

    private DateTime lastDeleteGroupMessageTime;

    public DateTime LastDeleteGroupMessageTime
    {
        get => lastDeleteGroupMessageTime;
        set => SetProperty(ref lastDeleteGroupMessageTime, value);
    }

    private int unreadFriendMessageCount;

    public int UnreadFriendMessageCount
    {
        get => unreadFriendMessageCount;
        set
        {
            if (SetProperty(ref unreadFriendMessageCount, value))
            {
                OnUnreadMessageCountChanged?.Invoke();
            }
        }
    }

    private int unreadGroupMessageCount;

    public int UnreadGroupMessageCount
    {
        get => unreadGroupMessageCount;
        set
        {
            if (SetProperty(ref unreadGroupMessageCount, value))
            {
                OnUnreadMessageCountChanged?.Invoke();
            }
        }
    }

    public event Action OnUnreadMessageCountChanged;

    public void Dispose()
    {
        UserDto.Dispose();
        OnUnreadMessageCountChanged = null;
    }
}