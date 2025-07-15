using Avalonia.Collections;
using ChatClient.Android.Shared.Views;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Control;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.Android.Shared.ViewModels;

public class ChatPageViewModel : BindableBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    private readonly ISideOverlayViewManager _sideOverlayViewManager;

    public AvaloniaList<FriendChatDto> Friends => _userManager.FriendChats!;
    public AvaloniaList<GroupChatDto> Groups => _userManager.GroupChats!;

    public DelegateCommand<FriendChatDto> FriendSelectionChangedCommand { get; init; }
    public AsyncDelegateCommand<GroupChatDto> GroupSelectionChangedCommand { get; init; }

    public ChatPageViewModel(IContainerProvider containerProvider, IUserManager userManager,
        ISideOverlayViewManager sideOverlayViewManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;
        _sideOverlayViewManager = sideOverlayViewManager;

        FriendSelectionChangedCommand = new DelegateCommand<FriendChatDto>(OnFriendSelectionChanged);
        GroupSelectionChangedCommand = new AsyncDelegateCommand<GroupChatDto>(OnGroupSelectionChanged);
    }

    // 点击好友列表项的群聊时触发
    private async Task OnGroupSelectionChanged(GroupChatDto arg)
    {
        await _sideOverlayViewManager.ShowSidePanelAsync(typeof(GroupChatView), new NavigationParameters
        {
            { nameof(GroupChatDto), arg }
        }, null, SidePanelAnimationType.FadeAndSlide);
    }

    // 点击好友列表项的好友时触发
    private async void OnFriendSelectionChanged(FriendChatDto arg)
    {
        await _sideOverlayViewManager.ShowSidePanelAsync(typeof(FriendChatView), new NavigationParameters
        {
            { nameof(FriendChatDto), arg }
        }, null, SidePanelAnimationType.FadeAndSlide);
    }
}