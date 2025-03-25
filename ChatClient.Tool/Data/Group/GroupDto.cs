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
            if (SetProperty(ref name, value))
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
        set
        {
            if (SetProperty(ref groupMembers, value))
                RaisePropertyChanged(nameof(GroupClipMembers));
        }
    }

    public List<GroupMemberDto> GroupClipMembers => [..GroupMembers.Take(16)];

    private AvaloniaList<GroupMemberDto> groupMembers = new();

    public int HeadIndex
    {
        get => headIndex;
        set
        {
            if (SetProperty(ref headIndex, value))
            {
                OnGroupChanged?.Invoke();
            }
        }
    }

    private int headIndex;

    private bool isDisband;

    public bool IsDisband
    {
        get => isDisband;
        set => SetProperty(ref isDisband, value);
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

    public event Action OnGroupChanged;

    public GroupDto()
    {
        GroupMembers.CollectionChanged += (_, _) => { RaisePropertyChanged(nameof(GroupClipMembers)); };
    }

    public void CopyFrom(GroupDto dto)
    {
        Name = dto.Name;
        Description = dto.Description;
        CreateTime = dto.CreateTime;
        headIndex = dto.headIndex;
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