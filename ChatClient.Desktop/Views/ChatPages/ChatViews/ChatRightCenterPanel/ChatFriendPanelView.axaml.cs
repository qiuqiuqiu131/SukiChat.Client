using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using Prism.Events;
using Prism.Navigation;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;

public partial class ChatFriendPanelView : UserControl, IDestructible
{
    private SubscriptionToken? token;

    public ChatFriendPanelView(IEventAggregator eventAggregator)
    {
        InitializeComponent();

        token = eventAggregator.GetEvent<SelectChatDtoChanged>()
            .Subscribe(() => { OverlaySplitView.IsPaneOpen = false; });
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

    public void Destroy()
    {
        token?.Dispose();
    }
}