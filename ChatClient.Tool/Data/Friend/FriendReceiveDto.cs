namespace ChatClient.Tool.Data.Friend;

public class FriendReceiveDto : BindableBase, IDisposable
{
    public int RequestId { get; set; }

    public string UserFromId { get; set; }

    public string UserTargetId { get; set; }

    public string Message { get; set; }
    public DateTime ReceiveTime { get; set; }

    private bool isAccept;

    public bool IsAccept
    {
        get => isAccept;
        set => SetProperty(ref isAccept, value);
    }

    private bool isSolved;

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

    private UserDto? _userDto;

    public UserDto? UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    public void Dispose()
    {
        _userDto = null;
    }
}