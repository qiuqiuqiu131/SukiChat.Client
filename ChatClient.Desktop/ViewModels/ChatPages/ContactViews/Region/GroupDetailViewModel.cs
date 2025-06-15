using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;

public class GroupDetailViewModel : ViewModelBase, IRegionMemberLifetime
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISukiDialogManager _dialogManager;
    private readonly IUserManager _userManager;
    private GroupRelationDto? _group;

    public GroupRelationDto? Group
    {
        get => _group;
        set => SetProperty(ref _group, value);
    }


    public AvaloniaList<string>? GroupNames { get; private set; }

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

        GroupNames = new AvaloniaList<string>(_userManager.GroupGroups?.Select(d => d.GroupName).Order());
        if (_userManager.GroupGroups != null)
            _userManager.GroupGroups.CollectionChanged += GroupGroupsOnCollectionChanged;
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
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var item in e.NewItems)
            {
                if (item is GroupGroupDto groupGroup)
                {
                    // 如果GroupNames中不包含此分组名称，则按顺序插入
                    if (!GroupNames.Contains(groupGroup.GroupName))
                    {
                        // 找到正确的插入位置
                        int insertIndex = 0;
                        while (insertIndex < GroupNames.Count &&
                               string.Compare(GroupNames[insertIndex], groupGroup.GroupName) < 0)
                        {
                            insertIndex++;
                        }

                        // 在正确位置插入
                        GroupNames.Insert(insertIndex, groupGroup.GroupName);
                    }
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (var item in e.OldItems)
            {
                if (item is GroupGroupDto groupGroup)
                {
                    // 检查是否有其他分组使用相同的名称
                    bool nameStillInUse = _userManager.GroupGroups?.Any(g => g.GroupName == groupGroup.GroupName) ??
                                          false;

                    // 如果没有其他分组使用此名称，则从列表中移除
                    if (!nameStillInUse && GroupNames.Contains(groupGroup.GroupName))
                    {
                        GroupNames.Remove(groupGroup.GroupName);
                    }
                }
            }
        }
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
        if (Group != null)
        {
            Group.GroupDto.OnGroupChanged += GroupDtoOnOnGroupChanged;
        }
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        if (Group != null)
        {
            Group.GroupDto.OnGroupChanged -= GroupDtoOnOnGroupChanged;
            Group = null;
        }
    }

    /// <summary>
    /// GroupRelation发生变化时调用
    /// </summary>
    private async void Group_OnGroupRelationChanged()
    {
        var groupService = _containerProvider.Resolve<IGroupService>();
        await groupService.UpdateGroupRelation(_userManager.User.Id, Group);

        IUserDtoManager userDtoManager = _containerProvider.Resolve<IUserDtoManager>();
        var memberDto = await userDtoManager.GetGroupMemberDto(Group.GroupDto.Id, _userManager.User.Id);
        if (memberDto != null)
            memberDto.NickName = Group.NickName;
    }

    private async void GroupDtoOnOnGroupChanged()
    {
        var groupService = _containerProvider.Resolve<IGroupService>();
        await groupService.UpdateGroup(_userManager.User.Id, Group.GroupDto);
    }

    public bool KeepAlive => false;
}