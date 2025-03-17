using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.Avalonia.Controls.OverlaySplitView;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Prism.Events;
using Prism.Navigation;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;

public partial class ChatGroupPanelView : UserControl, IDestructible
{
    private SubscriptionToken? token;

    public ChatGroupPanelView(IEventAggregator eventAggregator)
    {
        InitializeComponent();
        token = eventAggregator.GetEvent<SelectChatDtoChanged>()
            .Subscribe(() => { OverlaySplitView.IsPaneOpen = false; });
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        OverlaySplitView.IsPaneOpen = false;
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