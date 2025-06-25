using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ChatClient.Avalonia.Common;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;

public class GroupDetailViewModel : ViewModelBase, IRegionMemberLifetime, IDestructible
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISukiDialogManager _dialogManager;
    private readonly IUserManager _userManager;
    private GroupRelationDto? _group;

    public GroupRelationDto? Group
    {
        get => _group;
        set
        {
            if (SetProperty(ref _group, value))
            {
                RaisePropertyChanged(nameof(GroupMembers));
            }
        }
    }

    public IEnumerable<GroupMemberDto>? GroupMembers => Group?.GroupDto?.GroupMembers
        .OrderBy(d => d.Status)
        .ThenBy(d => d.JoinTime).Take(10);


    public IEnumerable<string>? GroupNames => _userManager.GroupGroups?.Select(d => d.GroupName).Order();

    private SubscriptionToken? _token;

    public DelegateCommand SendMessageCommand { get; init; }
    public DelegateCommand ShareGroupCommand { get; init; }

    public GroupDetailViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        ISukiDialogManager dialogManager,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _dialogManager = dialogManager;
        _userManager = userManager;

        SendMessageCommand = new DelegateCommand(SendMessage);
        ShareGroupCommand = new DelegateCommand(ShareGroup);

        if (_userManager.GroupGroups != null)
            _userManager.GroupGroups.CollectionChanged += GroupGroupsOnCollectionChanged;
        _token = _eventAggregator.GetEvent<GroupRenameEvent>()
            .Subscribe(() => RaisePropertyChanged(nameof(GroupNames)));
    }

    private void ShareGroup()
    {
        if (Group == null) return;

        _dialogManager.CreateDialog()
            .WithViewModel(d => new ShareViewModel(d, new DialogParameters
            {
                { "ShareMess", new CardMessDto { IsUser = false, Id = Group.Id } },
                { "ShowMess", false }
            }, null))
            .TryShow();
    }

    private void GroupGroupsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(GroupNames));
    }

    private async void SendMessage()
    {
        var group = Group;
        _eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
        _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(group);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        Group = parameters.GetValue<GroupRelationDto>("dto");
        if (Group != null && Group.GroupDto != null)
            Group.GroupDto.OnGroupChanged += GroupDtoOnOnGroupChanged;
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        if (Group != null && Group.GroupDto != null)
            Group.GroupDto.OnGroupChanged -= GroupDtoOnOnGroupChanged;
        Group = null;
    }

    private async void GroupDtoOnOnGroupChanged()
    {
        var groupService = _containerProvider.Resolve<IGroupService>();
        await groupService.UpdateGroup(_userManager.User.Id, Group.GroupDto);
    }

    public bool KeepAlive => false;

    public void Destroy()
    {
        _token?.Dispose();
        if (_userManager.GroupGroups != null)
            _userManager.GroupGroups.CollectionChanged -= GroupGroupsOnCollectionChanged;
    }
}