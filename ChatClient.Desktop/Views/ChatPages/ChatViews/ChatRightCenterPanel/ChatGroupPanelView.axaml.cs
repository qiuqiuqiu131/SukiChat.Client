using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.Chat.GroupChatUI;
using ChatClient.Avalonia.Controls.OverlaySplitView;
using ChatClient.BaseService.Manager;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Events;
using Prism.Navigation;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;

public partial class ChatGroupPanelView : UserControl, IDestructible
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;
    private readonly IUserDtoManager _userDtoManager;

    private readonly SubscriptionToken? token;

    public ChatGroupPanelView(IEventAggregator eventAggregator, IUserManager userManager,
        IUserDtoManager userDtoManager)
    {
        _eventAggregator = eventAggregator;
        _userManager = userManager;
        _userDtoManager = userDtoManager;
        InitializeComponent();
        token = eventAggregator.GetEvent<SelectChatDtoChanged>()
            .Subscribe(() => { Dispatcher.UIThread.Invoke(() => { OverlaySplitView.IsPaneOpen = false; }); });
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

    private async void ChatUI_OnHeadClick(object? sender, GroupHeadClickEventArgs e)
    {
        var userDto = e.User.IsUser
            ? _userManager.User
            : (await _userDtoManager.GetUserDto(e.User.Owner.UserId));
        _eventAggregator.GetEvent<UserMessageBoxShowEvent>()
            .Publish(new UserMessageBoxShowArgs(userDto, e.PointerPressedEventArgs));
    }

    private void HeadName_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _eventAggregator.GetEvent<GroupMessageBoxShowEvent>().Publish(
            new GroupMessageBoxShowEventArgs(
                ((ChatGroupPanelViewModel)DataContext).SelectedGroup.GroupRelationDto.GroupDto,
                e) { PlacementMode = PlacementMode.BottomEdgeAlignedLeft });
    }
}