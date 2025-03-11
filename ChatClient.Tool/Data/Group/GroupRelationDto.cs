namespace ChatClient.Tool.Data.Group;

public class GroupRelationDto : BindableBase
{
    public string Id { get; set; }

    public DateTime GroupTime { get; set; }

    private GroupDto? _groupDto;

    public GroupDto? GroupDto
    {
        get => _groupDto;
        set => SetProperty(ref _groupDto, value);
    }

    private string grouping;

    public string Grouping
    {
        get => grouping;
        set
        {
            if (SetProperty(ref grouping, value))
            {
                OnGroupRelationChanged?.Invoke();
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
                OnGroupRelationChanged?.Invoke();
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
                OnGroupRelationChanged?.Invoke();
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
                OnGroupRelationChanged?.Invoke();
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
                OnGroupRelationChanged?.Invoke();
            }
        }
    }

    public event Action OnGroupRelationChanged;
}