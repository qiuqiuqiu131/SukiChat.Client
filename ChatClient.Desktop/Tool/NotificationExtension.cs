using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Notification;
using SukiUI.ColorTheme;
using SukiUI.Content;

namespace ChatClient.Desktop.Tool;

public static class NotificationExtension
{
    public static void ShowMessage(this INotificationMessageManager manager, string message, NotificationType type,
        TimeSpan delay)
    {
        var icon = new PathIcon
        {
            Data = type switch
            {
                NotificationType.Information => Icons.CircleInformation,
                NotificationType.Success => Icons.CircleCheck,
                NotificationType.Warning => Icons.CircleWarning,
                NotificationType.Error => Icons.CircleWarning,
            },
            Foreground = type switch
            {
                NotificationType.Information => NotificationColor.InfoIconForeground,
                NotificationType.Success => NotificationColor.SuccessIconForeground,
                NotificationType.Warning => NotificationColor.WarningIconForeground,
                NotificationType.Error => NotificationColor.ErrorIconForeground,
            },
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Height = 20,
            Width = 20
        };

        manager.CreateMessage()
            .Animates(true)
            .WithAdditionalContent(ContentLocation.Left, icon)
            .HasMessage(message)
            .Dismiss().WithDelay(delay)
            .Queue();
    }
}