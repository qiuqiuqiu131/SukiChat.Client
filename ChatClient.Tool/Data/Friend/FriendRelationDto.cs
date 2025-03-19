namespace ChatClient.Tool.Data;

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

    private string grouping;

    public string Grouping
    {
        get => grouping;
        set
        {
            if (SetProperty(ref grouping, value))
            {
                OnFriendRelationChanged?.Invoke(this);
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

    private int _lastChatId;

    public int LastChatId
    {
        get => _lastChatId;
        set => SetProperty(ref _lastChatId, value);
    }

    public event Action<FriendRelationDto> OnFriendRelationChanged;

    public void Dispose()
    {
        _userDto = null;
    }
}