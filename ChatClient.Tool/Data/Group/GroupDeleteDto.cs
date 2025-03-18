namespace ChatClient.Tool.Data.Group;

public class GroupDeleteDto : BindableBase, IDisposable
{
    public string DeleteId { get; set; }
    public string GroupId { get; set; }

    public string MemberId { get; set; }

    public int DeleteMethod { get; set; }

    public string OperateUserId { get; set; }

    public DateTime DeleteTime { get; set; }

    private GroupDto? groupDto;

    public GroupDto? GroupDto
    {
        get => groupDto;
        set => SetProperty(ref groupDto, value);
    }

    private UserDto? userDto;

    public UserDto? UserDto
    {
        get => userDto;
        set => SetProperty(ref userDto, value);
    }

    public void Dispose()
    {
        groupDto = null;
        userDto = null;
    }
}