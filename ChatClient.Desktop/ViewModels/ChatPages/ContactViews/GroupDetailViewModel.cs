using System.Threading.Tasks;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class GroupDetailViewModel : ViewModelBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;
    private GroupRelationDto? _group;

    public GroupRelationDto? Group
    {
        get => _group;
        set => SetProperty(ref _group, value);
    }

    public DelegateCommand SendMessageCommand { get; init; }

    public GroupDetailViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _userManager = userManager;

        SendMessageCommand = new DelegateCommand(SendMessage);
    }

    private async void SendMessage()
    {
        _eventAggregator.GetEvent<ChangePageEvent>().Publish("聊天");
        await Task.Delay(50);
        _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(Group);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        Group = parameters.GetValue<GroupRelationDto>("dto");
        if (Group != null)
            Group.OnGroupRelationChanged += Group_OnGroupRelationChanged;
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        if (Group != null)
        {
            Group.OnGroupRelationChanged -= Group_OnGroupRelationChanged;
            Group = null;
        }
    }

    /// <summary>
    /// GroupRelation发生变化时调用
    /// </summary>
    private async void Group_OnGroupRelationChanged()
    {
        var groupService = _containerProvider.Resolve<IGroupService>();
        var result = await groupService.UpdateGroupRelation(_userManager.User.Id, Group);
    }
}