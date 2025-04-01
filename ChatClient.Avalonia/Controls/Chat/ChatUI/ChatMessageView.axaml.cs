using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChatClient.Tool.Data;

namespace ChatClient.Avalonia.Controls.Chat.ChatUI;

public partial class ChatMessageView : UserControl
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

    #region MessageBoxEvent

    public event EventHandler<MessageBoxShowEventArgs> MessageBoxShow;

    public static readonly RoutedEvent<MessageBoxShowEventArgs> MessageBoxShowEvent =
        RoutedEvent.Register<ChatMessageView, MessageBoxShowEventArgs>(nameof(MessageBoxShow),
            RoutingStrategies.Bubble);

    #endregion

    public ChatMessageView()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is CardMessDto cardMessDto &&
            e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            RaiseEvent(new MessageBoxShowEventArgs(control, MessageBoxShowEvent, e, cardMessDto));
        }
    }
}