namespace ChatClient.Tool.Data.Friend;

public class FriendDeleteDto : BindableBase, IDisposable
{
    public int DeleteId { get; set; }

    public string UseId1 { get; set; }

    public string UserId2 { get; set; }

    public DateTime DeleteTime { get; set; }

    public bool IsUser { get; set; } = false;

    private UserDto? userDto;

    public UserDto? UserDto
    {
        get => userDto;
        set => SetProperty(ref userDto, value);
    }

    public void Dispose()
    {
        userDto = null;
    }
}