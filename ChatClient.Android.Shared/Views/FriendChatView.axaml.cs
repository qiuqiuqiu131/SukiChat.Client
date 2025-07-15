using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ChatClient.Avalonia.Controls.Chat;
using ChatClient.Tool.Events;

namespace ChatClient.Android.Shared.Views;

public partial class FriendChatView : UserControl
{
    private readonly IEventAggregator _eventAggregator;

    public FriendChatView(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        InitializeComponent();
    }

    private void ChatUI_OnNotification(object? sender, NotificationMessageEventArgs e)
    {
        _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
        {
            Message = e.Message,
            Type = e.Type
        });
    }
}