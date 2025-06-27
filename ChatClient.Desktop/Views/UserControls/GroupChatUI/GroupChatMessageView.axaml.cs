using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChatClient.Avalonia.Controls.Chat;
using ChatClient.Desktop.Views.UserControls.ChatUI;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Desktop.Views.UserControls.GroupChatUI;

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

    #region MessageBoxEvent

    public event EventHandler<MessageBoxShowEventArgs> MessageBoxShow;

    public static readonly RoutedEvent<MessageBoxShowEventArgs> MessageBoxShowEvent =
        RoutedEvent.Register<GroupChatMessageView, MessageBoxShowEventArgs>(nameof(MessageBoxShow),
            RoutingStrategies.Bubble);

    #endregion

    public GroupChatMessageView()
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

    private void FileMess_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is FileMessDto fileMessDto)
        {
            if (fileMessDto.IsSuccess && fileMessDto.TargetFilePath != null)
            {
                var info = new FileInfo(fileMessDto.TargetFilePath);
                if (info.Exists)
                {
                    var argument = $"/select,\"{fileMessDto.TargetFilePath}\"";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", argument)
                    {
                        UseShellExecute = true
                    });
                }
            }
        }
    }
}