using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Notification.Controls;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.Chat.ChatUI;
using ChatClient.Avalonia.Controls.Chat.GroupChatUI;
using ChatClient.BaseService.Manager;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;

public partial class ChatFriendPanelView : UserControl, IDestructible
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;
    private readonly SubscriptionToken? token;
    private readonly SubscriptionToken? token2;

    public ChatFriendPanelView(IEventAggregator eventAggregator, IUserManager userManager,
        IUserDtoManager userDtoManager)
    {
        _eventAggregator = eventAggregator;
        _userManager = userManager;
        InitializeComponent();

        token = eventAggregator.GetEvent<SelectChatDtoChanged>()
            .Subscribe(() => { Dispatcher.UIThread.Invoke(() => { return OverlaySplitView.IsPaneOpen = false; }); });
        token2 = eventAggregator.GetEvent<NewMenuShow>().Subscribe(() =>
        {
            Dispatcher.UIThread.Invoke(() => { ChatUI.CloseMenu(); });
        });
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

    private void ChatUI_OnNotification(object? sender, NotificationMessageEventArgs e)
    {
        _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
        {
            Message = e.Message,
            Type = e.Type
        });
    }

    private async void ChatUI_OnMessageBoxShow(object? sender, MessageBoxShowEventArgs e)
    {
        if (sender is ChatUI chatUi)
        {
            if (e.CardMessDto.IsUser)
            {
                var userDtoManager = App.Current.Container.Resolve<IUserDtoManager>();
                var userDto = await userDtoManager.GetUserDto(e.CardMessDto.Id);
                if (userDto != null)
                {
                    var position = ((Control)e.PointerPressedEventArgs.Source).TranslatePoint(new Point(0, 0), chatUi);
                    var width = chatUi.Bounds.Width;
                    if (position.Value.X > width / 2)
                        _eventAggregator.GetEvent<UserMessageBoxShowEvent>().Publish(
                            new UserMessageBoxShowArgs(userDto, e.PointerPressedEventArgs)
                            {
                                PlacementMode = PlacementMode.LeftEdgeAlignedTop
                            });
                    else
                        _eventAggregator.GetEvent<UserMessageBoxShowEvent>().Publish(
                            new UserMessageBoxShowArgs(userDto, e.PointerPressedEventArgs)
                            {
                                PlacementMode = PlacementMode.RightEdgeAlignedTop
                            });
                }
            }
            else
            {
                var userDtoManager = App.Current.Container.Resolve<IUserDtoManager>();
                var groupDto = await userDtoManager.GetGroupDto(_userManager.User.Id, e.CardMessDto.Id);
                if (groupDto != null)
                {
                    var position = ((Control)e.PointerPressedEventArgs.Source).TranslatePoint(new Point(0, 0), chatUi);
                    var width = chatUi.Bounds.Width;
                    if (position.Value.X > width / 2)
                        _eventAggregator.GetEvent<GroupMessageBoxShowEvent>().Publish(
                            new GroupMessageBoxShowEventArgs(groupDto, e.PointerPressedEventArgs)
                            {
                                PlacementMode = PlacementMode.LeftEdgeAlignedTop
                            });
                    else
                        _eventAggregator.GetEvent<GroupMessageBoxShowEvent>().Publish(
                            new GroupMessageBoxShowEventArgs(groupDto, e.PointerPressedEventArgs)
                            {
                                PlacementMode = PlacementMode.RightEdgeAlignedTop
                            });
                }
            }
        }
    }

    private void ChatUI_OnContextMenuShow(object? sender, RoutedEventArgs e)
    {
        _eventAggregator.GetEvent<NewMenuShow>().Publish();
    }
}