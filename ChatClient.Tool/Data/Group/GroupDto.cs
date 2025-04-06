using Avalonia.Collections;
using Avalonia.Media.Imaging;

namespace ChatClient.Tool.Data.Group;

public class GroupDto : BindableBase, IDisposable
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
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Name = name;
            }
            else if (SetProperty(ref name, value))
            {
                OnGroupChanged?.Invoke();
            }
        }
    }

    private string name;

    public string? Description
    {
        get => description;
        set
        {
            if (SetProperty(ref description, value))
            {
                OnGroupChanged?.Invoke();
            }
        }
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

    private bool isDisband;

    public bool IsDisband
    {
        get => isDisband;
        set => SetProperty(ref isDisband, value);
    }

    private bool isCustomHead;

    public bool IsCustomHead
    {
        get => isCustomHead;
        set => SetProperty(ref isCustomHead, value);
    }

    public Bitmap HeadImage
    {
        get => headImage;
        set => SetProperty(ref headImage, value);
    }

    private Bitmap headImage;

    private bool isEntered;

    public bool IsEntered
    {
        get => isEntered;
        set => SetProperty(ref isEntered, value);
    }

    private string? remark;

    public string? Remark
    {
        get => remark;
        set => SetProperty(ref remark, value);
    }

    public event Action OnGroupChanged;

    public void CopyFrom(GroupDto dto)
    {
        Name = dto.Name;
        Description = dto.Description;
        CreateTime = dto.CreateTime;
        HeadIndex = dto.headIndex;
        IsDisband = dto.IsDisband;
        IsCustomHead = dto.IsCustomHead;
        IsEntered = dto.IsEntered;
        Remark = dto.Remark;
        HeadImage = dto.HeadImage;
    }

    public void Dispose()
    {
        headImage = null;
        OnGroupChanged = null;

        groupMembers?.Clear();
        groupMembers = null;
    }
}