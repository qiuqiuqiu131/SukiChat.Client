using Avalonia.Collections;

namespace ChatClient.Tool.Data.Friend;

public class GroupFriendDto : BindableBase, IDisposable
{
    private AvaloniaList<FriendRelationDto>? _friends;

    public AvaloniaList<FriendRelationDto>? Friends
    {
        get => _friends;
        set => SetProperty(ref _friends, value);
    }

    private string _groupName = string.Empty;

    public string GroupName
    {
        get => _groupName;
        set => SetProperty(ref _groupName, value);
    }

    public event Action<FriendRelationDto> DeSelectItemEvent;

    public void DeSelectItem(FriendRelationDto friend) =>
        DeSelectItemEvent?.Invoke(friend);

    public void Dispose()
    {
        DeSelectItemEvent = null;
        Friends?.Clear();
        Friends = null;
    }
}