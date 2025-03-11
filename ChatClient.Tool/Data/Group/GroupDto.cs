using Avalonia.Collections;
using Avalonia.Media.Imaging;

namespace ChatClient.Tool.Data.Group;

public class GroupDto : BindableBase
{
    public string Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    private string id;

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    private string name;

    public string? Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    private string? description;

    public DateTime CreateTime
    {
        get => createTime;
        set => SetProperty(ref createTime, value);
    }

    private DateTime createTime;

    public AvaloniaList<GroupMemberDto> GroupMembers
    {
        get => groupMembers;
        set => SetProperty(ref groupMembers, value);
    }

    private AvaloniaList<GroupMemberDto> groupMembers = new();

    public int HeadIndex
    {
        get => headIndex;
        set => SetProperty(ref headIndex, value);
    }

    private int headIndex;

    public Bitmap HeadImage
    {
        get => headImage;
        set => SetProperty(ref headImage, value);
    }

    private Bitmap headImage;

    public void CopyFrom(GroupDto dto)
    {
        Name = dto.Name;
        Description = dto.Description;
        CreateTime = dto.CreateTime;
        headIndex = dto.headIndex;
        HeadImage = dto.HeadImage;
    }
}