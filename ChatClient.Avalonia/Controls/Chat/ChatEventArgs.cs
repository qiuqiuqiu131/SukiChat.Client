using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Avalonia.Controls.Chat;

/// <summary>
/// 头像点击事件参数
/// </summary>
public class FriendHeadClickEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 用户数据
    /// </summary>
    public ChatData User { get; }

    /// <summary>
    /// 鼠标点击事件
    /// </summary>
    public PointerPressedEventArgs PointerPressedEventArgs { get; }

    public FriendHeadClickEventArgs(object sender, RoutedEvent routeEvent, ChatData user, PointerPressedEventArgs args)
        : base(routeEvent, sender)
    {
        User = user;
        PointerPressedEventArgs = args;
    }
}

public class NotificationMessageEventArgs : RoutedEventArgs
{
    public string Message { get; }

    public NotificationType Type { get; }

    public NotificationMessageEventArgs(object sender, RoutedEvent routeEvent, string message, NotificationType type)
        : base(routeEvent, sender)
    {
        Message = message;
        Type = type;
    }
}

public class MessageBoxShowEventArgs : RoutedEventArgs
{
    public CardMessDto CardMessDto { get; }

    public PointerPressedEventArgs PointerPressedEventArgs { get; }

    public MessageBoxShowEventArgs(object sender, RoutedEvent routedEvent, PointerPressedEventArgs pressArgs,
        CardMessDto cardMessDto)
        : base(routedEvent, sender)
    {
        CardMessDto = cardMessDto;
        PointerPressedEventArgs = pressArgs;
    }
}

/// <summary>
/// 头像点击事件参数
/// </summary>
public class GroupHeadClickEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 用户数据
    /// </summary>
    public GroupChatData User { get; }

    /// <summary>
    /// 鼠标点击事件
    /// </summary>
    public PointerPressedEventArgs PointerPressedEventArgs { get; }

    public GroupHeadClickEventArgs(object sender, RoutedEvent routeEvent, GroupChatData user,
        PointerPressedEventArgs args) : base(routeEvent, sender)
    {
        User = user;
        PointerPressedEventArgs = args;
    }
}