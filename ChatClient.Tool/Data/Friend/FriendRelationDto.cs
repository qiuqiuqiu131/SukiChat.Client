namespace ChatClient.Tool.Data;

public class FriendRelationDto : BindableBase
{
    public string Id { get; set; }

    public DateTime GroupTime { get; set; }

    private UserDto? _userDto;

    public UserDto? UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    private string grouping;

    public string Grouping
    {
        get => grouping;
        set
        {
            if (SetProperty(ref grouping, value))
            {
                OnFriendRelationChanged?.Invoke();
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
                OnFriendRelationChanged?.Invoke();
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
                OnFriendRelationChanged?.Invoke();
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
                OnFriendRelationChanged?.Invoke();
            }
        }
    }

    private int _lastChatId;

    public int LastChatId
    {
        get => _lastChatId;
        set => SetProperty(ref _lastChatId, value);
    }

    public event Action OnFriendRelationChanged;
}