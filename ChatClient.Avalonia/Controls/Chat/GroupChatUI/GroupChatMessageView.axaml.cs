using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ChatClient.Avalonia.Controls.Chat.ChatUI;

namespace ChatClient.Avalonia.Controls.Chat.GroupChatUI;

public partial class GroupChatMessageView : UserControl
{
    public static readonly StyledProperty<bool> IsLeftProperty =
        AvaloniaProperty.Register<ChatMessageView, bool>(nameof(IsLeft), defaultValue: false);

    public bool IsLeft
    {
        get => GetValue(IsLeftProperty);
        set => SetValue(IsLeftProperty, value);
    }

    public static readonly StyledProperty<object> MessageProperty =
        AvaloniaProperty.Register<ChatMessageView, object>(nameof(Message));

    public object Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public GroupChatMessageView()
    {
        InitializeComponent();
    }
}