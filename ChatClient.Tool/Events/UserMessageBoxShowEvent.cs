using Avalonia.Input;
using ChatClient.Tool.Data;

namespace ChatClient.Tool.Events;

public class UserMessageBoxShowEvent : PubSubEvent<UserMessageBoxShowArgs>
{
}

public class UserMessageBoxShowArgs
{
    public PointerPressedEventArgs Args { get; }

    public UserDto User { get; }

    public bool BottomCheck { get; set; } = true;

    public UserMessageBoxShowArgs(UserDto user, PointerPressedEventArgs args)
    {
        User = user;
        Args = args;
    }
}