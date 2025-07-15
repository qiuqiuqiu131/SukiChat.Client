using Avalonia.Collections;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.Android.Shared.ViewModels;

public class FriendChatViewModel : SidePageViewModelBase
{
    private readonly IChatLRService _chatLrService;
    private readonly IUserManager _userManager;

    private UserDto? _userDto;

    public UserDto? UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    private FriendChatDto? _friendChatDto = null;

    public FriendChatDto? FriendChatDto
    {
        get => _friendChatDto;
        set => SetProperty(ref _friendChatDto, value);
    }

    public DelegateCommand ReturnCommand { get; }

    public FriendChatViewModel(IContainerProvider containerProvider, IChatLRService chatLrService,
        IUserManager userManager) : base(
        containerProvider)
    {
        _chatLrService = chatLrService;
        _userManager = userManager;

        UserDto = _userManager.User.UserDto;
        ReturnCommand = new DelegateCommand(ReturnBack);
    }

    public override async Task OnSideViewOpened(ISideViewManager sideViewManager, INavigationParameters? parameters)
    {
        await base.OnSideViewOpened(sideViewManager, parameters);
        if (parameters.TryGetValue(nameof(ChatClient.Tool.Data.Friend.FriendChatDto), out FriendChatDto? friendChatDto))
        {
            await Task.Run(() => _chatLrService.LoadFriendChatDto(_userManager.User!.Id, friendChatDto!));
            FriendChatDto = friendChatDto;
        }
    }

    public override async Task OnSideViewClosed()
    {
        await base.OnSideViewClosed();
        if (FriendChatDto != null)
        {
            var friendChat = FriendChatDto;
            FriendChatDto = null;
            _chatLrService.ClearFriendChatDto(friendChat);
        }
    }
}