using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.Android.Shared.ViewModels;

public class GroupChatViewModel : SidePageViewModelBase
{
    private readonly IChatLRService _chatLrService;
    private readonly IUserManager _userManager;

    private GroupChatDto? _groupChatDto;

    public GroupChatDto? GroupChatDto
    {
        get => _groupChatDto;
        set => SetProperty(ref _groupChatDto, value);
    }

    public DelegateCommand ReturnCommand { get; }

    public GroupChatViewModel(IContainerProvider containerProvider, IChatLRService chatLrService,
        IUserManager userManager) : base(
        containerProvider)
    {
        _chatLrService = chatLrService;
        _userManager = userManager;

        ReturnCommand = new DelegateCommand(ReturnBack);
    }

    public override async Task OnSideViewOpened(ISideViewManager sideViewManager, INavigationParameters? parameters)
    {
        await base.OnSideViewOpened(sideViewManager, parameters);
        if (parameters.TryGetValue(nameof(ChatClient.Tool.Data.Group.GroupChatDto), out GroupChatDto? groupChatDto))
        {
            GroupChatDto = groupChatDto;
            await Task.Run(() => _chatLrService.LoadGroupChatDto(_userManager.User!.Id, GroupChatDto!));
        }
    }

    public override async Task OnSideViewClosed()
    {
        await base.OnSideViewClosed();
        if (GroupChatDto != null)
        {
            var groupChatDto = GroupChatDto;
            GroupChatDto = null;
            _chatLrService.ClearGroupChatDto(groupChatDto);
        }
    }
}