namespace ChatClient.Tool.Data.Friend;

public class FriendRelationDto : BindableBase, IDisposable
{
    public string Id { get; set; }

    public DateTime GroupTime { get; set; }

    private UserDto? _userDto;

    public UserDto? UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    public string grouping = string.Empty;

    public string GroupingWithoutEvent
    {
        get => grouping;
        set
        {
            if (SetProperty(ref grouping, value))
                RaisePropertyChanged(nameof(Grouping));
        }
    }

    public string Grouping
    {
        get => grouping;
        set
        {
            var origion = grouping;
            if (SetProperty(ref grouping, value))
            {
                OnFriendRelationChanged?.Invoke(this);
                OnGroupingChanged?.Invoke(this, origion);
            }
        }
    }

    private string? _remark;

    public string? Remark
    {
        get => _remark;
        set
        {
            if (SetProperty(ref _remark, value))
            {
                OnFriendRelationChanged?.Invoke(this);
            }
        }
    }

    private bool _cantDisturb;

    public bool CantDisturb
    {
        get => _cantDisturb;
        set
        {
            if (SetProperty(ref _cantDisturb, value))
            {
                OnFriendRelationChanged?.Invoke(this);
            }
        }
    }

    private bool _isTop;

    public bool IsTop
    {
        get => _isTop;
        set
        {
            if (SetProperty(ref _isTop, value))
            {
                OnFriendRelationChanged?.Invoke(this);
            }
        }
    }

    private bool _isChatting;

    public bool IsChatting
    {
        get => _isChatting;
        set
        {
            if (SetProperty(ref _isChatting, value))
            {
                OnFriendRelationChanged?.Invoke(this);
            }
        }
    }

    private int _lastChatId;

    public int LastChatId
    {
        get => _lastChatId;
        set => SetProperty(ref _lastChatId, value);
    }

    public event Action<FriendRelationDto> OnFriendRelationChanged;
    public event Action<FriendRelationDto, string> OnGroupingChanged;

    public void Dispose()
    {
        _userDto = null;
    }
}