namespace ChatClient.Tool.Data.Group;

public class GroupRelationDto : BindableBase, IDisposable
{
    public string Id { get; set; }

    public DateTime JoinTime { get; set; }

    private GroupDto? _groupDto;

    public GroupDto? GroupDto
    {
        get => _groupDto;
        set => SetProperty(ref _groupDto, value);
    }

    private string grouping;

    public string GroupingWithoutEvent
    {
        get => grouping;
        set => SetProperty(ref grouping, value);
    }

    public string Grouping
    {
        get => grouping;
        set
        {
            var origion = grouping;
            if (SetProperty(ref grouping, value))
            {
                OnGroupRelationChanged?.Invoke(this);
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
            if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_remark)) return;
            if (SetProperty(ref _remark, value))
            {
                OnGroupRelationChanged?.Invoke(this);
            }
        }
    }

    private string? _nickName;

    public string? NickName
    {
        get => _nickName;
        set
        {
            if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_nickName)) return;
            if (SetProperty(ref _nickName, value))
            {
                OnGroupRelationChanged?.Invoke(this);
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
                OnGroupRelationChanged?.Invoke(this);
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
                OnGroupRelationChanged?.Invoke(this);
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
                OnGroupRelationChanged?.Invoke(this);
            }
        }
    }

    private int _lastChatId;

    public int LastChatId
    {
        get => _lastChatId;
        set => SetProperty(ref _lastChatId, value);
    }

    private int status;

    public int Status
    {
        get => status;
        set
        {
            if (SetProperty(ref status, value))
            {
                RaisePropertyChanged(nameof(IsManager));
                RaisePropertyChanged(nameof(IsOwner));
            }
        }
    }

    public bool IsManager => status is 0 or 1;
    public bool IsOwner => status == 0;

    public event Action<GroupRelationDto> OnGroupRelationChanged;
    public event Action<GroupRelationDto, string> OnGroupingChanged;

    public void Dispose()
    {
        _groupDto = null;
        OnGroupRelationChanged = null;
    }
}