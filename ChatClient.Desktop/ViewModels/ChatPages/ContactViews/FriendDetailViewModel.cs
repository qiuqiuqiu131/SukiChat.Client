using System.Threading.Tasks;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class FriendDetailViewModel : ViewModelBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;

    private FriendRelationDto? _friend;

    public FriendRelationDto? Friend
    {
        get => _friend;
        set => SetProperty(ref _friend, value);
    }

    public DelegateCommand SendMessageCommand { get; init; }

    public FriendDetailViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _userManager = userManager;

        SendMessageCommand = new DelegateCommand(SendMessage);
    }

    private async void SendMessage()
    {
        var friend = Friend;
        _eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
        _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(friend);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        Friend = parameters.GetValue<FriendRelationDto>("dto");
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        Friend = null;
    }
}