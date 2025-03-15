using Avalonia.Collections;

namespace ChatClient.Tool.Data.Group;

public class GroupGroupDto : BindableBase, IDisposable
{
    private AvaloniaList<GroupRelationDto> _groups;

    public AvaloniaList<GroupRelationDto> Groups
    {
        get => _groups;
        set => SetProperty(ref _groups, value);
    }

    private string _groupName;

    public string GroupName
    {
        get => _groupName;
        set => SetProperty(ref _groupName, value);
    }

    public event Action<GroupRelationDto> DeSelectItemEvent;

    public void DeSelectItem(GroupRelationDto group) =>
        DeSelectItemEvent?.Invoke(group);

    public void Dispose()
    {
        _groups.Clear();
        _groups = null;

        DeSelectItemEvent = null;
    }
}