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

    private string? phoneNumber;

    public string? PhoneNumber
    {
        get => phoneNumber;
        set
        {
            if (SetProperty(ref phoneNumber, value))
                OnPhoneNumberChanged?.Invoke();
        }
    }

    public string? PhoneNumberWithoutEvent
    {
        get => phoneNumber;
        set
        {
            SetProperty(ref phoneNumber, value);
            RaisePropertyChanged(nameof(PhoneNumber));
        }
    }


    private string? emailNumber;

    public string? EmailNumber
    {
        get => emailNumber;
        set
        {
            if (SetProperty(ref emailNumber, value))
                OnEmailNumberChanged?.Invoke();
        }
    }

    public string? EmailNumberWithoutEvent
    {
        get => emailNumber;
        set
        {
            SetProperty(ref emailNumber, value);
            RaisePropertyChanged(nameof(EmailNumber));
        }
    }

    private bool isFirstLogin;

    public bool IsFirstLogin
    {
        get => isFirstLogin;
        set => SetProperty(ref isFirstLogin, value);
    }

    public event Action OnUnreadMessageCountChanged;

    public event Action OnPhoneNumberChanged;
    public event Action OnEmailNumberChanged;

    public void Dispose()
    {
        UserDto.Dispose();
        OnUnreadMessageCountChanged = null;
    }
}