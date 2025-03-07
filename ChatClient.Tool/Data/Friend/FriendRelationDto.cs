namespace ChatClient.Tool.Data;

public class FriendRelationDto : BindableBase
{
    public string Id { get; set; }

    public string Grouping { get; set; }

    public DateTime GroupTime { get; set; }

    private UserDto? _userDto;

    public UserDto? UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    private string? _remark;

    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    private bool _cantDisturb;

    public bool CantDisturb
    {
        get => _cantDisturb;
        set => SetProperty(ref _cantDisturb, value);
    }

    private bool _isTop;

    public bool IsTop
    {
        get => _isTop;
        set => SetProperty(ref _isTop, value);
    }
}