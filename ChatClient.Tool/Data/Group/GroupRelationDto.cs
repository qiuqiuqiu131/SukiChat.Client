namespace ChatClient.Tool.Data.Group;

public class GroupRelationDto : BindableBase
{
    public string Id { get; set; }

    public string Grouping { get; set; }

    public DateTime GroupTime { get; set; }

    private GroupDto? _groupDto;

    public GroupDto? GroupDto
    {
        get => _groupDto;
        set => SetProperty(ref _groupDto, value);
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