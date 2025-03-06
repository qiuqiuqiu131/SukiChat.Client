using Avalonia.Collections;

namespace ChatClient.Tool.Data;

public class GroupFriendDto : BindableBase
{
    private AvaloniaList<FriendRelationDto> _friends;

    public AvaloniaList<FriendRelationDto> Friends
    {
        get => _friends;
        set => SetProperty(ref _friends, value);
    }

    private string _groupName;

    public string GroupName
    {
        get => _groupName;
        set => SetProperty(ref _groupName, value);
    }
}