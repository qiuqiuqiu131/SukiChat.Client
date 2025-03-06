namespace ChatClient.Tool.Data;

public class FriendReceiveDto : BindableBase
{
    public int RequestId { get; set; }

    public string UserFromId { get; set; }

    public string UserTargetId { get; set; }

    public DateTime ReceiveTime { get; set; }

    private UserDto? _userDto;

    public UserDto? UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }
}