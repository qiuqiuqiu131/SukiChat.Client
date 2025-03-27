using Avalonia.Controls;
using Avalonia.Input;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Tool.Events;

public class GroupMessageBoxShowEvent : PubSubEvent<GroupMessageBoxShowEventArgs>
{
}

public class GroupMessageBoxShowEventArgs
{
    public PointerPressedEventArgs Args { get; }

    public GroupDto Group { get; }

    public PlacementMode PlacementMode { get; set; } = PlacementMode.Bottom;

    public GroupMessageBoxShowEventArgs(GroupDto group, PointerPressedEventArgs args)
    {
        Group = group;
        Args = args;
    }
}