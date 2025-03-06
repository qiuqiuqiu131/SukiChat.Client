using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        OverlaySplitView.IsPaneOpen = false;
    }

    private void ShowRightView(object? sender, RoutedEventArgs e)
    {
        OverlaySplitView.IsPaneOpen = !OverlaySplitView.IsPaneOpen;
    }
}