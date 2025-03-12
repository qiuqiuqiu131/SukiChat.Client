namespace ChatClient.Tool.Data.Group;

public class GroupReceivedDto : BindableBase
{
    public int RequestId { get; set; }
    public string UserFromId { get; set; }

    public string GroupId { get; set; }

    public DateTime ReceiveTime { get; set; }

    public string Message { get; set; }

    private bool isAccept = false;

    public bool IsAccept
    {
        get => isAccept;
        set => SetProperty(ref isAccept, value);
    }

    private bool isSolved = false;

    public bool IsSolved
    {
        get => isSolved;
        set => SetProperty(ref isSolved, value);
    }

    private DateTime? solveTime;

    public DateTime? SolveTime
    {
        get => solveTime;
        set => SetProperty(ref solveTime, value);
    }

    private string? acceptByUserId;

    public string? AcceptByUserId
    {
        get => acceptByUserId;
        set => SetProperty(ref acceptByUserId, value);
    }

    private UserDto? _userDto;

    public UserDto? UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    private GroupDto? _groupDto;

    public GroupDto? GroupDto
    {
        get => _groupDto;
        set => SetProperty(ref _groupDto, value);
    }
}