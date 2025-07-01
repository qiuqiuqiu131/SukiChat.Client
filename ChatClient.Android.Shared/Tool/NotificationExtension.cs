using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Notification;

namespace ChatClient.Android.Shared.Tool;

public static class NotificationExtension
{
    public static void ShowMessage(this INotificationMessageManager manager, string message, NotificationType type,
        TimeSpan delay)
    {
        var g = App.Current.FindResource("NotificationCardInformationIconPathData");

        var icon = new PathIcon
        {
            Data = type switch
            {
                NotificationType.Information =>
                    App.Current.FindResource("NotificationCardInformationIconPathData") as Geometry,
                NotificationType.Success => App.Current.FindResource("NotificationCardSuccessIconPathData") as Geometry,
                NotificationType.Warning => App.Current.FindResource("NotificationCardWarningIconPathData") as Geometry,
                NotificationType.Error => App.Current.FindResource("NotificationCardErrorIconPathData") as Geometry,
            },
            Foreground = type switch
            {
                NotificationType.Information =>
                    App.Current.FindResource("NotificationCardInformationIconForeground") as IBrush,
                NotificationType.Success => App.Current.FindResource("NotificationCardSuccessIconForeground") as IBrush,
                NotificationType.Warning => App.Current.FindResource("NotificationCardWarningIconForeground") as IBrush,
                NotificationType.Error => App.Current.FindResource("NotificationCardErrorIconForeground") as IBrush,
            },
            Margin = new Thickness(15, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Height = 23,
            Width = 23
        };

        manager.CreateMessage()
            .Animates(true)
            .WithAdditionalContent(ContentLocation.Left, icon)
            .HasMessage(message)
            .Dismiss().WithDelay(delay)
            .Queue();
    }
}