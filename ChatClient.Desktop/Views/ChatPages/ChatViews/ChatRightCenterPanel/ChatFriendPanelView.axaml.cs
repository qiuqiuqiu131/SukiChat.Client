using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.Chat.ChatUI;
using ChatClient.Avalonia.Controls.Chat.GroupChatUI;
using ChatClient.BaseService.Manager;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Events;
using Prism.Navigation;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;

public partial class ChatFriendPanelView : UserControl, IDestructible
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;
    private readonly SubscriptionToken? token;

    public ChatFriendPanelView(IEventAggregator eventAggregator, IUserManager userManager,
        IUserDtoManager userDtoManager)
    {
        _eventAggregator = eventAggregator;
        _userManager = userManager;
        InitializeComponent();

        token = eventAggregator.GetEvent<SelectChatDtoChanged>()
            .Subscribe(() => { Dispatcher.UIThread.Invoke(() => { return OverlaySplitView.IsPaneOpen = false; }); });
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

    private void ChatUI_OnHeadClick(object? sender, FriendHeadClickEventArgs e)
    {
        var userDto = e.User.IsUser
            ? _userManager.User
            : ((ChatFriendPanelViewModel)DataContext!).SelectedFriend.FriendRelatoinDto.UserDto;
        _eventAggregator.GetEvent<UserMessageBoxShowEvent>()
            .Publish(new UserMessageBoxShowArgs(userDto, e.PointerPressedEventArgs));
    }

    private void HeadName_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _eventAggregator.GetEvent<UserMessageBoxShowEvent>().Publish(
            new UserMessageBoxShowArgs(((ChatFriendPanelViewModel)DataContext).SelectedFriend.FriendRelatoinDto.UserDto,
                e) { BottomCheck = false, PlacementMode = PlacementMode.BottomEdgeAlignedLeft });
    }
}