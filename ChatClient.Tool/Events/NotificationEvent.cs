using Avalonia.Controls.Notifications;

namespace ChatClient.Tool.Events;

public class NotificationEvent : PubSubEvent<NotificationEventArgs>
{
}

public struct NotificationEventArgs
{
    public NotificationType Type { get; set; }
    public string Message { get; set; }
}