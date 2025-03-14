using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.Avalonia.Controls.OverlaySplitView;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Prism.Events;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;

public partial class ChatGroupPanelView : UserControl
{
    public ChatGroupPanelView(IEventAggregator eventAggregator)
    {
        InitializeComponent();
        eventAggregator.GetEvent<SelectChatDtoChanged>().Subscribe(() => { OverlaySplitView.IsPaneOpen = false; });
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